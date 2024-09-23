using Kentico.Xperience.AzureSearch.Indexing;

using Nest;

namespace Kentico.Xperience.AzureSearch.Search;

/// <inheritdoc />
public sealed class ElasticSearchQueryClientService(ElasticSearchOptions settings) : IElasticSearchQueryClientService
{
    private readonly ElasticSearchOptions settings = settings;

    public ElasticClient CreateSearchClientForQueries(string indexName)
    {
        var elasticSettings = new ConnectionSettings(new Uri(settings.SearchServiceEndPoint))
            .DefaultIndex(indexName)
            .BasicAuthentication(settings.SearchServiceUsername, settings.SearchServicePassword);
        return new ElasticClient(elasticSettings);
    }
}
