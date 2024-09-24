using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.ElasticSearch.Admin;

public class ElasticSearchConfigurationModel
{
    public int Id { get; set; }

    [TextInputComponent(
        Label = "Index Name",
        Order = 1)]
    [Required]
    [MinLength(1)]
    [RegularExpression("^(?!-)[a-z0-9-]+(?<!-)$", ErrorMessage = "Index name must only contain lowercase letters, digits or dashes, cannot start or end with dashes and is limited to 128 characters.")]
    public string IndexName { get; set; } = "";

    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 2)]
    public IEnumerable<string> LanguageNames { get; set; } = [];

    [DropDownComponent(Label = "Channel Name", DataProviderType = typeof(ChannelOptionsProvider), Order = 3)]
    public string ChannelName { get; set; } = "";

    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 4, ExplanationText = "Changing strategy which has an incompatible configuration will result in deleting indexed items.")]
    public string StrategyName { get; set; } = "";

    [TextInputComponent(Label = "Rebuild Hook")]
    public string RebuildHook { get; set; } = "";

    [ElasticSearchIndexConfigurationComponent(Label = "Included Paths")]
    public IEnumerable<ElasticSearchIndexIncludedPath> Paths { get; set; } = [];

    public ElasticSearchConfigurationModel() { }

    public ElasticSearchConfigurationModel(
        ElasticSearchIndexItemInfo index,
        IEnumerable<ElasticSearchIndexLanguageItemInfo> indexLanguages,
        IEnumerable<ElasticSearchIncludedPathItemInfo> indexPaths,
        IEnumerable<ElasticSearchIndexContentType> contentTypes
    )
    {
        Id = index.ElasticSearchIndexItemId;
        IndexName = index.ElasticSearchIndexItemIndexName;
        ChannelName = index.ElasticSearchIndexItemChannelName;
        RebuildHook = index.ElasticSearchIndexItemRebuildHook;
        StrategyName = index.ElasticSearchIndexItemStrategyName;
        LanguageNames = indexLanguages
            .Where(l => l.ElasticSearchIndexLanguageItemIndexItemId == index.ElasticSearchIndexItemId)
            .Select(l => l.ElasticSearchIndexLanguageItemName)
            .ToList();
        Paths = indexPaths
            .Where(p => p.ElasticSearchIncludedPathItemIndexItemId == index.ElasticSearchIndexItemId)
            .Select(p => new ElasticSearchIndexIncludedPath(p, contentTypes))
            .ToList();
    }
}
