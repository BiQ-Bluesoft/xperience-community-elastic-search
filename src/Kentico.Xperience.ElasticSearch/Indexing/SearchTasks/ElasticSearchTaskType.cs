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
    /// Task marks the end of indexed items, index is published after this task occurs
    /// </summary>
    PUBLISH_INDEX,

    /// <summary>
    /// A task for a page which should be updated
    /// </summary>
    UPDATE,

    /// <summary>
    /// Task marks the start of rebuilding index, index is deleted and recreated after this task occurs.
    /// </summary>
    REBUILD,
}
