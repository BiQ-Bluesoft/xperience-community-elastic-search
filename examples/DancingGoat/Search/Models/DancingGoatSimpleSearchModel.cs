using XperienceCommunity.ElasticSearch.Indexing.Models;

namespace DancingGoat.Search.Models;

public class DancingGoatSimpleSearchModel : BaseElasticSearchModel
{
    public string Title { get; set; }
}
