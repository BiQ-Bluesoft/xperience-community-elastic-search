using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using XperienceCommunity.ElasticSearch.Indexing;

namespace XperienceCommunity.ElasticSearch.Search;

/// <inheritdoc />
public sealed class ElasticSearchQueryClientService(ElasticSearchOptions options) : IElasticSearchQueryClientService
{
    public ElasticsearchClient CreateSearchClientForQueries(string indexName)
    {
        var settings = new ElasticsearchClientSettings(new Uri(options.SearchServiceEndPoint))
            .DefaultIndex(indexName);
        settings = !string.IsNullOrEmpty(options.SearchServiceAPIKey)
            ? settings
                .Authentication(new ApiKey(options.SearchServiceAPIKey))
            : settings
                .Authentication(new BasicAuthentication(options.SearchServiceUsername, options.SearchServicePassword));
        return new ElasticsearchClient(settings);
    }
}
