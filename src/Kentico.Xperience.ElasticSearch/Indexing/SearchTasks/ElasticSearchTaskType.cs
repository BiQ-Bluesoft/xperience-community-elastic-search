namespace Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;

/// <summary>
/// Represents the type of a <see cref="ElasticSearchQueueItem"/>
/// </summary>
public enum ElasticSearchTaskType
{
    /// <summary>
    /// Unsupported task type
    /// </summary>
    UNKNOWN,

    /// <summary>
    /// A task for a page which should be removed from the index
    /// </summary>
    DELETE,

    /// <summary>
    /// A task for a page which should be updated
    /// </summary>
    UPDATE,

    /// <summary>
    /// Task marks the end of indexed items, index is published after this task occurs
    /// </summary>
    REBUILD_ITEM,

    /// <summary>
    /// Task marks the end of rebuilding index, all pointers are redirected to new rebuilt index and the old index is deleted.
    /// </summary>
    REBUILD_END,
}
