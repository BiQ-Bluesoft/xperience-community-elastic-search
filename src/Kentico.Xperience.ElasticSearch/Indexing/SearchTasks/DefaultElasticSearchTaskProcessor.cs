using CMS.Base;
using CMS.Core;
using CMS.Websites;

using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;

internal class ElasticSearchBatchResult
{
    internal int SuccessfulOperations { get; set; } = 0;
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
        var groups = queueItems.GroupBy(item => item.IndexName);

        foreach (var group in groups)
        {
            try
            {
                if (ElasticSearchIndexStore.Instance.GetIndex(group.Key) is not { } index)
                {
                    eventLogService
                        .LogError(nameof(DefaultElasticSearchTaskProcessor), nameof(ProcessElasticSearchTasks), "Index instance not exists");
                    continue;
                }

                var deleteTasks = group.Where(queueItem => queueItem.TaskType == ElasticSearchTaskType.DELETE).ToList();
                var updateTasks = group.Where(queueItem => queueItem.TaskType is ElasticSearchTaskType.PUBLISH_INDEX or ElasticSearchTaskType.UPDATE);
                var rebuildTasks = group.Where(queueItem => queueItem.TaskType is ElasticSearchTaskType.REBUILD);

                var upsertData = new List<IElasticSearchModel>();
                foreach (var queueItem in updateTasks)
                {
                    var document = await GetSearchModel(queueItem);
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

                await ProcessRebuildTasks(elasticSearchClient, rebuildTasks, cancellationToken);

                previousBatchResults.SuccessfulOperations += await elasticSearchClient.DeleteRecordsAsync(deleteIds, group.Key, cancellationToken);
                previousBatchResults.SuccessfulOperations += await elasticSearchClient.UpsertRecordsAsync(upsertData, group.Key, cancellationToken);

                if (group.Any(t => t.TaskType == ElasticSearchTaskType.PUBLISH_INDEX) && !previousBatchResults.PublishedIndices.Any(x => x.IndexName == index.IndexName))
                {
                    previousBatchResults.PublishedIndices.Add(index);
                }
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultElasticSearchTaskProcessor), nameof(ProcessElasticSearchTasks), ex.Message);
            }
        }
    }

    #region Private methods

    private static async Task ProcessRebuildTasks(IElasticSearchClient elasticSearchClient, IEnumerable<ElasticSearchQueueItem> rebuildTasks, CancellationToken cancellationToken)
    {
        var rebuildIndexName = rebuildTasks.Select(task => task.IndexName).FirstOrDefault();
        if (!string.IsNullOrEmpty(rebuildIndexName))
        {
            await elasticSearchClient.RebuildAsync(rebuildIndexName, cancellationToken);
        }
    }

    private static IEnumerable<string?> GetIdsToDelete(IEnumerable<ElasticSearchQueueItem> deleteTasks) => deleteTasks.Select(queueItem => queueItem.ItemToIndex?.ItemGuid.ToString());

    private async Task<IElasticSearchModel?> GetSearchModel(ElasticSearchQueueItem queueItem)
    {
        if (queueItem.ItemToIndex is null)
        {
            return null;
        }

        var strategy = serviceProvider.GetRequiredStrategy(ElasticSearchIndexStore.Instance.GetRequiredIndex(queueItem.IndexName));
        var data = await strategy.MapToElasticSearchModelOrNull(queueItem.ItemToIndex);
        if (data is null)
        {
            return null;
        }

        await AddBaseProperties(queueItem.ItemToIndex, data!);

        return data;
    }

    private async Task AddBaseProperties(IIndexEventItemModel item, IElasticSearchModel model)
    {
        model.ContentTypeName = item.ContentTypeName;
        model.LanguageName = item.LanguageName;
        model.ItemGuid = item.ItemGuid.ToString();
        model.ObjectID = item.ItemGuid.ToString();

        if (item is IndexEventWebPageItemModel webpageItem && string.IsNullOrEmpty(model.Url))
        {
            try
            {
                model.Url = (await urlRetriever.Retrieve(webpageItem.WebPageItemTreePath, webpageItem.WebsiteChannelName, webpageItem.LanguageName)).RelativePath;
            }
            catch (Exception)
            {
                // Retrieve can throw an exception when processing a page update ElasticSearchQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
                model.Url = string.Empty;
            }
        }
    }

    #endregion
}
