using CMS.Core;
using CMS.Websites;

using XperienceCommunity.ElasticSearch.Indexing.Models;

namespace XperienceCommunity.ElasticSearch.Indexing.SearchTasks;

/// <summary>
/// Default implementation of <see cref="IElasticSearchTaskLogger"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultElasticSearchTaskLogger"/> class.
/// </remarks>
internal class DefaultElasticSearchTaskLogger(IEventLogService eventLogService, IServiceProvider serviceProvider) : IElasticSearchTaskLogger
{
    /// <inheritdoc />
    public async Task HandleEvent(IndexEventWebPageItemModel webpageItem, string eventName)
    {
        var taskType = GetTaskType(eventName);

        foreach (var elasticSearchIndex in ElasticSearchIndexStore.Instance.GetAllIndices())
        {
            if (!webpageItem.IsIndexedByIndex(eventLogService, elasticSearchIndex.IndexName, eventName))
            {
                continue;
            }

            var strategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);
            var toReindex = await strategy.FindItemsToReindex(webpageItem);

            foreach (var item in toReindex)
            {
                if (item.ItemGuid == webpageItem.ItemGuid)
                {
                    if (taskType == ElasticSearchTaskType.DELETE)
                    {
                        LogIndexTask(new ElasticSearchQueueItem(item, ElasticSearchTaskType.DELETE, elasticSearchIndex.IndexName));
                    }
                    else
                    {
                        LogIndexTask(new ElasticSearchQueueItem(item, ElasticSearchTaskType.UPDATE, elasticSearchIndex.IndexName));
                    }
                }
            }
        }
    }

    public async Task HandleReusableItemEvent(IndexEventReusableItemModel reusableItem, string eventName)
    {
        foreach (var elasticSearchIndex in ElasticSearchIndexStore.Instance.GetAllIndices())
        {
            if (!reusableItem.IsIndexedByIndex(eventLogService, elasticSearchIndex.IndexName, eventName))
            {
                continue;
            }

            var strategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);
            var toReindex = await strategy.FindItemsToReindex(reusableItem);

            foreach (var item in toReindex)
            {
                LogIndexTask(new ElasticSearchQueueItem(item, ElasticSearchTaskType.UPDATE, elasticSearchIndex.IndexName));
            }
        }
    }

    /// <summary>
    /// Logs a single <see cref="ElasticSearchQueueItem"/>.
    /// </summary>
    /// <param name="task">The task to log.</param>
    private void LogIndexTask(ElasticSearchQueueItem task)
    {
        try
        {
            ElasticSearchQueueWorker.EnqueueElasticSearchQueueItem(task);
        }
        catch (InvalidOperationException ex)
        {
            eventLogService.LogException(nameof(DefaultElasticSearchTaskLogger), nameof(LogIndexTask), ex);
        }
    }

    private static ElasticSearchTaskType GetTaskType(string eventName)
    {
        if (eventName.Equals(WebPageEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return ElasticSearchTaskType.UPDATE;
        }

        if (eventName.Equals(WebPageEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(WebPageEvents.Unpublish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return ElasticSearchTaskType.DELETE;
        }

        return ElasticSearchTaskType.UNKNOWN;
    }
}
