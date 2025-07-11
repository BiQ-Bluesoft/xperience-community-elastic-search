using Microsoft.Extensions.DependencyInjection;

using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.Strategies;

namespace XperienceCommunity.ElasticSearch;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Returns an instance of the <see cref="IElasticSearchIndexingStrategy"/> assigned to the given <paramref name="index" />.
    /// Used to generate instances of a <see cref="IElasticSearchIndexingStrategy"/> service type that can change at runtime.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="index"></param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the assigned <see cref="IElasticSearchIndexingStrategy"/> cannot be instantiated.
    ///     This shouldn't normally occur because we fallback to <see cref="BaseElasticSearchIndexingStrategy{DefaultElasticSearchModel}" /> if not custom strategy is specified.
    ///     However, incorrect dependency management in user-code could cause issues.
    /// </exception>
    /// <returns></returns>
    public static IElasticSearchIndexingStrategy GetRequiredStrategy(this IServiceProvider serviceProvider, ElasticSearchIndex index)
    {
        var strategy = serviceProvider.GetRequiredService(index.ElasticSearchIndexingStrategyType) as IElasticSearchIndexingStrategy;

        return strategy!;
    }
}
