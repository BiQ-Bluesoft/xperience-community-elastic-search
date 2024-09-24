using DancingGoat.Search.Services;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

[Route("[controller]")]
[ApiController]
public class SearchController(DancingGoatSearchService searchService) : Controller
{
    public async Task<IActionResult> Index(string? query, int? pageSize, int? page, string? indexName)
    {
        var results = await searchService.GlobalSearch(indexName ?? "advanced", query, page ?? 1, pageSize ?? 10);
        results.Endpoint = nameof(Index);

        return View(results);
    }

    [HttpGet(nameof(Geo))]
    public async Task<IActionResult> Geo(string? query, double? latitude, double? longitude, bool? sortByDistance, int? pageSize, int? page, string? indexName)
    {
        var results = await searchService.GeoSearch(indexName ?? "geo", query, latitude ?? 0, longitude ?? 0, sortByDistance ?? true, page ?? 1, pageSize ?? 10);
        results.Endpoint = nameof(Geo);

        return View("~/Views/Search/GeoSearch.cshtml", results);
    }

    [HttpGet(nameof(Simple))]
    public async Task<IActionResult> Simple(string? query, int? pageSize, int? page, string? indexName)
    {
        var results = await searchService.SimpleSearch(indexName ?? "simple", query ?? "", page ?? 1, pageSize ?? 10);
        results.Endpoint = nameof(Simple);

        return View("~/Views/Search/Index.cshtml", results);
    }
}
