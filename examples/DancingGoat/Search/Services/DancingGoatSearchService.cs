using DancingGoat.Search.Models;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

using Kentico.Xperience.ElasticSearch.Search;

namespace DancingGoat.Search.Services;

public class DancingGoatSearchService(IElasticSearchQueryClientService searchClientService)
{
    public async Task<DancingGoatSearchViewModel> GlobalSearch(string indexName, string searchText, int page = 1, int pageSize = 10)
    {
        var index = searchClientService.CreateSearchClientForQueries(indexName);

        page = Math.Max(page, 1);
        pageSize = Math.Max(1, pageSize);

        var request = new SearchRequest(indexName)
        {
            From = (page - 1) * pageSize,
            Size = pageSize,
            Query = string.IsNullOrEmpty(searchText)
                ? new MatchAllQuery()
                : new MultiMatchQuery()
                {
                    Fields = new[]
                    {
                        nameof(DancingGoatSearchModel.Title).ToLower(),
                    },
                    Query = searchText,
                },
            TrackTotalHits = new TrackHits(true)
        };

        var response = await index.SearchAsync<DancingGoatSearchModel>(request);
        return new DancingGoatSearchViewModel()
        {
            Hits = response.Hits.Select(x => new DancingGoatSearchResult()
            {
                Title = x.Source.Title,
                Url = x.Source.Url,
            }),
            TotalHits = (int)response.Total,
            Query = searchText,
            TotalPages = (int)response.Total <= 0 ? 0 : (((int)response.Total - 1) / pageSize) + 1,
            PageSize = pageSize,
            Page = page
        };
    }

    public async Task<DancingGoatSearchViewModel> SimpleSearch(string indexName, string searchText, int page = 1, int pageSize = 10)
    {
        var index = searchClientService.CreateSearchClientForQueries(indexName);

        page = Math.Max(page, 1);
        pageSize = Math.Max(1, pageSize);

        var request = new SearchRequest(indexName)
        {
            From = (page - 1) * pageSize,
            Size = pageSize,
            Query = string.IsNullOrEmpty(searchText)
                ? new MatchAllQuery()
                : new MultiMatchQuery()
                {
                    Fields = new[]
                    {
                        nameof(DancingGoatSimpleSearchModel.Title).ToLower(),
                        nameof(DancingGoatSimpleSearchModel.Url).ToLower()
                    },
                    Query = searchText,
                },
            TrackTotalHits = new TrackHits(true)
        };

        var response = await index.SearchAsync<DancingGoatSimpleSearchModel>(request);
        return new DancingGoatSearchViewModel()
        {
            Hits = response.Documents.Select(x => new DancingGoatSearchResult()
            {
                Title = x.Title,
                Url = x.Url,
            }),
            TotalHits = (int)response.Total,
            Query = searchText,
            TotalPages = (int)response.Total <= 0 ? 0 : (((int)response.Total - 1) / pageSize) + 1,
            PageSize = pageSize,
            Page = page
        };
    }
}
