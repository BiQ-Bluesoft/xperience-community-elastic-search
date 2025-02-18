using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Aliasing;
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
    IConversionService conversionService,
    IProgressiveCache cache,
    ElasticsearchClient searchIndexClient,
    IEventLogService eventLogService,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService) : IElasticSearchClient
{

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
                    "ELASTIC_SEARCH",
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
        ValidateIndexWithNameExists(indexName);

        await searchIndexClient.Indices.DeleteAsync(indexName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteRecordsAsync(IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken)
    {
        ValidateIndexWithNameExists(indexName);

        if (!itemGuids.Any())
        {
            return await Task.FromResult(0);
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
            eventLogService.LogError(nameof(DeleteRecordsAsync),
                "ELASTIC_SEARCH",
                $"Unable to delete records with guids: {itemGuids} from index with name {indexName}. Operation failed with error: {bulkDeleteResponse.DebugInformation}");
        }

        return bulkDeleteResponse.Items.Count(item => item.Status == 200);
    }

    /// <inheritdoc />
    public async Task<int> UpsertRecordsAsync(IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken)
    {
        ValidateIndexWithNameExists(indexName);

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
    public async Task StartRebuildAsync(string indexName, CancellationToken? cancellationToken)
    {
        ValidateIndexWithNameExists(indexName);

        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetRequiredIndex(indexName);

        var indexedItems = await FetchContentForRebuildAsync(executor, elasticSearchIndex, cancellationToken);
        EnqueueRebuildTasksAsync(elasticSearchIndex, indexedItems);
    }

    /// <inheritdoc />
    public async Task RebuildAsync(string indexName, CancellationToken cancellationToken)
    {
        ValidateIndexWithNameExists(indexName);

        // Retrieve existing aliases
        var getAliasResponse = await searchIndexClient.Indices
            .GetAliasAsync(alias => alias.Indices(indexName), cancellationToken);
        if (!getAliasResponse.IsValidResponse)
        {
            eventLogService.LogError(nameof(RebuildAsync),
                "ELASTIC_SEARCH",
                $"Unable to retrieve aliases for index with name {indexName}. Operation failed with error: {getAliasResponse.DebugInformation}");
        }
        var aliases = getAliasResponse.Aliases.Values.SelectMany(val => val.Aliases.Keys);

        // Delete index in elastic
        await DeleteIndexAsync(indexName, cancellationToken);

        // Create index in elastic
        var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
        await strategy.CreateIndexInternalAsync(searchIndexClient, indexName, cancellationToken);

        // Reassign aliases
        if (aliases.Any())
        {
            foreach (var alias in aliases)
            {
                await elasticSearchIndexAliasService.AddAliasAsync(alias, indexName, cancellationToken);
            }
        }
    }

    #region Private methods

    private static void ValidateIndexWithNameExists(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
    }

    private static void EnqueueRebuildTasksAsync(ElasticSearchIndex elasticSearchIndex, List<IIndexEventItemModel> indexedItems)
    {
        ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(new ElasticSearchQueueItem(null, ElasticSearchTaskType.REBUILD, elasticSearchIndex.IndexName));
        indexedItems.ForEach(item => ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(new ElasticSearchQueueItem(item, ElasticSearchTaskType.PUBLISH_INDEX, elasticSearchIndex.IndexName)));
    }

    private async Task<List<IIndexEventItemModel>> FetchContentForRebuildAsync(IContentQueryExecutor executor, ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken)
    {
        var indexedItems = new List<IIndexEventItemModel>();

        foreach (var includedPathAttribute in elasticSearchIndex.IncludedPaths)
        {
            var pathMatch =
             includedPathAttribute.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase)
                 ? PathMatch.Children(includedPathAttribute.AliasPath[..^2])
                 : PathMatch.Single(includedPathAttribute.AliasPath);

            await AddPageItemsAsync(executor, elasticSearchIndex, cancellationToken, indexedItems, includedPathAttribute, pathMatch);

        }

        await AddReusableItemsAsync(executor, elasticSearchIndex, cancellationToken, indexedItems);

        return indexedItems;
    }

    private async Task AddPageItemsAsync(IContentQueryExecutor executor, ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken, List<IIndexEventItemModel> indexedItems, ElasticSearchIndexIncludedPath includedPathAttribute, PathMatch pathMatch)
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

    private async Task AddReusableItemsAsync(IContentQueryExecutor executor, ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken, List<IIndexEventItemModel> indexedItems)
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
                    items.Add(new(conversionService.GetInteger(channelID, 0), conversionService.GetString(channelName, string.Empty)));
                }
            }

            return items.AsEnumerable();
        }, new CacheSettings(5, nameof(DefaultElasticSearchClient), nameof(GetAllWebsiteChannelsAsync)));

    #endregion
}
