using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchIndexItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchIndexItemInfoProvider))]
public partial class ElasticSearchIndexItemInfoProvider : AbstractInfoProvider<ElasticSearchIndexItemInfo, ElasticSearchIndexItemInfoProvider>, IElasticSearchIndexItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchIndexItemInfoProvider"/> class.
    /// </summary>
    public ElasticSearchIndexItemInfoProvider()
        : base(ElasticSearchIndexItemInfo.TYPEINFO)
    {
    }
}
