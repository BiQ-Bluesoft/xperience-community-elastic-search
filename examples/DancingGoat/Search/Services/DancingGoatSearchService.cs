using DancingGoat.Search.Models;

using Kentico.Xperience.ElasticSearch.Search;

using Nest;

namespace DancingGoat.Search.Services;

public class DancingGoatSearchService(IElasticSearchQueryClientService searchClientService)
{
    public async Task<DancingGoatSearchViewModel> GlobalSearch(
        string indexName,
        string searchText,
        int page = 1,
        int pageSize = 10)
    {
        var index = searchClientService.CreateSearchClientForQueries(indexName);

        page = Math.Max(page, 1);
        pageSize = Math.Max(1, pageSize);

        var response = await index.SearchAsync<DancingGoatSearchModel>(s => s
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Source(src => src
                .Includes(i => i
                    .Fields(f => f.Title, f => f.Url)))
            .Query(q => q
                .MultiMatch(mm => mm
                    .Fields(f => f
                        .Field(p => p.Title)
                        .Field(p => p.Url))
                    .Query(searchText))));

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

    public async Task<DancingGoatSearchViewModel> SimpleSearch(
        string indexName,
        string searchText,
        int page = 1,
        int pageSize = 10)
    {
        var index = searchClientService.CreateSearchClientForQueries(indexName);

        page = Math.Max(page, 1);
        pageSize = Math.Max(1, pageSize);

        var response = await index.SearchAsync<DancingGoatSimpleSearchModel>(s => s
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Source(src => src
                .Includes(i => i
                    .Fields(f => f.Title, fields => fields.Url)))
            .Query(q => q
                .MultiMatch(mm => mm
                    .Fields(f => f.Field(p => p.Title).Field(p => p.Url))
                    .Query(searchText)))
            .TrackTotalHits(true));

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

    public async Task<GeoLocationSearchViewModel> GeoSearch(
        string indexName,
        string searchText,
        double latitude,
        double longitude,
        bool sortByDistance = true,
        int page = 1,
        int pageSize = 10
        )
    {
        var index = searchClientService.CreateSearchClientForQueries(indexName);

        page = Math.Max(page, 1);
        pageSize = Math.Max(1, pageSize);

        var response = await index.SearchAsync<GeoLocationSearchModel>(s => s
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Source(src => src
                .Includes(i => i
                    .Fields(f => f.Title, f => f.Url, f => f.Location)))
            .Query(q => q
                .MultiMatch(mm => mm
                    .Fields(f => f
                        .Field(p => p.Title)
                        .Field(p => p.Url))
                    .Query(searchText)))
            .TrackTotalHits(true)
            .Sort(sort =>
            {
                if (sortByDistance)
                {
                    sort.GeoDistance(g => g
                        .Field(p => p.GeoLocation)
                        .DistanceType(GeoDistanceType.Arc)
                        .Order(SortOrder.Ascending)
                        .Points(new GeoLocation(latitude, longitude)));
                }
                return sort;
            }));

        return new GeoLocationSearchViewModel
        {
            Hits = response.Hits.Select(x => new GeoLocationSearchResult()
            {
                Title = x.Source.Title,
                Url = x.Source.Url,
                Location = x.Source.Location,
            }),
            TotalHits = (int)response.Total,
            Query = searchText,
            TotalPages = (int)response.Total <= 0 ? 0 : (((int)response.Total - 1) / pageSize) + 1,
            PageSize = pageSize,
            Page = page
        };
    }
}
