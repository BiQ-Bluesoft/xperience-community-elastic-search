using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Admin.InfoModels.ElasticSearchIndexItem;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Aliasing;
using Kentico.Xperience.ElasticSearch.Helpers;
using Kentico.Xperience.ElasticSearch.Helpers.Constants;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

/// <summary>
/// Default implementation of <see cref="IElasticSearchClient"/>.
/// </summary>
internal class DefaultElasticSearchClient(
    IContentQueryExecutor executor,
    ElasticsearchClient elasticSearchClient,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService,
    IEventLogService eventLogService,
    IProgressiveCache cache,
    IServiceProvider serviceProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    IInfoProvider<ElasticSearchIndexItemInfo> elasticSearchIndexProvider) : IElasticSearchClient
{

    /// <inheritdoc/>
    public async Task<ICollection<ElasticSearchIndexStatisticsViewModel>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var indices = ElasticSearchIndexStore.Instance.GetAllIndices();

        var statistics = new List<ElasticSearchIndexStatisticsViewModel>();
        foreach (var index in indices)
        {
            var countResponse = await elasticSearchClient.CountAsync<IElasticSearchModel>(c => c.Indices(index.IndexName), cancellationToken);
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

    /// <inheritdoc/>
    public async Task<ElasticSearchResponse> CreateIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var elasticSearchStrategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);

        var indexExistsInElastic = (await elasticSearchClient.Indices.ExistsAsync(indexName, cancellationToken))?.Exists ?? false;
        if (!indexExistsInElastic)
        {
            return await elasticSearchStrategy.CreateIndexInternalAsync(elasticSearchClient, indexName, cancellationToken);
        }
        return ElasticSearchResponse.Success();
    }

    /// <inheritdoc />
    public async Task<ElasticSearchResponse> DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        eventLogService.LogInformation(
            nameof(DefaultElasticSearchClient),
            EventLogConstants.ElasticDeleteEventCode,
            $"Delete of index {indexName} started.");

        // Get real index, because the given index name can be only alias name for the index.
        var getResponse = await elasticSearchClient.Indices.GetAsync(indexName, cancellationToken);
        if (!getResponse.IsValidResponse || getResponse.Indices.Count == 0)
        {
            eventLogService.LogError(
                nameof(DefaultElasticSearchClient),
                EventLogConstants.ElasticDeleteEventCode,
                $"Index or alias with name: {indexName} not found. Get operation failed with error: {getResponse.DebugInformation}");
            return ElasticSearchResponse.Failure($"Index with name: {indexName} not found.");
        }

        var deleteResponse = await elasticSearchClient.Indices
            .DeleteAsync(getResponse.Indices.First().Key.ToString(), cancellationToken);
        if (!deleteResponse.IsValidResponse)
        {
            eventLogService.LogError(
                nameof(DefaultElasticSearchClient),
                EventLogConstants.ElasticDeleteEventCode,
                $"Unable to delete index with name: {indexName}. Operation failed with error: {deleteResponse.DebugInformation}");
            return ElasticSearchResponse.Failure($"Unable to delete index with name: {indexName}.");
        }
        return ElasticSearchResponse.Success();
    }

    /// <inheritdoc />
    public async Task<ElasticSearchResponse> EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (newConfiguration.IndexName != oldIndexName)
        {
            // Do we want this functionality?
            return ElasticSearchResponse.Failure($"Editing name of the already created index is not permitted.");
        }
        else
        {
            return await StartRebuildAsync(newConfiguration.IndexName, cancellationToken);
        }
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

        var bulkDeleteResponse = await elasticSearchClient.BulkAsync(bulk =>
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

        var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
        return await strategy.UploadDocumentsAsync(models, elasticSearchClient, indexName);
    }

    /// <inheritdoc />
    public async Task<ElasticSearchResponse> StartRebuildAsync(string indexName, CancellationToken cancellationToken = default)
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
        var elasticResponse = await strategy.CreateIndexInternalAsync(elasticSearchClient, newIndexName, cancellationToken);
        if (!elasticResponse.IsSuccess)
        {
            return elasticResponse;
        }

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
        return ElasticSearchResponse.Success();
    }

    /// <inheritdoc/>
    public async Task EndRebuildAsync(string indexName, string? elasticOldIndex, string elasticNewIndex, CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyIndexName(indexName);

        if (elasticOldIndex != null)
        {
            // Retrieve existing aliases
            var getAliasResponse = await elasticSearchClient.Indices
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
                    // we need to transfer index name alias after deleting the old index.
                    if (alias == indexName)
                    {
                        continue;
                    }
                    await elasticSearchIndexAliasService.AddAliasAsync(alias, elasticNewIndex, cancellationToken);
                }
            }

            // Delete old index
            var deleteResponse = await elasticSearchClient.Indices.DeleteAsync(elasticOldIndex, cancellationToken);
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

        // Set last rebuild
        var kenticoElasticIndex = elasticSearchIndexProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemIndexName), indexName)
            .FirstOrDefault();
        if (kenticoElasticIndex != null)
        {
            kenticoElasticIndex.ElasticSearchIndexItemLastRebuild = DateTime.Now;
            kenticoElasticIndex.Update();
        }
    }

    #region Private methods

    private static void ValidateNonEmptyIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
    }

    private async Task<(string?, string)> GetElasticIndexNames(string indexName)
    {
        var primaryName = $"{indexName}-primary";
        var secondaryName = $"{indexName}-secondary";

        var getResponse = await elasticSearchClient.Indices.GetAsync(indexName);
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
                var contentTypeNames = includedPathAttribute.ContentTypes.Select(ct => ct.ContentTypeName);
                var queryBuilder = new ContentItemQueryBuilder()
                    .ForContentTypes(q => q
                        .OfContentType(contentTypeNames.ToArray())
                        .ForWebsite(elasticSearchIndex.WebSiteChannelName, includeUrlPath: true, pathMatch: pathMatch))
                    .InLanguage(language);

                var webpages = await executor.GetWebPageResult(queryBuilder, container => container, cancellationToken: cancellationToken ?? default);
                foreach (var page in webpages)
                {
                    var item = await MapToEventItemAsync(page);
                    indexedItems.Add(item);
                }
            }
        }
    }

    private async Task AddReusableItemsAsync(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken, List<IIndexEventItemModel> indexedItems)
    {
        foreach (var language in elasticSearchIndex.LanguageNames)
        {
            if (elasticSearchIndex.IncludedReusableContentTypes != null && elasticSearchIndex.IncludedReusableContentTypes.Count > 0)
            {
                var queryBuilder = new ContentItemQueryBuilder()
                        .ForContentTypes(q => q.OfContentType(elasticSearchIndex.IncludedReusableContentTypes.ToArray()))
                        .InLanguage(language);

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
