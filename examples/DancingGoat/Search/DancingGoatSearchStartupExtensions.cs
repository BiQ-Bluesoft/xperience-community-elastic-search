using DancingGoat.Search.Models;
using DancingGoat.Search.Services;
using DancingGoat.Search.Strategies;

using XperienceCommunity.ElasticSearch;

namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddXperienceCommunityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddXperienceCommunity(builder =>
        {
            builder.RegisterStrategy<DancingGoatSearchStrategy, DancingGoatSearchModel>(nameof(DancingGoatSearchStrategy));
            builder.RegisterStrategy<DancingGoatSimpleSearchStrategy, DancingGoatSimpleSearchModel>(nameof(DancingGoatSimpleSearchStrategy));
            builder.RegisterStrategy<CustomItemsReindexingSearchStrategy, DancingGoatSearchModel>(nameof(CustomItemsReindexingSearchStrategy));
            builder.RegisterStrategy<ReusableContentItemsIndexingStrategy, DancingGoatSearchModel>(nameof(ReusableContentItemsIndexingStrategy));
        }, configuration);

        services.AddTransient<DancingGoatSearchService>();

        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();
        services.AddTransient<StrategyHelper>();

        return services;
    }
}
