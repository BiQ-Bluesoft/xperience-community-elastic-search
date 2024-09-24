using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

public partial interface IElasticSearchIncludedPathItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
