using Kentico.Xperience.AzureSearch.Indexing;

using Nest;

namespace DancingGoat.Search.Models;

public class DancingGoatSearchModel : BaseElasticSearchModel
{
    [Text(Name = "content")]
    public string Content { get; set; }

    [Text(Name = "title")]
    public string Title { get; set; }
}
