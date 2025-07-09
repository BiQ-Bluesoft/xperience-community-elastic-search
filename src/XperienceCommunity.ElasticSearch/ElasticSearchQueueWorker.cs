using CMS.Base;
using CMS.Core;

using XperienceCommunity.ElasticSearch.Indexing;
using XperienceCommunity.ElasticSearch.Indexing.SearchTasks;

namespace XperienceCommunity.ElasticSearch;

/// <summary>
/// Thread worker which enqueues recently updated or deleted nodes indexed
/// by ElasticSearch and processes the tasks in the background thread.
/// </summary>
internal class ElasticSearchQueueWorker : ThreadQueueWorker<ElasticSearchQueueItem, ElasticSearchQueueWorker>
{
    private readonly IElasticSearchTaskProcessor elasticSearchTaskProcessor;

    /// <inheritdoc />
    protected override int DefaultInterval => 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchQueueWorker"/> class.
    /// Should not be called directly- the worker should be initialized during startup using
    /// <see cref="ThreadWorker{T}.EnsureRunningThread"/>.
    /// </summary>
    public ElasticSearchQueueWorker() => elasticSearchTaskProcessor = Service.Resolve<IElasticSearchTaskProcessor>() ?? throw new InvalidOperationException($"{nameof(IElasticSearchTaskProcessor)} is not registered.");

    /// <summary>
    /// Adds an <see cref="ElasticSearchQueueItem"/> to the worker queue to be processed.
    /// </summary>
    /// <param name="queueItem">The item to be added to the queue.</param>
    /// <exception cref="InvalidOperationException" />
    public static void EnqueueElasticSearchQueueItem(ElasticSearchQueueItem queueItem)
    {
        // Don't enqueue items that cannot be processed.
        if (string.IsNullOrEmpty(queueItem.IndexName) || queueItem.TaskType == ElasticSearchTaskType.UNKNOWN)
        {
            return;
        }

        // Don't enqueue items that require item to index to be processed correctly, but it is empty.
        if (queueItem.ItemToIndex == null && queueItem.TaskType != ElasticSearchTaskType.REBUILD_ITEM)
        {
            return;
        }

        if (ElasticSearchIndexStore.Instance.GetIndex(queueItem.IndexName) == null)
        {
            throw new InvalidOperationException($"Attempted to log task for ElasticSearch index '{queueItem.IndexName},' but it is not registered.");
        }

        Current.Enqueue(queueItem, false);
    }

    /// <inheritdoc />
    protected override void Finish() => RunProcess();

    /// <inheritdoc/>
    protected override void ProcessItem(ElasticSearchQueueItem item)
    {
    }

    /// <inheritdoc />
    protected override int ProcessItems(IEnumerable<ElasticSearchQueueItem> items) =>
         elasticSearchTaskProcessor.ProcessElasticSearchTasks(items, CancellationToken.None).GetAwaiter().GetResult();

}
