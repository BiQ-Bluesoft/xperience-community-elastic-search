using System.Text;

using CMS.DataEngine;

using Kentico.Xperience.ElasticSearch.Admin.Models;

namespace Kentico.Xperience.ElasticSearch.Admin.Services;

internal class DefaultElasticSearchConfigurationStorageService(
    IElasticSearchIndexItemInfoProvider indexProvider,
    IElasticSearchIndexAliasItemInfoProvider indexAliasProvider,
    IElasticSearchIndexAliasIndexItemInfoProvider indexAliasIndexProvider,
    IElasticSearchIncludedPathItemInfoProvider pathProvider,
    IElasticSearchContentTypeItemInfoProvider contentTypeProvider,
    IElasticSearchIndexLanguageItemInfoProvider languageProvider
    ) : IElasticSearchConfigurationStorageService
{
    private static string RemoveWhitespacesUsingStringBuilder(string source)
    {
        var builder = new StringBuilder(source.Length);

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return source.Length == builder.Length ? source : builder.ToString();
    }

    public bool TryCreateIndex(ElasticSearchConfigurationModel configuration)
    {
        var existingIndex = indexProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemIndexName), configuration.IndexName)
            .TopN(1)
            .FirstOrDefault();

        if (existingIndex is not null)
        {
            return false;
        }

        var newInfo = new ElasticSearchIndexItemInfo()
        {
            ElasticSearchIndexItemIndexName = configuration.IndexName ?? "",
            ElasticSearchIndexItemChannelName = configuration.ChannelName ?? "",
            ElasticSearchIndexItemStrategyName = configuration.StrategyName ?? "",
            ElasticSearchIndexItemRebuildHook = configuration.RebuildHook ?? ""
        };

        indexProvider.Set(newInfo);

        configuration.Id = newInfo.ElasticSearchIndexItemId;

        foreach (var language in configuration.LanguageNames)
        {
            var languageInfo = new ElasticSearchIndexLanguageItemInfo()
            {
                ElasticSearchIndexLanguageItemName = language,
                ElasticSearchIndexLanguageItemIndexItemId = newInfo.ElasticSearchIndexItemId
            };

            languageInfo.Insert();
        }

        foreach (var path in configuration.Paths)
        {
            var pathInfo = new ElasticSearchIncludedPathItemInfo()
            {
                ElasticSearchIncludedPathItemAliasPath = path.AliasPath,
                ElasticSearchIncludedPathItemIndexItemId = newInfo.ElasticSearchIndexItemId
            };
            pathProvider.Set(pathInfo);

            foreach (var contentType in path.ContentTypes)
            {
                var contentInfo = new ElasticSearchContentTypeItemInfo()
                {
                    ElasticSearchContentTypeItemContentTypeName = contentType.ContentTypeName,
                    ElasticSearchContentTypeItemIncludedPathItemId = pathInfo.ElasticSearchIncludedPathItemId,
                    ElasticSearchContentTypeItemIndexItemId = newInfo.ElasticSearchIndexItemId
                };
                contentInfo.Insert();
            }
        }

        return true;
    }

    public bool TryCreateAlias(ElasticSearchAliasConfigurationModel configuration)
    {
        var existingAliases = indexAliasProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemIndexAliasName), configuration.AliasName)
            .TopN(1)
            .FirstOrDefault();

        if (existingAliases is not null)
        {
            return false;
        }

        var aliasInfo = new ElasticSearchIndexAliasItemInfo()
        {
            ElasticSearchIndexAliasItemIndexAliasName = configuration.AliasName ?? "",
        };

        var indexIds = indexProvider
            .Get()
            .Where(index => configuration.IndexNames.Any(name => index.ElasticSearchIndexItemIndexName == name))
            .Select(index => index.ElasticSearchIndexItemId)
            .ToList();

        indexAliasProvider.Set(aliasInfo);

        foreach (var indexId in indexIds)
        {
            var indexAliasIndexInfo = new ElasticSearchIndexAliasIndexItemInfo()
            {
                ElasticSearchIndexAliasIndexItemIndexAliasId = aliasInfo.ElasticSearchIndexAliasItemId,
                ElasticSearchIndexAliasIndexItemIndexItemId = indexId
            };

            indexAliasIndexProvider.Set(indexAliasIndexInfo);
        }

        configuration.Id = aliasInfo.ElasticSearchIndexAliasItemId;

        return true;
    }

    public ElasticSearchConfigurationModel? GetIndexDataOrNull(int indexId)
    {
        var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemIndexItemId), indexInfo.ElasticSearchIndexItemId).GetEnumerableTypedResult();

        var contentTypesInfoItems = contentTypeProvider
        .Get()
        .WhereEquals(nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIndexItemId), indexInfo.ElasticSearchIndexItemId)
        .GetEnumerableTypedResult();

        var contentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(
                nameof(DataClassInfo.ClassName),
                contentTypesInfoItems
                    .Select(x => x.ElasticSearchContentTypeItemContentTypeName)
                    .ToArray()
            ).GetEnumerableTypedResult()
            .Select(x => new ElasticSearchIndexContentType(x.ClassName, x.ClassDisplayName));

        var languages = languageProvider.Get().WhereEquals(nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemIndexItemId), indexInfo.ElasticSearchIndexItemId).GetEnumerableTypedResult();

        return new ElasticSearchConfigurationModel(indexInfo, languages, paths, contentTypes);
    }

    public ElasticSearchAliasConfigurationModel? GetAliasDataOrNull(int aliasId)
    {
        var aliasInfo = indexAliasProvider.Get().WithID(aliasId).FirstOrDefault();
        if (aliasInfo == default)
        {
            return default;
        }

        var indexAliasIndexIndexInfoIds = indexAliasIndexProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexAliasId), aliasId)
            .GetEnumerableTypedResult()
            .Select(indexAliasIndex => indexAliasIndex.ElasticSearchIndexAliasIndexItemIndexItemId);

        var indexNames = indexProvider.Get()
            .WhereIn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId), indexAliasIndexIndexInfoIds.ToList())
            .GetEnumerableTypedResult()
            .Select(index => index.ElasticSearchIndexItemIndexName);

        return new ElasticSearchAliasConfigurationModel(aliasInfo, indexNames);
    }

    public List<int> GetIndexIds() => indexProvider.Get().Select(x => x.ElasticSearchIndexItemId).ToList();

    public List<int> GetAliasIds() => indexAliasProvider.Get().Select(x => x.ElasticSearchIndexAliasItemId).ToList();

    public IEnumerable<ElasticSearchConfigurationModel> GetAllIndexData()
    {
        var indexInfos = indexProvider.Get().GetEnumerableTypedResult().ToList();
        if (indexInfos.Count == 0)
        {
            return new List<ElasticSearchConfigurationModel>();
        }

        var paths = pathProvider.Get().ToList();

        var contentTypesInfoItems = contentTypeProvider
           .Get()
           .GetEnumerableTypedResult();

        var contentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(
                nameof(DataClassInfo.ClassName),
                contentTypesInfoItems
                    .Select(x => x.ElasticSearchContentTypeItemContentTypeName)
                    .ToArray()
            ).GetEnumerableTypedResult()
            .Select(x => new ElasticSearchIndexContentType(x.ClassName, x.ClassDisplayName));

        var languages = languageProvider.Get().ToList();

        return indexInfos.Select(index => new ElasticSearchConfigurationModel(index, languages, paths, contentTypes));
    }

    public IEnumerable<ElasticSearchAliasConfigurationModel> GetAllAliasData()
    {
        var aliasInfoIds = indexAliasProvider.Get().GetEnumerableTypedResult().Select(x => x.ElasticSearchIndexAliasItemId).ToList();
        if (aliasInfoIds.Count == 0)
        {
            return new List<ElasticSearchAliasConfigurationModel>();
        }

        var result = new List<ElasticSearchAliasConfigurationModel>();

        foreach (var aliasInfoId in aliasInfoIds)
        {
            var aliasData = GetAliasDataOrNull(aliasInfoId);

            if (aliasData is not null)
            {
                result.Add(aliasData);
            }
        }

        return result;
    }

    public bool TryEditIndex(ElasticSearchConfigurationModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

        var indexInfo = indexProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId), configuration.Id)
            .TopN(1)
            .FirstOrDefault();

        if (indexInfo is null)
        {
            return false;
        }

        pathProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemIndexItemId)} = {configuration.Id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIndexItemId)} = {configuration.Id}"));
        indexAliasIndexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexItemId)} = {configuration.Id}"));

        indexInfo.ElasticSearchIndexItemRebuildHook = configuration.RebuildHook ?? "";
        indexInfo.ElasticSearchIndexItemStrategyName = configuration.StrategyName ?? "";
        indexInfo.ElasticSearchIndexItemChannelName = configuration.ChannelName ?? "";
        indexInfo.ElasticSearchIndexItemIndexName = configuration.IndexName ?? "";

        indexProvider.Set(indexInfo);

        foreach (var language in configuration.LanguageNames)
        {
            var languageInfo = new ElasticSearchIndexLanguageItemInfo()
            {
                ElasticSearchIndexLanguageItemName = language,
                ElasticSearchIndexLanguageItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
            };

            languageProvider.Set(languageInfo);
        }

        foreach (var path in configuration.Paths)
        {
            var pathInfo = new ElasticSearchIncludedPathItemInfo()
            {
                ElasticSearchIncludedPathItemAliasPath = path.AliasPath,
                ElasticSearchIncludedPathItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
            };
            pathProvider.Set(pathInfo);

            foreach (var contentType in path.ContentTypes)
            {
                var contentInfo = new ElasticSearchContentTypeItemInfo()
                {
                    ElasticSearchContentTypeItemContentTypeName = contentType.ContentTypeName ?? "",
                    ElasticSearchContentTypeItemIncludedPathItemId = pathInfo.ElasticSearchIncludedPathItemId,
                    ElasticSearchContentTypeItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
                };
                contentInfo.Insert();
            }
        }

        return true;
    }

    public bool TryEditAlias(ElasticSearchAliasConfigurationModel configuration)
    {
        configuration.AliasName = RemoveWhitespacesUsingStringBuilder(configuration.AliasName ?? "");

        var aliasInfo = indexAliasProvider.Get()
            .WhereEquals(nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemId), configuration.Id)
            .TopN(1)
            .FirstOrDefault();

        indexAliasIndexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexItemId)} = {configuration.Id}"));

        if (aliasInfo is null)
        {
            return false;
        }

        aliasInfo.ElasticSearchIndexAliasItemIndexAliasName = configuration.AliasName ?? "";

        var indexIds = indexProvider
            .Get()
            .Where(index => configuration.IndexNames.Any(name => index.ElasticSearchIndexItemIndexName == name))
            .Select(index => index.ElasticSearchIndexItemId)
            .ToList();

        foreach (var indexId in indexIds)
        {
            var indexAliasIndexInfo = new ElasticSearchIndexAliasIndexItemInfo()
            {
                ElasticSearchIndexAliasIndexItemIndexAliasId = aliasInfo.ElasticSearchIndexAliasItemId,
                ElasticSearchIndexAliasIndexItemIndexItemId = indexId
            };

            indexAliasIndexProvider.Set(indexAliasIndexInfo);
        }

        indexAliasProvider.Set(aliasInfo);

        return true;
    }

    public bool TryDeleteIndex(int id)
    {
        indexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId)} = {id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemIndexItemId)} = {id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemIndexItemId)} = {id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIndexItemId)} = {id}"));
        indexAliasIndexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexItemId)} = {id}"));

        return true;
    }

    public bool TryDeleteAlias(int id)
    {
        indexAliasProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemId)} = {id}"));
        indexAliasIndexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexAliasId)} = {id}"));

        return true;
    }
}
