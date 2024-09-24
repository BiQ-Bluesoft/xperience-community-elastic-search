using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

public partial interface IElasticSearchIndexAliasIndexItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
