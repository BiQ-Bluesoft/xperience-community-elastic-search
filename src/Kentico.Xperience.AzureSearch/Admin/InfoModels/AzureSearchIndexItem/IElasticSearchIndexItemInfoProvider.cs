using CMS.DataEngine;

namespace Kentico.Xperience.AzureSearch.Admin;

public partial interface IElasticSearchIndexItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
