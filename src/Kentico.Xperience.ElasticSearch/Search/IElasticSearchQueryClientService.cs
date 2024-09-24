using Nest;

namespace Kentico.Xperience.ElasticSearch.Search;

/// <summary>
/// Primary service used for querying elasticsearch indexes
/// </summary>
public interface IElasticSearchQueryClientService
{
    ElasticClient CreateSearchClientForQueries(string indexName);
}
