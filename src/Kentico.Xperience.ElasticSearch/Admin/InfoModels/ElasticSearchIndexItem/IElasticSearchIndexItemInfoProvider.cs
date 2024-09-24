using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

public partial interface IElasticSearchIndexItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
