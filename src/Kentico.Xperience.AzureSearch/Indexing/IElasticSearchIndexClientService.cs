//using Kentico.Xperience.AzureSearch.Admin;

using Nest;

namespace Kentico.Xperience.AzureSearch.Indexing;

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

    /// <summary>
    /// Edits the AzureSearch index in Azure.
    /// </summary>
    /// <param name="oldIndexName">The name of index to edit.</param>
    /// <param name="newIndexConfiguration">New index configuration.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    //Task EditIndex(string oldIndexName, ElasticSearchConfigurationModel newIndexConfiguration, CancellationToken cancellationToken);
}
