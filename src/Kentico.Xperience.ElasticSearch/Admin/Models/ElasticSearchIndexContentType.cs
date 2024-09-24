namespace Kentico.Xperience.ElasticSearch.Admin.Models;

public class ElasticSearchIndexContentType
{
    /// <summary>
    /// Name of the indexed content type for an indexed path
    /// </summary>
    public string ContentTypeName { get; set; } = "";

    /// <summary>
    /// Displayed name of the indexed content type for an indexed path which will be shown in admin UI
    /// </summary>
    public string ContentTypeDisplayName { get; set; } = "";

    public ElasticSearchIndexContentType()
    { }

    public ElasticSearchIndexContentType(string className, string classDisplayName)
    {
        ContentTypeName = className;
        ContentTypeDisplayName = classDisplayName;
    }
}
