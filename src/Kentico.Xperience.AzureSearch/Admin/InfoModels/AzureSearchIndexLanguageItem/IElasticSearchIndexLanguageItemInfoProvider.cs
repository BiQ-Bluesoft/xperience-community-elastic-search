using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

/// <summary>
/// Declares members for <see cref="ElasticSearchIndexLanguageItemInfo"/> management.
/// </summary>
public partial interface IElasticSearchIndexLanguageItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
