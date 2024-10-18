using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Kentico.Xperience.ElasticSearch.Indexing;

namespace Kentico.Xperience.ElasticSearch.Search;

/// <inheritdoc />
public sealed class ElasticSearchQueryClientService(ElasticSearchOptions settings) : IElasticSearchQueryClientService
{
    public ElasticsearchClient CreateSearchClientForQueries(string indexName)
    {
        var elasticSettings = new ElasticsearchClientSettings(new Uri(settings.SearchServiceEndPoint))
            .DefaultIndex(indexName)
            .Authentication(new BasicAuthentication(settings.SearchServiceUsername, settings.SearchServicePassword));

        return new ElasticsearchClient(elasticSettings);
    }
}
