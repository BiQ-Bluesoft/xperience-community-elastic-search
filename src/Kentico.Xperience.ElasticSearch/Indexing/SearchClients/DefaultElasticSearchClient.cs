using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;

using Kentico.Xperience.ElasticSearch.Admin.Models;
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
    IEventLogService eventLogService) : IElasticSearchClient
{
    /// <inheritdoc />
    public async Task<int> DeleteRecordsAsync(IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (!itemGuids.Any())
        {
            return await Task.FromResult(0);
        }

        return await DeleteRecordsInternalAsync(itemGuids, indexName, cancellationToken);
    }


    /// <inheritdoc/>
    public async Task<ICollection<ElasticSearchIndexStatisticsViewModel>> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var indices = ElasticSearchIndexStore.Instance.GetAllIndices();

        var stats = new List<ElasticSearchIndexStatisticsViewModel>();

        foreach (var index in indices)
        {
            var indexClient = await elasticSearchIndexClientService.InitializeIndexClient(index.IndexName, cancellationToken);

            var countResponse = await indexClient.CountAsync<IElasticSearchModel>(c => c.Indices(index.IndexName), cancellationToken);
            if (!countResponse.IsValidResponse)
            {
                // Additional work - Discuss whether exception should be thrown or logging the error is enough.
                eventLogService.LogError(
                    nameof(GetStatisticsAsync),
                    "ELASTIC_SEARCH",
                    $"Unable to fetch statistics for index with name {index.IndexName}. Operation failed with error: {countResponse.DebugInformation}");
            }

            stats.Add(new ElasticSearchIndexStatisticsViewModel()
            {
                Name = index.IndexName,
                Entries = countResponse.Count
            });
        }

        return stats;
    }

    /// <inheritdoc />
    public Task Rebuild(string indexName, CancellationToken? cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetRequiredIndex(indexName);
        return RebuildInternalAsync(elasticSearchIndex, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIndex(string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        await searchIndexClient.Indices.DeleteAsync(indexName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> UpsertRecords(IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (models == null || !models.Any())
        {
            return Task.FromResult(0);
        }

        return UpsertRecordsInternalAsync(models, indexName, cancellationToken);
    }

    private async Task<int> DeleteRecordsInternalAsync(IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken)
    {
        var bulkDeleteResponse = await searchIndexClient
            .BulkAsync(bulk =>
            {
                foreach (var guid in itemGuids)
                {
                    bulk.Delete(guid, d => d.Index(indexName));
                }
            }, cancellationToken);

        if (!bulkDeleteResponse.IsValidResponse)
        {
            // Additional work - Discuss whether exception should be thrown or logging the error is enough.
            eventLogService.LogError(nameof(DeleteRecordsInternalAsync),
                "ELASTIC_SEARCH",
                $"Unable to delete records with guids: {itemGuids} from index with name {indexName}. Operation failed with error: {bulkDeleteResponse.DebugInformation}");
        }

        return bulkDeleteResponse.Items.Count(item => item.Status == 200);
    }

    private async Task RebuildInternalAsync(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken)
    {
        var indexedItems = new List<IIndexEventItemModel>();

        foreach (var includedPathAttribute in elasticSearchIndex.IncludedPaths)
        {
            var pathMatch =
             includedPathAttribute.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase)
                 ? PathMatch.Children(includedPathAttribute.AliasPath[..^2])
                 : PathMatch.Single(includedPathAttribute.AliasPath);

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

        await searchIndexClient.Indices.DeleteAsync(elasticSearchIndex.IndexName, cancellationToken ?? default);
        indexedItems.ForEach(item => ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(new ElasticSearchQueueItem(item, ElasticSearchTaskType.PUBLISH_INDEX, elasticSearchIndex.IndexName)));
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

    private async Task<int> UpsertRecordsInternalAsync(IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken)
    {
        var upsertedCount = 0;
        var searchClient = await elasticSearchIndexClientService.InitializeIndexClient(indexName, cancellationToken);

        var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);

        upsertedCount += await strategy.UploadDocumentsAsync(models, searchClient, indexName);

        return upsertedCount;
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
}
