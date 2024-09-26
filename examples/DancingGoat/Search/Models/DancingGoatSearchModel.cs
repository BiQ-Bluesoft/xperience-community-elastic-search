using Kentico.Xperience.ElasticSearch.Indexing.Models;

using Nest;

namespace DancingGoat.Search.Models;

[ElasticsearchType(IdProperty = nameof(base.ItemGuid))]
public class DancingGoatSearchModel : BaseElasticSearchModel
{
    [Text]
    public string Content { get; set; }

    [Text]
    public string Title { get; set; }
}
