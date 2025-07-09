using CMS.Websites;

namespace XperienceCommunity.ElasticSearch.Indexing.Models;

/// <summary>
/// Represents a modification to a web page
/// </summary>
public class IndexEventWebPageItemModel : IIndexEventItemModel
{
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemID"/> 
    /// </summary>
    public int ItemID { get; set; }

    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }

    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemName"/>
    /// </summary>
    public string Name { get; set; }
    public string LanguageName { get; set; }
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }
    public string ContentTypeName { get; set; }
    public int ContentLanguageID { get; set; }

    public string WebsiteChannelName { get; set; }
    public string WebPageItemTreePath { get; set; }
    public int? ParentID { get; set; }
    public int Order { get; set; }

    public IndexEventWebPageItemModel(
        int itemID,
        Guid itemGuid,
        string languageName,
        string contentTypeName,
        string name,
        bool isSecured,
        int contentTypeID,
        int contentLanguageID,
        string websiteChannelName,
        string webPageItemTreePath,
        int order,
        int? parentID = null)
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        Name = name;
        LanguageName = languageName;
        IsSecured = isSecured;

        ContentTypeID = contentTypeID;
        ContentTypeName = contentTypeName;
        ContentLanguageID = contentLanguageID;

        WebsiteChannelName = websiteChannelName;
        WebPageItemTreePath = webPageItemTreePath;
        ParentID = parentID;
        Order = order;
    }
}
