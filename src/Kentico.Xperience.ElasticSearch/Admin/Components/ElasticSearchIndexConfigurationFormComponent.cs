using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin.Components;
using Kentico.Xperience.ElasticSearch.Admin.Models;

[assembly: RegisterFormComponent(
    identifier: ElasticSearchIndexConfigurationFormComponent.IDENTIFIER,
    componentType: typeof(ElasticSearchIndexConfigurationFormComponent),
    name: "ElasticSearch Search Index Configuration")]

namespace Kentico.Xperience.ElasticSearch.Admin.Components;

#pragma warning disable S2094 // intentionally empty class
public class ElasticSearchIndexConfigurationComponentProperties : FormComponentProperties
{
}
#pragma warning restore

public class ElasticSearchIndexConfigurationComponentClientProperties : FormComponentClientProperties<IEnumerable<ElasticSearchIndexIncludedPath>>
{
    public IEnumerable<ElasticSearchIndexContentType>? PossibleContentTypeItems { get; set; }
}

public sealed class ElasticSearchIndexConfigurationComponentAttribute : FormComponentAttribute
{
}

[ComponentAttribute(typeof(ElasticSearchIndexConfigurationComponentAttribute))]
public class ElasticSearchIndexConfigurationFormComponent : FormComponent<ElasticSearchIndexConfigurationComponentProperties, ElasticSearchIndexConfigurationComponentClientProperties, IEnumerable<ElasticSearchIndexIncludedPath>>
{
    public const string IDENTIFIER = "kentico.xperience-integrations-elasticsearch.elasticsearch-index-configuration";

    internal List<ElasticSearchIndexIncludedPath>? Value { get; set; }

    public override string ClientComponentName => "@kentico/xperience-integrations-elasticsearch/ElasticSearchIndexConfiguration";

    public override IEnumerable<ElasticSearchIndexIncludedPath> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<ElasticSearchIndexIncludedPath> value) => Value = value.ToList();

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> DeletePath(string path)
    {
        var toRemove = Value?.Find(x => Equals(x.AliasPath == path, StringComparison.OrdinalIgnoreCase));

        if (toRemove != null)
        {
            Value?.Remove(toRemove);
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> SavePath(ElasticSearchIndexIncludedPath path)
    {
        var value = Value?.SingleOrDefault(x => Equals(x.AliasPath == path.AliasPath, StringComparison.OrdinalIgnoreCase));

        if (value is not null)
        {
            Value?.Remove(value);
        }

        Value?.Add(path);

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> AddPath(string path)
    {
        if (Value?.Exists(x => x.AliasPath == path) ?? false)
        {
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }

        Value?.Add(new ElasticSearchIndexIncludedPath(path));

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    protected override async Task ConfigureClientProperties(ElasticSearchIndexConfigurationComponentClientProperties properties)
    {
        var allWebsiteContentTypes = (await DataClassInfoProvider.ProviderObject
              .Get()
              .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), ClassContentTypeType.WEBSITE)
              .GetEnumerableTypedResultAsync())
              .Select(x => new ElasticSearchIndexContentType(x.ClassName, x.ClassDisplayName));

        properties.Value = Value ?? [];
        properties.PossibleContentTypeItems = allWebsiteContentTypes.ToList();

        await base.ConfigureClientProperties(properties);
    }
}
