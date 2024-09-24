using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Declares members for <see cref="ElasticSearchContentTypeItemInfo"/> management.
/// </summary>
public partial interface IElasticSearchContentTypeItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
