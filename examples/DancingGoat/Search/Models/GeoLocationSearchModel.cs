using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace DancingGoat.Search.Models;

public class GeoLocationSearchModel : BaseElasticSearchModel
{
    public string Title { get; set; }

    public string Content { get; set; }

    public GeoLocation GeoLocation { get; set; }

    public string Location { get; set; }
}
