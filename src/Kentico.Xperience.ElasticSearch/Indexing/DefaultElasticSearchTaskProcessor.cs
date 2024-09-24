using CMS.Base;
using CMS.Core;
using CMS.Websites;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.ElasticSearch.Indexing;

internal class ElasticSearchBatchResult
{
    internal int SuccessfulOperations { get; set; } = 0;
    internal HashSet<ElasticSearchIndex> PublishedIndices { get; set; } = [];
}

internal class DefaultElasticSearchTaskProcessor(
    IElasticSearchClient elasticSearchClient,
    IEventLogService eventLogService,
    IWebPageUrlRetriever urlRetriever,
    IServiceProvider serviceProvider) : IElasticSearchTaskProcessor
{

    /// <inheritdoc />
    public async Task<int> ProcessElasticSearchTasks(IEnumerable<ElasticSearchQueueItem> queueItems, CancellationToken cancellationToken, int maximumBatchSize = 100)
    {
        ElasticSearchBatchResult batchResults = new();

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
                var deleteIds = new List<string>();
                var deleteTasks = group.Where(queueItem => queueItem.TaskType == ElasticSearchTaskType.DELETE).ToList();

                var updateTasks = group.Where(queueItem => queueItem.TaskType is ElasticSearchTaskType.PUBLISH_INDEX or ElasticSearchTaskType.UPDATE);
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

                deleteIds.AddRange(GetIdsToDelete(deleteTasks ?? []).Where(x => x is not null).Select(x => x ?? ""));

                if (ElasticSearchIndexStore.Instance.GetIndex(group.Key) is { } index)
                {
                    previousBatchResults.SuccessfulOperations += await elasticSearchClient.DeleteRecords(deleteIds, group.Key, cancellationToken);
                    previousBatchResults.SuccessfulOperations += await elasticSearchClient.UpsertRecords(upsertData, group.Key, cancellationToken);

                    if (group.Any(t => t.TaskType == ElasticSearchTaskType.PUBLISH_INDEX) && !previousBatchResults.PublishedIndices.Any(x => x.IndexName == index.IndexName))
                    {
                        previousBatchResults.PublishedIndices.Add(index);
                    }
                }
                else
                {
                    eventLogService.LogError(nameof(DefaultElasticSearchTaskProcessor), nameof(ProcessElasticSearchTasks), "Index instance not exists");
                }
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultElasticSearchTaskProcessor), nameof(ProcessElasticSearchTasks), ex.Message);
            }
        }
    }

    private static IEnumerable<string?> GetIdsToDelete(IEnumerable<ElasticSearchQueueItem> deleteTasks) => deleteTasks.Select(queueItem => queueItem.ItemToIndex.ItemGuid.ToString());

    private async Task<IElasticSearchModel?> GetSearchModel(ElasticSearchQueueItem queueItem)
    {
        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetRequiredIndex(queueItem.IndexName);

        var strategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);

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
}
