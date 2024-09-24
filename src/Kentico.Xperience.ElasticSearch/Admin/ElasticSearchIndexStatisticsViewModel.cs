namespace Kentico.Xperience.ElasticSearch.Admin;

public class ElasticSearchIndexStatisticsViewModel
{
    //
    // Summary:
    //     Index name.
    public string? Name { get; set; }

    //
    // Summary:
    //     Number of records contained in the index
    public long Entries { get; set; }
}
