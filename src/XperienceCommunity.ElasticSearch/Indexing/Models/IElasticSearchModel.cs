namespace XperienceCommunity.ElasticSearch.Indexing.Models;

/// <summary>
/// Abstraction of properties used to define, create and retrieve data from elastic search index.
/// </summary>
public interface IElasticSearchModel
{
    string? Url { get; set; }
    string ContentTypeName { get; set; }
    string LanguageName { get; set; }
    string ItemGuid { get; set; }
    string ObjectID { get; set; }
    string Name { get; set; }

    /// <summary>
    /// Returns identifier, which is used to compare documents in search
    /// </summary>
    /// <returns></returns>
    string GetId();
}
