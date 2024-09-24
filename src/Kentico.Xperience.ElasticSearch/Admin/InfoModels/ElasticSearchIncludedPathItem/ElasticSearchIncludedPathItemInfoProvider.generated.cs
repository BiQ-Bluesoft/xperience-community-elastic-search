using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchIncludedPathItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchIncludedPathItemInfoProvider))]
public partial class ElasticSearchIncludedPathItemInfoProvider : AbstractInfoProvider<ElasticSearchIncludedPathItemInfo, ElasticSearchIncludedPathItemInfoProvider>, IElasticSearchIncludedPathItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchIncludedPathItemInfoProvider"/> class.
    /// </summary>
    public ElasticSearchIncludedPathItemInfoProvider()
        : base(ElasticSearchIncludedPathItemInfo.TYPEINFO)
    {
    }
}
