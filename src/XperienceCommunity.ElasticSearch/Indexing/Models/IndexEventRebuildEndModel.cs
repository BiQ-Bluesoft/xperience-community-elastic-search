namespace XperienceCommunity.ElasticSearch.Indexing.Models;

/// <summary>
/// Represents end of rebuild event
/// </summary>
public class IndexEventRebuildEndModel : IIndexEventItemModel
{
    public int ItemID { get; set; }
    public Guid ItemGuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public int ContentLanguageID { get; set; }

    public string? CurrentElasticIndexName { get; private set; }
    public string NewElasticIndexName { get; private set; }
    public string IndexName { get; private set; }

    public IndexEventRebuildEndModel(
        string? currentElasticIndexName,
        string newElasticIndexName,
        string indexName)
    {
        CurrentElasticIndexName = currentElasticIndexName;
        NewElasticIndexName = newElasticIndexName;
        IndexName = indexName;
    }
}
