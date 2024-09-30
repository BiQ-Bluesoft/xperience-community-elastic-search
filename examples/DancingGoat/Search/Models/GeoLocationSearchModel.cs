using Kentico.Xperience.ElasticSearch.Indexing.Models;

using Nest;

namespace DancingGoat.Search.Models;

[ElasticsearchType(IdProperty = nameof(base.ItemGuid))]
public class GeoLocationSearchModel : BaseElasticSearchModel
{
    [Text(Name = "title")]
    public string Title { get; set; }

    [Text(Name = "content")]
    public string Content { get; set; }

    [GeoPoint(Name = "geo_location")]
    public GeoLocation GeoLocation { get; set; }

    [Keyword(Name = "location")]
    public string Location { get; set; }
}
