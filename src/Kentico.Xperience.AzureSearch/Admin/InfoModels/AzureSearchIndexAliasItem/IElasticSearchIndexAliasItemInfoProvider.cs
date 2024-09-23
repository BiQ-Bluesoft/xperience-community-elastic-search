using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

public partial interface IElasticSearchIndexAliasItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
