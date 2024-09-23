using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchIndexAliasIndexItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchIndexAliasIndexItemInfoProvider))]
public partial class ElasticSearchIndexAliasIndexItemInfoProvider : AbstractInfoProvider<ElasticSearchIndexAliasIndexItemInfo, ElasticSearchIndexAliasIndexItemInfoProvider>, IElasticSearchIndexAliasIndexItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchIndexAliasIndexItemInfoProvider"/> class.
    /// </summary>
    public ElasticSearchIndexAliasIndexItemInfoProvider()
        : base(ElasticSearchIndexAliasIndexItemInfo.TYPEINFO)
    {
    }
}
