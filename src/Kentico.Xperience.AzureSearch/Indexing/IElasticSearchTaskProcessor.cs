namespace Kentico.Xperience.AzureSearch.Indexing;

/// <summary>
/// Processes tasks from <see cref="ElasticSearchQueueWorker"/>.
/// </summary>
public interface IElasticSearchTaskProcessor
{
    /// <summary>
    /// Processes multiple queue items from all ElasticSearch indexes in batches.
    /// </summary>
    /// <param name="queueItems">The items to process.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <param name="maximumBatchSize"></param>
    /// <returns>The number of items processed.</returns>
    Task<int> ProcessElasticSearchTasks(IEnumerable<ElasticSearchQueueItem> queueItems, CancellationToken cancellationToken, int maximumBatchSize = 100);
}
