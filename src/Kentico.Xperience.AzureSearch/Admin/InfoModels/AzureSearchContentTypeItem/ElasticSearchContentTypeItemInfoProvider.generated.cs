using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchContentTypeItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchContentTypeItemInfoProvider))]
public partial class ElasticSearchContentTypeItemInfoProvider : AbstractInfoProvider<ElasticSearchContentTypeItemInfo, ElasticSearchContentTypeItemInfoProvider>, IElasticSearchContentTypeItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchContentTypeItemInfoProvider"/> class.
    /// </summary>
    public ElasticSearchContentTypeItemInfoProvider()
        : base(ElasticSearchContentTypeItemInfo.TYPEINFO)
    {
    }
}
