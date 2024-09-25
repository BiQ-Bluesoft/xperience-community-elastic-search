using DancingGoat.Search.Models;
using DancingGoat.Search.Services;
using DancingGoat.Search.Strategies;

namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddKenticoElasticSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKenticoElasticSearch(builder =>
        {
            builder.RegisterStrategy<DancingGoatSearchStrategy, DancingGoatSearchModel>(nameof(DancingGoatSearchStrategy));
            builder.RegisterStrategy<DancingGoatSimpleSearchStrategy, DancingGoatSimpleSearchModel>(nameof(DancingGoatSimpleSearchStrategy));
            builder.RegisterStrategy<GeoLocationSearchStrategy, GeoLocationSearchModel>(nameof(GeoLocationSearchStrategy));
            builder.RegisterStrategy<CustomItemsReindexingSearchStrategy, DancingGoatSearchModel>(nameof(CustomItemsReindexingSearchStrategy));
            builder.RegisterStrategy<ReusableContentItemsIndexingStrategy, DancingGoatSearchModel>(nameof(ReusableContentItemsIndexingStrategy));
        }, configuration);

        services.AddTransient<DancingGoatSearchService>();

        services.AddKenticoElasticSearch(configuration);

        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();
        services.AddTransient<StrategyHelper>();

        return services;
    }
}
