using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing.Models;

/// <summary>
/// Base implementation of <see cref="IElasticSearchModel"/> with decorators used to specify properties of indexed columns.
/// </summary>
public class BaseElasticSearchModel : IElasticSearchModel
{
    [Text]
    public string? Url { get; set; } = string.Empty;

    [Text]
    public string ContentTypeName { get; set; } = string.Empty;

    [Text]
    public string LanguageName { get; set; } = string.Empty;

    [Keyword]
    public string ItemGuid { get; set; } = string.Empty;

    [Keyword]
    public string ObjectID { get; set; } = string.Empty;

    [Text]
    public string Name { get; set; } = string.Empty;
}
