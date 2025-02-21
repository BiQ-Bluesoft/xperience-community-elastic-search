namespace Kentico.Xperience.ElasticSearch.Indexing.Models;

/// <summary>
/// Abstraction of different types of events generated from content modifications
/// </summary>
public interface IIndexEventItemModel
{
    /// <summary>
    /// The identifier of the item
    /// </summary>
    int ItemID { get; set; }
    Guid ItemGuid { get; set; }
    string Name { get; set; }
    string LanguageName { get; set; }
    bool IsSecured { get; set; }
    int ContentTypeID { get; set; }
    string ContentTypeName { get; set; }
    int ContentLanguageID { get; set; }
}
