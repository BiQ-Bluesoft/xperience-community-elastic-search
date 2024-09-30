using Kentico.Xperience.ElasticSearch.Indexing;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Search;

/// <inheritdoc />
public sealed class ElasticSearchQueryClientService(ElasticSearchOptions settings) : IElasticSearchQueryClientService
{
    public ElasticClient CreateSearchClientForQueries(string indexName)
    {
        var elasticSettings = new ConnectionSettings(new Uri(settings.SearchServiceEndPoint))
            .DefaultIndex(indexName)
            .BasicAuthentication(settings.SearchServiceUsername, settings.SearchServicePassword);

        return new ElasticClient(elasticSettings);
    }
}
