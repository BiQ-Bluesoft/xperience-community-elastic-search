using Kentico.Xperience.ElasticSearch.Indexing.Models;

using Nest;

namespace DancingGoat.Search.Models;

public class DancingGoatSimpleSearchModel : BaseElasticSearchModel
{
    [Text(Name = "title")]
    public string Title { get; set; }
}
