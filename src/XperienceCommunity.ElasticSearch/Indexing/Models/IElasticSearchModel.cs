namespace XperienceCommunity.ElasticSearch.Indexing.Models;

/// <summary>
/// Abstraction of properties used to define, create and retrieve data from elastic search index.
/// </summary>
public interface IElasticSearchModel
{
    public string? Url { get; set; }
    public string ContentTypeName { get; set; }
    public string LanguageName { get; set; }
    public string ItemGuid { get; set; }
    public string ObjectID { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Returns identifier, which is used to compare documents in search
    /// </summary>
    /// <returns></returns>
    string GetId();
}
