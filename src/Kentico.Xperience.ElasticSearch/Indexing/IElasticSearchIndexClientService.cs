using Kentico.Xperience.ElasticSearch.Admin;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing;

/// <summary>
/// Initializes <see cref="ElasticClient" /> instances.
/// </summary>
public interface IElasticSearchIndexClientService
{
    /// <summary>
    /// Initializes a new <see cref="ElasticSearchIndex" /> for the given <paramref name="indexName" />
    /// </summary>
    /// <param name="indexName">The code name of the index.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    Task<ElasticClient> InitializeIndexClient(string indexName, CancellationToken cancellationToken);

    Task EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration,
            CancellationToken cancellationToken);
}
