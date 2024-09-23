using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Class providing <see cref="ElasticSearchIndexLanguageItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(IElasticSearchIndexLanguageItemInfoProvider))]
public partial class ElasticSearchIndexedLanguageInfoProvider : AbstractInfoProvider<ElasticSearchIndexLanguageItemInfo, ElasticSearchIndexedLanguageInfoProvider>, IElasticSearchIndexLanguageItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticSearchIndexedLanguageInfoProvider"/> class.
    /// </summary>
    public ElasticSearchIndexedLanguageInfoProvider()
        : base(ElasticSearchIndexLanguageItemInfo.TYPEINFO)
    {
    }
}
