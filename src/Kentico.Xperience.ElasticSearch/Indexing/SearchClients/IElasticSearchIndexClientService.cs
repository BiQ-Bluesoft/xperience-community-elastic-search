using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

/// <summary>
/// Initializes <see cref="ElasticsearchClient" /> instances.
/// </summary>
public interface IElasticSearchIndexClientService
{
    /// <summary>
    /// Initializes a new <see cref="ElasticSearchIndex" /> for the given <paramref name="indexName" />
    /// </summary>
    /// <param name="indexName">The code name of the index.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    Task<ElasticsearchClient> InitializeIndexClient(string indexName, CancellationToken cancellationToken);

    /// <summary>
    /// Edits the ElasticSearch index.
    /// </summary>
    /// <param name="oldIndexName">The name of index to edit.</param>
    /// <param name="newConfiguration">New index configuration.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="OperationCanceledException" />
    Task EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration,
            CancellationToken cancellationToken);
}
