namespace XperienceCommunity.ElasticSearch.Admin.Models;

public class ElasticSearchIndexContentType
{
    /// <summary>
    /// Name of the indexed content type for an indexed path
    /// </summary>
    public string ContentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Displayed name of the indexed content type for an indexed path which will be shown in admin UI
    /// </summary>
    public string ContentTypeDisplayName { get; set; } = string.Empty;

    public ElasticSearchIndexContentType()
    { }

    public ElasticSearchIndexContentType(string className, string classDisplayName)
    {
        ContentTypeName = className;
        ContentTypeDisplayName = classDisplayName;
    }
}
