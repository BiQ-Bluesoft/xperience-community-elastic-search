namespace Kentico.Xperience.ElasticSearch.Admin;

public class ElasticSearchIndexStatisticsViewModel
{
    /// <summary>
    /// Index name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Number of records contained in the index
    /// </summary>
    public long Entries { get; set; }
}
