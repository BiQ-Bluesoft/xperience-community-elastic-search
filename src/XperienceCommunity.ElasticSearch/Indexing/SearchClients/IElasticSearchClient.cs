using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Helpers;
using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.SearchTasks;

namespace XperienceCommunity.ElasticSearch.Indexing.SearchClients;

/// <summary>
/// Contains methods to interface with the ElasticSearch API.
/// </summary>
public interface IElasticSearchClient
{
    /// <summary>
    /// Gets the indices of the ElasticSearch application with basic statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// 
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ObjectDisposedException" />
    Task<ICollection<ElasticSearchIndexStatisticsViewModel>> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new ElasticSearch index with the given name.
    /// </summary>
    /// <param name="indexName">Name of the index.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    Task<ElasticSearchResponse> CreateIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the ElasticSearch index by removing existing index data from Elastic.
    /// </summary>
    /// <param name="indexName">The index to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="indexName"/> is null.</exception>
    Task<ElasticSearchResponse> DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits the ElasticSearch index by starting the zero downtime rebuild action. 
    /// If edited index has a different name, return a failure response, since this is not permitted.
    /// </summary>
    /// <param name="oldIndexName">Name of the original index.</param>
    /// <param name="newConfiguration">New configuration of the index containing edited information.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    Task<ElasticSearchResponse> EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes records from the ElasticSearch index.
    /// </summary>
    /// <param name="itemGuids">The ElasticSearch internal IDs of the records to delete.</param>
    /// <param name="indexName">The index containing the objects to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// 
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ObjectDisposedException" />
    /// <exception cref="OverflowException" />
    /// <returns>The number of records deleted.</returns>
    Task<int> DeleteRecordsAsync(
        IEnumerable<string> itemGuids, string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the ElasticSearch index with the dynamic data in each object of the passed <paramref name="models"/>.
    /// </summary>
    /// <remarks>Logs an error if there are issues loading the node data.</remarks>
    /// <param name="models">The document to upsert into ElasticSearch.</param>
    /// <param name="indexName">The index to upsert the data to.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// 
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ObjectDisposedException" />
    /// <exception cref="OverflowException" />
    /// <returns>The number of objects processed.</returns>
    Task<int> UpsertRecordsAsync(
        IEnumerable<IElasticSearchModel> models, string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the rebuild process by creating new undelaying ElasticSearch index and creating corresponding search tasks 
    /// that will later be processed by <see cref="DefaultElasticSearchTaskProcessor"/>
    /// </summary>
    /// <param name="indexName">The index to rebuild.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// 
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ObjectDisposedException" />
    Task<ElasticSearchResponse> StartRebuildAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the rebuild process by transfering aliases from old underlaying ElasticSearch index to new underlaying ElasticSearch index 
    /// and setting alias with given indexName to point to the new underlaying index.
    /// </summary>
    /// <param name="indexName">Name of the index in Kentico.</param>
    /// <param name="elasticOldIndex">Name of the old underlaying index in ElasticSearch.</param>
    /// <param name="elasticNewIndex">Name of the new underlaying index in ElasticSearch.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    Task EndRebuildAsync(string indexName, string? elasticOldIndex, string elasticNewIndex, CancellationToken cancellationToken = default);
}
