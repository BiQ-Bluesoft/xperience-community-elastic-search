namespace Kentico.Xperience.ElasticSearch.Indexing;

/// <summary>
/// Contains methods for logging <see cref="ElasticSearchQueueItem"/>s and <see cref="ElasticSearchQueueItem"/>s
/// for processing by <see cref="ElasticSearchQueueWorker"/> and <see cref="ElasticSearchQueueWorker"/>.
/// </summary>
public interface IElasticSearchTaskLogger
{
    /// <summary>
    /// Logs an <see cref="ElasticSearchQueueItem"/> for each registered crawler. Then, loops
    /// through all registered ElasticSearch indexes and logs a task if the passed <paramref name="webpageItem"/> is indexed.
    /// </summary>
    /// <param name="webpageItem">The <see cref="IndexEventWebPageItemModel"/> that triggered the event.</param>
    /// <param name="eventName">The name of the Xperience event that was triggered.</param>
    Task HandleEvent(IndexEventWebPageItemModel webpageItem, string eventName);

    /// <summary>
    /// Logs an <see cref="ElasticSearchQueueItem"/> for each registered crawler. Then, loops
    /// through all registered ElasticSearch indexes and logs a task if the passed <paramref name="reusableItem"/> is indexed.
    /// </summary>
    /// <param name="reusableItem"></param>
    /// <param name="eventName"></param>
    /// <returns></returns>
    Task HandleReusableItemEvent(IndexEventReusableItemModel reusableItem, string eventName);
}
