using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Aliasing;

using Kentico.Xperience.ElasticSearch.Indexing;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchClients;
using Kentico.Xperience.ElasticSearch.Indexing.SearchTasks;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;
using Kentico.Xperience.ElasticSearch.Search;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Nest;

namespace Microsoft.Extensions.DependencyInjection;

public static class ElasticSearchStartupExtensions
{
    /// <summary>
    /// Adds Elastic search services and custom module to application using the <see cref="BaseElasticSearchIndexingStrategy{BaseElasticSearchModel}"/> for all indexes
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddKenticoElasticSearch(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddElasticSearchServicesInternal(configuration);

        return serviceCollection;
    }

    /// <summary>
    /// Adds ElasticSearch services and custom module to application with customized options provided by the <see cref="IElasticSearchBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddKenticoElasticSearch(this IServiceCollection serviceCollection, Action<IElasticSearchBuilder> configure, IConfiguration configuration)
    {
        serviceCollection.AddElasticSearchServicesInternal(configuration);

        var builder = new ElasticSearchBuilder(serviceCollection);

        configure(builder);

        if (builder.IncludeDefaultStrategy)
        {
            serviceCollection.AddTransient<BaseElasticSearchIndexingStrategy<BaseElasticSearchModel>>();
            builder.RegisterStrategy<BaseElasticSearchIndexingStrategy<BaseElasticSearchModel>, BaseElasticSearchModel>("Default");
        }

        return serviceCollection;
    }

    private static IServiceCollection AddElasticSearchServicesInternal(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<ElasticSearchOptions>(configuration.GetSection(ElasticSearchOptions.CMS_ELASTIC_SEARCH_SECTION_NAME))
            .AddSingleton<ElasticSearchModuleInstaller>()
            .AddSingleton(x =>
            {
                var options = x.GetRequiredService<IOptions<ElasticSearchOptions>>();
                var settings = new ConnectionSettings(new Uri(options.Value.SearchServiceEndPoint))
                    .BasicAuthentication(options.Value.SearchServiceUsername, options.Value.SearchServicePassword)
                    .DisableDirectStreaming()  // Enable detailed logging of request/response
                    .PrettyJson();
                return new ElasticClient(settings);
            })
            .AddSingleton<IElasticSearchQueryClientService>(x =>
            {
                var options = x.GetRequiredService<IOptions<ElasticSearchOptions>>();
                return new ElasticSearchQueryClientService(options.Value);
            })
            .AddSingleton<IElasticSearchClient, DefaultElasticSearchClient>()
            .AddSingleton<IElasticSearchTaskLogger, DefaultElasticSearchTaskLogger>()
            .AddSingleton<IElasticSearchTaskProcessor, DefaultElasticSearchTaskProcessor>()
            .AddSingleton<IElasticSearchConfigurationStorageService, DefaultElasticSearchConfigurationStorageService>()
            .AddSingleton<IElasticSearchIndexClientService, ElasticSearchIndexClientService>()
            .AddSingleton<IElasticSearchIndexAliasService, ElasticSearchIndexAliasService>();
}

public interface IElasticSearchBuilder
{
    /// <summary>
    /// Registers the given <typeparamref name="TStrategy" /> as a transient service under <paramref name="strategyName" />
    /// </summary>
    /// <typeparam name="TStrategy">The custom type of <see cref="IElasticSearchIndexingStrategy"/> </typeparam>
    /// <typeparam name="TSearchModel">The custom rype of <see cref="IElasticSearchModel"/> used to create and use an index.</typeparam>
    /// <param name="strategyName">Used internally <typeparamref name="TStrategy" /> to enable dynamic assignment of strategies to search indexes. Names must be unique.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if an strategy has already been registered with the given <paramref name="strategyName"/>
    /// </exception>
    /// <returns></returns>
    IElasticSearchBuilder RegisterStrategy<TStrategy, TSearchModel>(string strategyName) where TStrategy : BaseElasticSearchIndexingStrategy<TSearchModel> where TSearchModel : class, IElasticSearchModel, new();
}

internal class ElasticSearchBuilder(IServiceCollection serviceCollection) : IElasticSearchBuilder
{
    /// <summary>
    /// If true, the <see cref="BaseElasticSearchIndexingStrategy{BaseElasticSearchModel}" /> will be available as an explicitly selectable indexing strategy
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    public bool IncludeDefaultStrategy { get; set; } = true;

    /// <summary>
    /// Registers the <see cref="IElasticSearchIndexingStrategy"/> strategy <typeparamref name="TStrategy" /> in DI and
    /// as a selectable strategy in the Admin UI
    /// </summary>
    /// <typeparam name="TStrategy"></typeparam>
    /// <typeparam name="TSearchModel">The custom rype of <see cref="IElasticSearchModel"/> used to create and use an index.</typeparam>
    /// <param name="strategyName"></param>
    /// <returns></returns>
    public IElasticSearchBuilder RegisterStrategy<TStrategy, TSearchModel>(string strategyName) where TStrategy : BaseElasticSearchIndexingStrategy<TSearchModel> where TSearchModel : class, IElasticSearchModel, new()
    {
        StrategyStorage.AddStrategy<TStrategy>(strategyName);
        serviceCollection.AddTransient<TStrategy>();

        return this;
    }
}
