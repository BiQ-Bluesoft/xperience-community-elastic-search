using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Aliasing;
using Kentico.Xperience.ElasticSearch.Helpers.Constants;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

/// <summary>
/// Default implementation of <see cref="IElasticSearchClient"/>.
/// </summary>
internal class DefaultElasticSearchClient(
    IElasticSearchIndexClientService elasticSearchIndexClientService,
    IContentQueryExecutor executor,
    IServiceProvider serviceProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    IProgressiveCache cache,
    ElasticsearchClient searchIndexClient,
    IEventLogService eventLogService,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService) : IElasticSearchClient
{

    /// <inheritdoc/>
    public async Task<(string?, string)> GetElasticIndexNames(string indexName)
    {
        var primaryName = $"{indexName}-primary";
        var secondaryName = $"{indexName}-secondary";

        var getResponse = await searchIndexClient.Indices.GetAsync(indexName);
        if (!getResponse.IsValidResponse || getResponse.Indices.Count == 0)
        {
            return (null, primaryName);
        }

        var currentIndex = getResponse.Indices.First();
        var currentIndexName = currentIndex.Key.ToString();

        return currentIndexName == primaryName
            ? (currentIndexName, secondaryName)
            : (currentIndexName, primaryName);
    }

    /// <inheritdoc/>
    public async Task<ICollection<ElasticSearchIndexStatisticsViewModel>> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var indices = ElasticSearchIndexStore.Instance.GetAllIndices();

        var statistics = new List<ElasticSearchIndexStatisticsViewModel>();
        foreach (var index in indices)
        {
            var indexClient = await elasticSearchIndexClientService.InitializeIndexClient(index.IndexName, cancellationToken);

            var countResponse = await indexClient.CountAsync<IElasticSearchModel>(c => c.Indices(index.IndexName), cancellationToken);
            if (!countResponse.IsValidResponse)
            {
                eventLogService.LogError(
                    nameof(GetStatisticsAsync),
                    EventLogConstants.ElasticInfoEventCode,
                    $"Unable to fetch statistics for index with name {index.IndexName}. Operation failed with error: {countResponse.DebugInformation}");
            }

            statistics.Add(new ElasticSearchIndexStatisticsViewModel
            {
                Name = index.IndexName,
                Entries = countResponse.Count
            });
        }

        return statistics;
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken)
    {
        ValidateNonEmptyIndexName(indexName);

        eventLogService.LogInformation(
            nameof(DefaultElasticSearchClient),
            EventLogConstants.ElasticDeleteEventCode,
            $"Delete of index {indexName} started.");
        await searchIndexClient.Indices.DeleteAsync(indexName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteRecordsAsync(
        IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        if (!itemGuids.Any())
        {
            return 0;
        }

        var bulkDeleteResponse = await searchIndexClient.BulkAsync(bulk =>
        {
            foreach (var guid in itemGuids)
            {
                bulk.Delete(guid, d => d.Index(indexName));
            }

        }, cancellationToken);

        if (!bulkDeleteResponse.IsValidResponse)
        {
            eventLogService.LogError(
                nameof(DeleteRecordsAsync),
                EventLogConstants.ElasticItemsDeleteEventCode,
                $"Unable to delete records with guids: {itemGuids} from index with name {indexName}. Operation failed with error: {bulkDeleteResponse.DebugInformation}");
        }

        return bulkDeleteResponse.Items.Count(item => item.Status == 200);
    }

    /// <inheritdoc />
    public async Task<int> UpsertRecordsAsync(
        IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        if (models == null || !models.Any())
        {
            return 0;
        }

        var searchClient = await elasticSearchIndexClientService.InitializeIndexClient(indexName, cancellationToken);
        var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
        return await strategy.UploadDocumentsAsync(models, searchClient, indexName);
    }

    /// <inheritdoc />
    public async Task StartRebuildAsync(string indexName, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        eventLogService.LogInformation(
            nameof(DefaultElasticSearchClient),
            EventLogConstants.ElasticRebuildEventCode,
            $"Rebuild of index {indexName} started");

        // Get items that should be indexed in index
        var elasticIndex = ElasticSearchIndexStore.Instance.GetRequiredIndex(indexName);
        var indexedItems = await FetchContentForRebuildAsync(elasticIndex, cancellationToken);

        // Create new index for rebuild = to ensure zero down time
        var (oldIndexName, newIndexName) = await GetElasticIndexNames(indexName);
        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
        await strategy.CreateIndexInternalAsync(searchIndexClient, newIndexName, cancellationToken);

        // Enqueue items neccessary for rebuild
        indexedItems.ForEach(item =>
            ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(
                new ElasticSearchQueueItem(
                    item,
                    ElasticSearchTaskType.REBUILD_ITEM,
                    newIndexName)));
        ElasticSearchQueueWorker
            .EnqueueElasticSearchQueueItem(
                new ElasticSearchQueueItem(
                    new IndexEventRebuildEndModel(oldIndexName, newIndexName, indexName),
                    ElasticSearchTaskType.REBUILD_END,
                    elasticIndex.IndexName));
    }

    /// <inheritdoc/>
    public async Task EndRebuildAsync(string indexName, string? elasticOldIndex, string elasticNewIndex, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        if (elasticOldIndex != null)
        {
            // Retrieve existing aliases
            var getAliasResponse = await searchIndexClient.Indices
                .GetAliasAsync(alias => alias.Indices(elasticOldIndex), cancellationToken);
            if (!getAliasResponse.IsValidResponse)
            {
                eventLogService.LogError(nameof(DefaultElasticSearchClient),
                    EventLogConstants.ElasticRebuildEventCode,
                    $"Unable to retrieve aliases for index with name {indexName}. Operation failed with error: {getAliasResponse.DebugInformation}");
            }

            // Transfer aliases to new index
            var aliases = getAliasResponse.Aliases.Values.SelectMany(val => val.Aliases.Keys);
            if (aliases.Any())
            {
                foreach (var alias in aliases)
                {
                    await elasticSearchIndexAliasService.AddAliasAsync(alias, elasticNewIndex, cancellationToken);
                }
            }

            // Delete old index
            var deleteResponse = await searchIndexClient.Indices.DeleteAsync(elasticOldIndex, cancellationToken);
            if (!deleteResponse.IsValidResponse)
            {
                eventLogService.LogError(nameof(DefaultElasticSearchClient),
                    EventLogConstants.ElasticRebuildEventCode,
                    $"Unable to delete old index for {indexName}. Operation failed with error: {deleteResponse.DebugInformation}");
                return;
            }
        }

        // Transfer index name alias to new index
        await elasticSearchIndexAliasService.AddAliasAsync(indexName, elasticNewIndex, cancellationToken);
    }

    ///// <inheritdoc />
    //public async Task RebuildAsync(string indexName, CancellationToken cancellationToken)
    //{
    //    ValidateIndexWithNameExists(indexName);

    //    // Retrieve existing aliases
    //    var getAliasResponse = await searchIndexClient.Indices
    //        .GetAliasAsync(alias => alias.Indices(indexName), cancellationToken);
    //    if (!getAliasResponse.IsValidResponse)
    //    {
    //        eventLogService.LogError(nameof(RebuildAsync),
    //            EventLogConstants.ElasticRebuildEventCode,
    //            $"Unable to retrieve aliases for index with name {indexName}. Operation failed with error: {getAliasResponse.DebugInformation}");
    //    }
    //    var aliases = getAliasResponse.Aliases.Values.SelectMany(val => val.Aliases.Keys);

    //    var newIndexTempName = $"{indexName}_temp";

    //    // Create index in elastic
    //    var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
    //        throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");
    //    var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
    //    await strategy.CreateIndexInternalAsync(searchIndexClient, newIndexTempName, cancellationToken);

    //    var existsResponse = await searchIndexClient.Indices.ExistsAsync(indexName, cancellationToken);
    //    if (existsResponse.Exists)
    //    {
    //        var reindexResponse = await searchIndexClient.ReindexAsync(r => r
    //            .Source(s => s.Indices(indexName))
    //            .Dest(d => d.Index(newIndexTempName))
    //            .WaitForCompletion(true),
    //            cancellationToken);

    //        if (!reindexResponse.IsValidResponse)
    //        {
    //            eventLogService.LogError(
    //                nameof(DefaultElasticSearchClient),
    //                EventLogConstants.ElasticRebuildEventCode,
    //                $"Reindex from '{indexName}' to '{newIndexTempName}' failed. Error: {reindexResponse.DebugInformation}");
    //            return;
    //        }

    //        var aliasUpdateResponse = await searchIndexClient.Indices.UpdateAliasesAsync(a => a
    //            .Actions(actions =>
    //                actions
    //                    .Remove(r => r.Index(indexName).Alias(indexName))
    //                    .Add(new AddAction
    //                    {
    //                        Alias = newIndexTempName,
    //                        Index = newIndexTempName,
    //                    })
    //            ), cancellationToken);
    //    }

    //    // Delete index in elastic
    //    await DeleteIndexAsync(indexName, cancellationToken);



    //    // Reassign aliases
    //    if (aliases.Any())
    //    {
    //        foreach (var alias in aliases)
    //        {
    //            await elasticSearchIndexAliasService.AddAliasAsync(alias, indexName, cancellationToken);
    //        }
    //    }

    //    var index = indexItemInfoProvider.Get()
    //        .WhereEquals(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemIndexName), indexName)
    //        .FirstOrDefault();

    //    if (index != null)
    //    {
    //        index.ElasticSearchIndexItemLastRebuild = DateTime.Now;
    //        index.Update();
    //    }
    //}

    #region Private methods

    private static void ValidateNonEmptyIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
    }

    private async Task<List<IIndexEventItemModel>> FetchContentForRebuildAsync(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken)
    {
        var indexedItems = new List<IIndexEventItemModel>();

        foreach (var includedPathAttribute in elasticSearchIndex.IncludedPaths)
        {
            var pathMatch =
             includedPathAttribute.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase)
                 ? PathMatch.Children(includedPathAttribute.AliasPath[..^2])
                 : PathMatch.Single(includedPathAttribute.AliasPath);

            await AddPageItemsAsync(elasticSearchIndex, cancellationToken, indexedItems, includedPathAttribute, pathMatch);

        }

        await AddReusableItemsAsync(elasticSearchIndex, cancellationToken, indexedItems);

        return indexedItems;
    }

    private async Task AddPageItemsAsync(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken, List<IIndexEventItemModel> indexedItems, ElasticSearchIndexIncludedPath includedPathAttribute, PathMatch pathMatch)
    {
        foreach (var language in elasticSearchIndex.LanguageNames)
        {
            if (includedPathAttribute.ContentTypes != null && includedPathAttribute.ContentTypes.Count > 0)
            {
                var queryBuilder = new ContentItemQueryBuilder();
                foreach (var contentType in includedPathAttribute.ContentTypes)
                {
                    queryBuilder.ForContentType(contentType.ContentTypeName, config => config.ForWebsite(elasticSearchIndex.WebSiteChannelName, includeUrlPath: true, pathMatch: pathMatch));
                    queryBuilder.InLanguage(language);

                    var webpages = await executor.GetWebPageResult(queryBuilder, container => container, cancellationToken: cancellationToken ?? default);

                    foreach (var page in webpages)
                    {
                        var item = await MapToEventItemAsync(page);
                        indexedItems.Add(item);
                    }
                }
            }
        }
    }

    private async Task AddReusableItemsAsync(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken, List<IIndexEventItemModel> indexedItems)
    {
        foreach (var language in elasticSearchIndex.LanguageNames)
        {
            var queryBuilder = new ContentItemQueryBuilder();

            if (elasticSearchIndex.IncludedReusableContentTypes != null && elasticSearchIndex.IncludedReusableContentTypes.Count > 0)
            {
                foreach (var reusableContentType in elasticSearchIndex.IncludedReusableContentTypes)
                {
                    queryBuilder.ForContentType(reusableContentType);
                }
                queryBuilder.InLanguage(language);

                var reusableItems = await executor.GetResult(queryBuilder, result => result, cancellationToken: cancellationToken ?? default);
                foreach (var reusableItem in reusableItems)
                {
                    var item = await MapToEventReusableItemAsync(reusableItem);
                    indexedItems.Add(item);
                }
            }
        }
    }

    private async Task<IndexEventWebPageItemModel> MapToEventItemAsync(IWebPageContentQueryDataContainer content)
    {
        var languages = await GetAllLanguagesAsync();

        var languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? string.Empty;

        var websiteChannels = await GetAllWebsiteChannelsAsync();

        var channelName = websiteChannels.FirstOrDefault(c => c.WebsiteChannelID == content.WebPageItemWebsiteChannelID).ChannelName ?? string.Empty;

        var item = new IndexEventWebPageItemModel(
            content.WebPageItemID,
            content.WebPageItemGUID,
            languageName,
            content.ContentTypeName,
            content.WebPageItemName,
            content.ContentItemIsSecured,
            content.ContentItemContentTypeID,
            content.ContentItemCommonDataContentLanguageID,
            channelName,
            content.WebPageItemTreePath,
            content.WebPageItemOrder);

        return item;
    }

    private async Task<IndexEventReusableItemModel> MapToEventReusableItemAsync(IContentQueryDataContainer content)
    {
        var languages = await GetAllLanguagesAsync();

        var languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? string.Empty;

        var item = new IndexEventReusableItemModel(
            content.ContentItemID,
            content.ContentItemGUID,
            languageName,
            content.ContentTypeName,
            content.ContentItemName,
            content.ContentItemIsSecured,
            content.ContentItemContentTypeID,
            content.ContentItemCommonDataContentLanguageID);

        return item;
    }

    private Task<IEnumerable<ContentLanguageInfo>> GetAllLanguagesAsync() =>
        cache.LoadAsync(async cs =>
        {
            var results = await languageProvider.Get().GetEnumerableTypedResultAsync();

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");

            return results;
        }, new CacheSettings(5, nameof(DefaultElasticSearchClient), nameof(GetAllLanguagesAsync)));

    private Task<IEnumerable<(int WebsiteChannelID, string ChannelName)>> GetAllWebsiteChannelsAsync() =>
        cache.LoadAsync(async cs =>
        {

            var results = await channelProvider.Get()
                .Source(s => s.Join<WebsiteChannelInfo>(nameof(ChannelInfo.ChannelID), nameof(WebsiteChannelInfo.WebsiteChannelChannelID)))
                .Columns(nameof(WebsiteChannelInfo.WebsiteChannelID), nameof(ChannelInfo.ChannelName))
                .GetDataContainerResultAsync();

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency([$"{ChannelInfo.OBJECT_TYPE}|all", $"{WebsiteChannelInfo.OBJECT_TYPE}|all"]);

            var items = new List<(int WebsiteChannelID, string ChannelName)>();

            foreach (var item in results)
            {
                if (item.TryGetValue(nameof(WebsiteChannelInfo.WebsiteChannelID), out var channelID) && item.TryGetValue(nameof(ChannelInfo.ChannelName), out var channelName))
                {
                    items.Add(new(
                        ValidationHelper.GetInteger(channelID, 0),
                        ValidationHelper.GetString(channelName, string.Empty)));
                }
            }

            return items.AsEnumerable();
        }, new CacheSettings(5, nameof(DefaultElasticSearchClient), nameof(GetAllWebsiteChannelsAsync)));

    #endregion
}
