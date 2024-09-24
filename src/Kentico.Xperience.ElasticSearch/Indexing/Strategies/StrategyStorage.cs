using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace Kentico.Xperience.ElasticSearch.Indexing.Strategies;

internal static class StrategyStorage
{
    public static Dictionary<string, Type> Strategies { get; private set; }

    static StrategyStorage() => Strategies = [];

    public static void AddStrategy<TStrategy>(string strategyName) where TStrategy : IElasticSearchIndexingStrategy
        => Strategies.Add(strategyName, typeof(TStrategy));

    public static Type GetOrDefault(string strategyName) =>
        Strategies.TryGetValue(strategyName, out var type)
            ? type
            : typeof(BaseElasticSearchIndexingStrategy<BaseElasticSearchModel>);
}
