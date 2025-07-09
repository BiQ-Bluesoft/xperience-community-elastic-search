using CMS.Base;
using CMS.Core;
using CMS.Websites;

using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.SearchClients;

namespace XperienceCommunity.ElasticSearch.Indexing.SearchTasks;

internal class ElasticSearchBatchResult
{
    internal int SuccessfulOperations { get; set; }
    internal HashSet<ElasticSearchIndex> PublishedIndices { get; set; } = [];
}

/// <summary>
/// Default implementation of <see cref="IElasticSearchTaskProcessor"/>.
/// </summary>
internal class DefaultElasticSearchTaskProcessor(
    IElasticSearchClient elasticSearchClient,
    IEventLogService eventLogService,
    IWebPageUrlRetriever urlRetriever,
    IServiceProvider serviceProvider) : IElasticSearchTaskProcessor
{

    /// <inheritdoc />
    public async Task<int> ProcessElasticSearchTasks(IEnumerable<ElasticSearchQueueItem> queueItems, CancellationToken cancellationToken, int maximumBatchSize = 100)
    {
        var batchResults = new ElasticSearchBatchResult();
        var batches = queueItems.Batch(maximumBatchSize);

        foreach (var batch in batches)
        {
            await ProcessElasticSearchBatch(batch, batchResults, cancellationToken);
        }

        return batchResults.SuccessfulOperations;
    }

    private async Task ProcessElasticSearchBatch(IEnumerable<ElasticSearchQueueItem> queueItems, ElasticSearchBatchResult previousBatchResults, CancellationToken cancellationToken)
    {
        var groups = queueItems
            .Where(item => item.TaskType != ElasticSearchTaskType.REBUILD_END)
            .GroupBy(item => item.IndexName);

        foreach (var group in groups)
        {
            var indexName = group.Key;
            try
            {
                var deleteTasks = group
                    .Where(queueItem => queueItem.TaskType == ElasticSearchTaskType.DELETE)
                    .ToList();
                var updateTasks = group
                    .Where(queueItem => queueItem.TaskType is ElasticSearchTaskType.UPDATE or ElasticSearchTaskType.REBUILD_ITEM)
                    .ToList();

                var upsertData = new List<IElasticSearchModel>();
                foreach (var queueItem in updateTasks)
                {
                    var document = await GetSearchItem(queueItem);
                    if (document is not null)
                    {
                        upsertData.Add(document);
                    }
                    else
                    {
                        deleteTasks.Add(queueItem);
                    }
                }

                var deleteIds = GetIdsToDelete(deleteTasks ?? [])
                    .Where(x => x is not null)
                    .Select(x => x ?? string.Empty);

                previousBatchResults.SuccessfulOperations += await elasticSearchClient.DeleteRecordsAsync(deleteIds, indexName, cancellationToken);
                previousBatchResults.SuccessfulOperations += await elasticSearchClient.UpsertRecordsAsync(upsertData, indexName, cancellationToken);


            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultElasticSearchTaskProcessor), nameof(ProcessElasticSearchTasks), ex.Message);
            }
        }

        var rebuildEndItems = queueItems
            .Where(queueItem => queueItem.TaskType == ElasticSearchTaskType.REBUILD_END)
            .Select(item => item.ItemToIndex)
            .OfType<IndexEventRebuildEndModel>()
            .ToList();

        foreach (var rebuildEndItem in rebuildEndItems)
        {
            await elasticSearchClient.EndRebuildAsync(
                rebuildEndItem.IndexName,
                rebuildEndItem.CurrentElasticIndexName,
                rebuildEndItem.NewElasticIndexName,
                cancellationToken);
        }
    }

    #region Private methods


    private async Task<IElasticSearchModel?> GetSearchItem(ElasticSearchQueueItem queueItem)
    {
        if (queueItem.ItemToIndex is null)
        {
            return null;
        }

        var elasticIndex = ElasticSearchIndexStore.Instance.GetRequiredIndex(queueItem.IndexName);

        var strategy = serviceProvider.GetRequiredStrategy(elasticIndex);
        var data = await strategy.MapToElasticSearchModelOrNull(queueItem.ItemToIndex);
        if (data is null)
        {
            return null;
        }

        await AddBaseProperties(queueItem.ItemToIndex, data!);

        return data;
    }

    private async Task AddBaseProperties(IIndexEventItemModel eventItem, IElasticSearchModel searchItem)
    {
        if (eventItem is not null && searchItem is not null)
        {
            if (string.IsNullOrEmpty(searchItem.ItemGuid))
            {
                searchItem.ItemGuid = eventItem.ItemGuid.ToString();
            }
            if (string.IsNullOrEmpty(searchItem.ObjectID))
            {
                searchItem.ObjectID = $"{eventItem.ItemGuid}_{eventItem.LanguageName}";
            }
            if (string.IsNullOrEmpty(searchItem.ContentTypeName))
            {
                searchItem.ContentTypeName = eventItem.ContentTypeName;
            }
            if (string.IsNullOrEmpty(searchItem.LanguageName))
            {
                searchItem.LanguageName = eventItem.LanguageName;
            }

            if (eventItem is IndexEventWebPageItemModel webpageItem && string.IsNullOrEmpty(searchItem.Url))
            {
                try
                {
                    searchItem.Url = (await urlRetriever.Retrieve(webpageItem.WebPageItemTreePath, webpageItem.WebsiteChannelName, webpageItem.LanguageName)).RelativePath;
                }
                catch (Exception)
                {
                    // Retrieve can throw an exception when processing a page update ElasticSearchQueueItem
                    // and the page was deleted before the update task has processed. In this case, upsert an
                    // empty URL
                    searchItem.Url = string.Empty;
                }
            }
        }
    }

    private static IEnumerable<string?> GetIdsToDelete(IEnumerable<ElasticSearchQueueItem> deleteTasks) =>
        deleteTasks.Select(queueItem => queueItem.ItemToIndex?.ItemGuid.ToString());

    #endregion
}
