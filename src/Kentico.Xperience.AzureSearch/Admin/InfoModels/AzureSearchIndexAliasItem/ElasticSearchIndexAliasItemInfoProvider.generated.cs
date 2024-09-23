using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchIndexAliasItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchIndexAliasItemInfoProvider))]
public partial class ElasticSearchIndexAliasItemInfoProvider : AbstractInfoProvider<ElasticSearchIndexAliasItemInfo, ElasticSearchIndexAliasItemInfoProvider>, IElasticSearchIndexAliasItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchIndexAliasItemInfoProvider"/> class.
    /// </summary>
    public ElasticSearchIndexAliasItemInfoProvider()
        : base(ElasticSearchIndexAliasItemInfo.TYPEINFO)
    {
    }
}
