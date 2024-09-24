using CMS.DataEngine;

namespace Kentico.Xperience.ElasticSearch.Admin;

public partial interface IElasticSearchIndexAliasItemInfoProvider
{
    void BulkDelete(IWhereCondition where, BulkDeleteSettings? settings = null);
}
