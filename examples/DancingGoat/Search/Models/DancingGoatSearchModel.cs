using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace DancingGoat.Search.Models;

public class DancingGoatSearchModel : BaseElasticSearchModel
{
    public string Content { get; set; }

    public string Title { get; set; }
}
