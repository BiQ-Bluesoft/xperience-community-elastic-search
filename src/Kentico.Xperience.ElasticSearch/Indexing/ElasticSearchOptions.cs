namespace Kentico.Xperience.ElasticSearch.Indexing;

public sealed class ElasticSearchOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string CMS_ELASTIC_SEARCH_SECTION_NAME = "CMSElasticSearch";

    /// <summary>
    /// /// Turn off functionality if application is not configured in the appsettings
    /// </summary>
    public bool SearchServiceEnabled
    {
        get;
        set;
    } = true;

    /// <summary>
    /// Url of elastic search provider.
    /// </summary>
    public string SearchServiceEndPoint
    {
        get;
        set;
    } = "";

    public string SearchServiceUsername
    {
        get;
        set;
    } = "";

    public string SearchServicePassword
    {
        get;
        set;
    } = "";
}
