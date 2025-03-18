using CMS.ContentEngine;

namespace Kentico.Xperience.ElasticSearch.Indexing.Models;

/// <summary>
/// Represents a modification to a reusable content item
/// </summary>
public class IndexEventReusableItemModel : IIndexEventItemModel
{
    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemID"/>
    /// </summary>
    public int ItemID { get; set; }

    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }

    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemName"/>
    /// </summary>
    public string Name { get; set; }
    public string LanguageName { get; set; }
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }
    public string ContentTypeName { get; set; }
    public int ContentLanguageID { get; set; }

    public IndexEventReusableItemModel(
        int itemID,
        Guid itemGuid,
        string languageName,
        string contentTypeName,
        string name,
        bool isSecured,
        int contentTypeID,
        int contentLanguageID)
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        Name = name;
        LanguageName = languageName;
        IsSecured = isSecured;
        ContentTypeID = contentTypeID;
        ContentTypeName = contentTypeName;
        ContentLanguageID = contentLanguageID;
    }
}
