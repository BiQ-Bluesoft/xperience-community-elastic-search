using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;

using XperienceCommunity.ElasticSearch.Admin.Components;
using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchIncludedPathItem;
using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchIndexItem;
using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchIndexLanguageItem;
using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchReusableContentItem;
using XperienceCommunity.ElasticSearch.Admin.Providers;

namespace XperienceCommunity.ElasticSearch.Admin.Models;

public class ElasticSearchConfigurationModel
{
    public int Id { get; set; }

    [TextInputComponent(Label = "Index Name", Order = 1)]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    [RegularExpression("^(?!-)[a-z0-9-]+(?<!-)$", ErrorMessage = "Index name must only contain lowercase letters, digits or dashes, cannot start or end with dashes and is limited to 128 characters.")]
    public string IndexName { get; set; } = string.Empty;

    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 2)]
    [MinLength(1, ErrorMessage = "You must select at least one Language name")]
    public IEnumerable<string> LanguageNames { get; set; } = [];

    [DropDownComponent(Label = "Channel Name", DataProviderType = typeof(ChannelOptionsProvider), Order = 3)]
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 4, ExplanationText = "Changing strategy which has an incompatible configuration will result in deleting indexed items.")]
    [Required]
    public string StrategyName { get; set; } = string.Empty;

    [TextInputComponent(Label = "Rebuild Hook")]
    public string RebuildHook { get; set; } = string.Empty;

    [ElasticSearchIndexConfigurationComponent(Label = "Included Paths")]
    public IEnumerable<ElasticSearchIndexIncludedPath> Paths { get; set; } = [];

    [GeneralSelectorComponent(dataProviderType: typeof(ReusableContentOptionsProvider), Label = "Included Reusable Content Types", Order = 3)]
    public IEnumerable<string> ReusableContentTypeNames { get; set; } = [];

    public DateTime? LastRebuild { get; set; }

    public ElasticSearchConfigurationModel() { }

    public ElasticSearchConfigurationModel(
        ElasticSearchIndexItemInfo index,
        IEnumerable<ElasticSearchIndexLanguageItemInfo> indexLanguages,
        IEnumerable<ElasticSearchIncludedPathItemInfo> indexPaths,
        IEnumerable<ElasticSearchIndexContentType> contentTypes,
        IEnumerable<ElasticSearchReusableContentTypeItemInfo> reusableContentTypes
    )
    {
        Id = index.ElasticSearchIndexItemId;
        IndexName = index.ElasticSearchIndexItemIndexName;
        ChannelName = index.ElasticSearchIndexItemChannelName;
        RebuildHook = index.ElasticSearchIndexItemRebuildHook;
        StrategyName = index.ElasticSearchIndexItemStrategyName;
        LastRebuild = index.ElasticSearchIndexItemLastRebuild;
        ReusableContentTypeNames = reusableContentTypes
             .Where(c => c.ElasticSearchReusableContentTypeItemIndexItemId == index.ElasticSearchIndexItemId)
             .Select(c => c.ElasticSearchReusableContentTypeItemContentTypeName)
             .ToList();
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
