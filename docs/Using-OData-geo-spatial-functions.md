# Use OData geo-spatial functions in Elastic AI Search

Elastic AI Search supports geo-spatial queries in OData filter expressions via the geo_distance and geo_polygon functions. The geo_distance query returns documents that are within a certain distance from a given point. The geo_polygon query returns documents that are within a given polygon, where the point is defined as a field in the document.

## Create a strategy

Elastic AI Search supports various queries in OData filter expressions. We can create a custom `BaseElasticSearchIndexingStrategy` and `GeoLocationSearchModel` to implement geo-spatial queries. 

```csharp
public class GeoLocationSearchModel : BaseElasticSearchModel
{
    [Text]
    public string Title { get; set; }

    [GeoPoint]
    public GeoLocation GeoLocation { get; set; }

    [Text]
    public string Location { get; set; }
}
```

Let's say we specified `CafePage` in the admin UI.
Now we implement how we want to save CafePage page in our strategy.

```csharp
public class GeoLocationSearchStrategy : BaseElasticSearchIndexingStrategy<GeoLocationSearchModel>
{
    private readonly StrategyHelper strategyHelper;

    public GeoLocationSearchStrategy(StrategyHelper strategyHelper) => this.strategyHelper = strategyHelper;

    public override async Task<IElasticSearchModel> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
    {
        var result = new GeoLocationSearchModel();

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is IndexEventWebPageItemModel indexedPage)
        {
            if (string.Equals(item.ContentTypeName, CafePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
            {
                // The implementation of GetPage<T>() is below
                var page = await strategyHelper.GetPage<CafePage>(
                    indexedPage.ItemGuid,
                    indexedPage.WebsiteChannelName,
                    indexedPage.LanguageName,
                    CafePage.CONTENT_TYPE_NAME);

                if (page is null)
                {
                    return null;
                }

                result.Title = page?.CafeTitle ?? "";
                result.Location = page?.CafeLocation ?? "";

                //We can use this value later to sort by distance from the user accessing our search page.
                //Example for this scenario is shown in DancingGoatSearchService.GeoSearch
                result.GeoLocation = new GeoLocation((double)page.CafeLocationLatitude, (double)page.CafeLocationLongitude);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        return result;
    }
}

```

## Create a service which uses Geo Location

Add an OrderBy option to query.

```csharp
public class GeoSearchService
{
    private readonly IElasticSearchQueryClientService searchClientService;

    public GeoSearchService(IElasticSearchQueryClientService searchClientService) => this.searchClientService = searchClientService;

    public async Task<GeoLocationSearchViewModel> GeoSearch(
        string indexName,
        string searchText,
        double latitude,
        double longitude,
        bool sortByDistance = true,
        int page = 1,
        int pageSize = 10)
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
```

Map Retrieved SearchModel data to a more simple `GeoLocationSearchResult`.

```csharp
public class GeoLocationSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
```

Create a ViewModel.

```csharp
public class GeoLocationSearchViewModel
{
    public int TotalHits { get; set; }
    public string Query { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int Page { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string IndexName { get; set; }
    public bool SortByDistance { get; set; } = true;
    public string Endpoint { get; set; }

    public IEnumerable<GeoLocationSearchResult> Hits { get; set; }
}
```

## Display Results

Create a Controller which uses `GeoSearchService` to display view with search bar.

```csharp
[Route("[controller]")]
[ApiController]
public class SearchController : Controller
{
    private readonly GeoSearchService searchService;

    public SearchController(GeoSearchService searchService) => this.searchService = searchService;

    [HttpGet(nameof(Geo))]
    public async Task<IActionResult> Geo(string? query, double? latitude, double? longitude, bool? sortByDistance, int? pageSize, int? page, string? indexName)
    {
        var results = await searchService.GeoSearch(indexName ?? "geo", query, latitude ?? 0, longitude ?? 0, sortByDistance ?? true, page ?? 1, pageSize ?? 10);
        results.Endpoint = nameof(Geo);

        return View("~/Views/Search/GeoSearch.cshtml", results);
    }
}
```

Return a view.

```cshtml
@model GeoLocationSearchViewModel
@{
    Dictionary<string, string> GetRouteData(int page) =>
        new Dictionary<string, string>() { 
            { "query", Model.Query },
            { "pageSize", Model.PageSize.ToString() },
            { "page", page.ToString() },
            { "latitude", Model.Latitude.ToString() },
            { "longitude", Model.Longitude.ToString() },
            { "sortByDistance", Model.SortByDistance.ToString() },
            { "indexName", Model.IndexName }
        };
}

<h1>Search</h1>

<style>
    .form-field {
        margin-bottom: 0.8rem;
    }
</style>

<form asp-action=@Model.Endpoint method="get">
    <div class="row">
        <div class="col-md-12">
            <div class="form-field">
                <label class="control-label" asp-for="@Model.Query"></label>
                <div class="editing-form-control-nested-control">
                    <input class="form-control" asp-for="@Model.Query" name="query">
                    <input type="hidden" asp-for="@Model.PageSize" name="pageSize" />
                    <input type="hidden" asp-for="@Model.Page" name="page" />
                    <input type="hidden" id="latitude" asp-for="@Model.Latitude" name="latitude"/>
                    <input type="hidden" id="longitude" asp-for="@Model.Longitude" name="longitude"/>
                    <input type="hidden" asp-for="@Model.IndexName" name="indexName" />
                </div>
            </div>
        </div>
    </div>
    <label for="sortByDistance">Sort By Distance</label>
    <input type="checkbox" id="sortByDistance" value="true" asp-for="@Model.SortByDistance" name="sortByDistance">
    <input type="submit" value="Submit">
</form>

@if (!Model.Hits.Any())
{
    if (!String.IsNullOrWhiteSpace(Model.Query))
    {
        @HtmlLocalizer["Sorry, no results match {0}", Model.Query]
    }

    return;
}

@foreach (var item in Model.Hits)
{
    <div class="col-sm-12">
        <div class="section-text">
            <h3 class="h4 search-tile-title">
                <a href="@item.Url">@item.Title</a>
            </h3>
            <div>
                @Html.Raw(item.Location)
            </div>
        </div>
    </div>
}

<div class="pagination-container">
    <ul class="pagination">
        @if (Model.Page > 1)
        {
            <li class="PagedList-skipToPrevious">
                <a asp-controller="Search" asp-all-route-data="GetRouteData(Model.Page - 1)">
                    @HtmlLocalizer["previous"]
                </a>
            </li>
        }

        @for (int pageNumber = 1; pageNumber <= Model.TotalPages; pageNumber++)
        {
            if (pageNumber == Model.Page)
            {
                <li class="active">
                    <span>
                        @pageNumber
                    </span>
                </li>
            }
            else
            {
                <li>
                    <a asp-controller="Search" asp-all-route-data="GetRouteData(pageNumber)">@pageNumber</a>
                </li>
            }
        }

        @if (Model.Page < Model.TotalPages)
        {
            <li class="PagedList-skipToNext">
                <a asp-controller="Search" asp-all-route-data="GetRouteData(Model.Page + 1)">
                    @HtmlLocalizer["next"]
                </a>
            </li>
        }
    </ul>
</div>

<script>
    document.addEventListener("DOMContentLoaded", function() {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function(position) {
                document.getElementById('latitude').value = position.coords.latitude;
                document.getElementById('longitude').value = position.coords.longitude;
            });
        } else {
            alert("Geolocation is not supported by this browser.");
        }
    });
</script>
```