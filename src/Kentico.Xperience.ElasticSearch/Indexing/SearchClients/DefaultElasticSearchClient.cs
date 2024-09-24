using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;

using Microsoft.Extensions.DependencyInjection;

using Nest;

using BulkRequest = Nest.BulkRequest;

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
    ElasticClient searchIndexClient) : IElasticSearchClient
{
    /// <inheritdoc />
    public async Task<int> DeleteRecords(IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (!itemGuids.Any())
        {
            return await Task.FromResult(0);
        }

        return await DeleteRecordsInternal(itemGuids, indexName, cancellationToken);
    }


    /// <inheritdoc/>
    public async Task<ICollection<ElasticSearchIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken)
    {
        var indices = ElasticSearchIndexStore.Instance.GetAllIndices();

        var stats = new List<ElasticSearchIndexStatisticsViewModel>();

        foreach (var index in indices)
        {
            var indexClient = await elasticSearchIndexClientService.InitializeIndexClient(index.IndexName, cancellationToken);

            var countResponse = await indexClient.CountAsync<IElasticSearchModel>(c => c.Index(index.IndexName), cancellationToken);
            if (!countResponse.IsValid)
            {
                // TODO
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
        return RebuildInternal(elasticSearchIndex, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIndex(string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        await searchIndexClient.Indices.DeleteAsync(indexName, ct: cancellationToken);
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

        return UpsertRecordsInternal(models, indexName, cancellationToken);
    }

    private async Task<int> DeleteRecordsInternal(IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken)
    {
        var bulkDeleteResponse = await searchIndexClient.BulkAsync(new BulkRequest(indexName)
        {
            Operations = itemGuids.Select(x => new BulkDeleteOperation<BaseElasticSearchModel>(x)).Cast<IBulkOperation>().ToList()
        }, cancellationToken);

        if (!bulkDeleteResponse.IsValid)
        {
            // TODO
        }

        return bulkDeleteResponse.Items.Count(item => item.Status == 200);
    }

    private async Task RebuildInternal(ElasticSearchIndex elasticSearchIndex, CancellationToken? cancellationToken)
    {
        var indexedItems = new List<IndexEventWebPageItemModel>();

        foreach (var includedPathAttribute in elasticSearchIndex.IncludedPaths)
        {
            foreach (var language in elasticSearchIndex.LanguageNames)
            {
                var queryBuilder = new ContentItemQueryBuilder();

                if (includedPathAttribute.ContentTypes != null && includedPathAttribute.ContentTypes.Count > 0)
                {
                    foreach (var contentType in includedPathAttribute.ContentTypes)
                    {
                        queryBuilder.ForContentType(contentType.ContentTypeName, config => config.ForWebsite(elasticSearchIndex.WebSiteChannelName, includeUrlPath: true));
                    }
                }

                queryBuilder.InLanguage(language);

                var webpages = await executor.GetWebPageResult(queryBuilder, container => container, cancellationToken: cancellationToken ?? default);

                foreach (var page in webpages)
                {
                    var item = await MapToEventItem(page);
                    indexedItems.Add(item);
                }
            }
        }

        await searchIndexClient.Indices.DeleteAsync(elasticSearchIndex.IndexName, ct: cancellationToken ?? default);

        indexedItems.ForEach(item => ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(new ElasticSearchQueueItem(item, ElasticSearchTaskType.PUBLISH_INDEX, elasticSearchIndex.IndexName)));
    }

    private async Task<IndexEventWebPageItemModel> MapToEventItem(IWebPageContentQueryDataContainer content)
    {
        var languages = await GetAllLanguages();

        var languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? "";

        var websiteChannels = await GetAllWebsiteChannels();

        var channelName = websiteChannels.FirstOrDefault(c => c.WebsiteChannelID == content.WebPageItemWebsiteChannelID).ChannelName ?? "";

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

    private async Task<int> UpsertRecordsInternal(IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken)
    {
        var upsertedCount = 0;
        var searchClient = await elasticSearchIndexClientService.InitializeIndexClient(indexName, cancellationToken);

        var elasticIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);

        upsertedCount += await strategy.UploadDocuments(models, searchClient, indexName);

        return upsertedCount;
    }

    private Task<IEnumerable<ContentLanguageInfo>> GetAllLanguages() =>
        cache.LoadAsync(async cs =>
        {
            var results = await languageProvider.Get().GetEnumerableTypedResultAsync();

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");

            return results;
        }, new CacheSettings(5, nameof(DefaultElasticSearchClient), nameof(GetAllLanguages)));

    private Task<IEnumerable<(int WebsiteChannelID, string ChannelName)>> GetAllWebsiteChannels() =>
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
                    items.Add(new(conversionService.GetInteger(channelID, 0), conversionService.GetString(channelName, "")));
                }
            }

            return items.AsEnumerable();
        }, new CacheSettings(5, nameof(DefaultElasticSearchClient), nameof(GetAllWebsiteChannels)));
}
