using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Declares members for <see cref="ElasticSearchIndexItemInfo"/> management.
/// </summary>
public partial interface IElasticSearchIndexItemInfoProvider : IInfoProvider<ElasticSearchIndexItemInfo>, IInfoByIdProvider<ElasticSearchIndexItemInfo>, IInfoByNameProvider<ElasticSearchIndexItemInfo>
{
}
