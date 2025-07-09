namespace XperienceCommunity.ElasticSearch.Indexing.Models;

/// <summary>
/// Base implementation of <see cref="IElasticSearchModel"/> with decorators used to specify properties of indexed columns.
/// </summary>
public class BaseElasticSearchModel : IElasticSearchModel
{
    public string? Url { get; set; } = string.Empty;

    public string ContentTypeName { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public string ItemGuid { get; set; } = string.Empty;

    public string ObjectID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public virtual string GetId() => ObjectID;
}
