using Elastic.Clients.Elasticsearch;

namespace Kentico.Xperience.ElasticSearch.Search;

/// <summary>
/// Primary service used for querying elasticsearch indexes
/// </summary>
public interface IElasticSearchQueryClientService
{
    ElasticsearchClient CreateSearchClientForQueries(string indexName);
}
