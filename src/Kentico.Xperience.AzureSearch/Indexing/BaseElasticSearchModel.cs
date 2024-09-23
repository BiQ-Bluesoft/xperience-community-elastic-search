using Nest;

namespace Kentico.Xperience.AzureSearch.Indexing;

/// <summary>
/// Base implementation of <see cref="IElasticSearchModel"/> with decorators used to specify properties of indexed columns.
/// </summary>
public class BaseElasticSearchModel : IElasticSearchModel
{
    [Text(Name = "url")]
    public string? Url { get; set; } = string.Empty;

    [Text(Name = "contentTypeName")]
    public string ContentTypeName { get; set; } = string.Empty;

    [Text(Name = "languageName")]
    public string LanguageName { get; set; } = string.Empty;

    [Keyword(Name = "itemGuid")]
    public string ItemGuid { get; set; } = string.Empty;

    [Keyword(Name = "objectId")]
    public string ObjectID { get; set; } = string.Empty;

    [Text(Name = "name")]
    public string Name { get; set; } = string.Empty;
}
