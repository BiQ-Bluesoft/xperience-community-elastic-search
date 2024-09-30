using Kentico.Xperience.ElasticSearch.Indexing.Models;

using Nest;

namespace DancingGoat.Search.Models;

[ElasticsearchType(IdProperty = nameof(base.ItemGuid))]
public class DancingGoatSimpleSearchModel : BaseElasticSearchModel
{
    [Text(Name = "title")]
    public string Title { get; set; }
}
