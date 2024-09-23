using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Declares members for <see cref="ElasticSearchIndexItemInfo"/> management.
/// </summary>
public partial interface IElasticSearchIndexItemInfoProvider : IInfoProvider<ElasticSearchIndexItemInfo>, IInfoByIdProvider<ElasticSearchIndexItemInfo>, IInfoByNameProvider<ElasticSearchIndexItemInfo>
{
}
