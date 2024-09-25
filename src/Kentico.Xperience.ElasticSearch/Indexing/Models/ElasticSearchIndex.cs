using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

namespace Kentico.Xperience.ElasticSearch.Indexing.Models;

/// <summary>
/// Represents the configuration of an ElasticSearch index.
/// </summary>
public sealed class ElasticSearchIndex
{
    /// <summary>
    /// An arbitrary ID used to identify the ElasticSearch index in the admin UI.
    /// </summary>
    public int Identifier { get; set; }

    /// <summary>
    /// The code name of the ElasticSearch index.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The Name of the WebSiteChannel.
    /// </summary>
    public string WebSiteChannelName { get; }

    /// <summary>
    /// The Language used on the WebSite on the Channel which is indexed.
    /// </summary>
    public List<string> LanguageNames { get; }

    /// <summary>
    /// A list of reusable content types that will be indexed.
    /// </summary>
    public List<string> IncludedReusableContentTypes { get; }

    /// <summary>
    /// The type of the class which extends <see cref="ElasticSearchIndexingStrategyType"/>.
    /// </summary>
    public Type ElasticSearchIndexingStrategyType { get; }

    internal IEnumerable<ElasticSearchIndexIncludedPath> IncludedPaths { get; set; }

    internal ElasticSearchIndex(ElasticSearchConfigurationModel indexConfiguration, Dictionary<string, Type> strategies)
    {
        Identifier = indexConfiguration.Id;
        IndexName = indexConfiguration.IndexName;
        WebSiteChannelName = indexConfiguration.ChannelName;
        LanguageNames = indexConfiguration.LanguageNames.ToList();
        IncludedPaths = indexConfiguration.Paths;
        IncludedReusableContentTypes = indexConfiguration.ReusableContentTypeNames.ToList();

        var strategy = typeof(BaseElasticSearchIndexingStrategy<BaseElasticSearchModel>);

        if (strategies.ContainsKey(indexConfiguration.StrategyName))
        {
            strategy = strategies[indexConfiguration.StrategyName];
        }

        ElasticSearchIndexingStrategyType = strategy;
    }
}
