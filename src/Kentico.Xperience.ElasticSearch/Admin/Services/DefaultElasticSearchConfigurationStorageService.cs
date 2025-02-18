using System.Text;

using CMS.DataEngine;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Helpers.Extensions;

namespace Kentico.Xperience.ElasticSearch.Admin.Services;

internal class DefaultElasticSearchConfigurationStorageService(
    IInfoProvider<ElasticSearchIndexItemInfo> indexProvider,
    IInfoProvider<ElasticSearchIndexAliasItemInfo> indexAliasProvider,
    IInfoProvider<ElasticSearchIndexAliasIndexItemInfo> indexAliasIndexProvider,
    IInfoProvider<ElasticSearchIncludedPathItemInfo> pathProvider,
    IInfoProvider<ElasticSearchContentTypeItemInfo> contentTypeProvider,
    IInfoProvider<ElasticSearchIndexLanguageItemInfo> languageProvider,
    IInfoProvider<ElasticSearchReusableContentTypeItemInfo> reusableContentTypeProvider
    ) : IElasticSearchConfigurationStorageService
{
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

        var newInfo = new ElasticSearchIndexItemInfo
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
            var languageInfo = new ElasticSearchIndexLanguageItemInfo
            {
                ElasticSearchIndexLanguageItemName = language,
                ElasticSearchIndexLanguageItemIndexItemId = newInfo.ElasticSearchIndexItemId
            };

            languageInfo.Insert();
        }

        foreach (var path in configuration.Paths)
        {
            var pathInfo = new ElasticSearchIncludedPathItemInfo
            {
                ElasticSearchIncludedPathItemAliasPath = path.AliasPath,
                ElasticSearchIncludedPathItemIndexItemId = newInfo.ElasticSearchIndexItemId
            };
            pathProvider.Set(pathInfo);

            foreach (var contentType in path.ContentTypes)
            {
                var contentInfo = new ElasticSearchContentTypeItemInfo
                {
                    ElasticSearchContentTypeItemContentTypeName = contentType.ContentTypeName,
                    ElasticSearchContentTypeItemIncludedPathItemId = pathInfo.ElasticSearchIncludedPathItemId,
                    ElasticSearchContentTypeItemIndexItemId = newInfo.ElasticSearchIndexItemId
                };
                contentInfo.Insert();
            }
        }

        if (configuration.ReusableContentTypeNames is not null)
        {
            foreach (var reusableContentTypeName in configuration.ReusableContentTypeNames)
            {
                var reusableContentTypeItemInfo = new ElasticSearchReusableContentTypeItemInfo
                {
                    ElasticSearchReusableContentTypeItemContentTypeName = reusableContentTypeName,
                    ElasticSearchReusableContentTypeItemIndexItemId = newInfo.ElasticSearchIndexItemId
                };

                reusableContentTypeItemInfo.Insert();
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
            ElasticSearchIndexAliasItemIndexAliasName = configuration.AliasName ?? string.Empty,
        };

        var indexIds = indexProvider
            .Get()
            .Where(index => configuration.IndexNames.Any(name => index.ElasticSearchIndexItemIndexName == name))
            .Select(index => index.ElasticSearchIndexItemId)
            .ToList();

        indexAliasProvider.Set(aliasInfo);

        foreach (var indexId in indexIds)
        {
            var indexAliasIndexInfo = new ElasticSearchIndexAliasIndexItemInfo
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

        var reusableContentTypes = reusableContentTypeProvider
            .Get()
            .WhereEquals(nameof(ElasticSearchReusableContentTypeItemInfo.ElasticSearchReusableContentTypeItemIndexItemId), indexInfo.ElasticSearchIndexItemId)
            .GetEnumerableTypedResult();

        var languages = languageProvider.Get().WhereEquals(nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemIndexItemId), indexInfo.ElasticSearchIndexItemId).GetEnumerableTypedResult();

        return new ElasticSearchConfigurationModel(indexInfo, languages, paths, contentTypes, reusableContentTypes);
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
        var reusableContentTypes = reusableContentTypeProvider.Get().ToList();
        var languages = languageProvider.Get().ToList();

        return indexInfos.Select(index =>
        {
            // Additional work -  Report as bug in Azure search as well
            var contentTypesInfoItems = contentTypeProvider
                .Get()
                .WhereEquals(nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIndexItemId), index.ElasticSearchIndexItemId)
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

            return new ElasticSearchConfigurationModel(index, languages, paths, contentTypes, reusableContentTypes);
        });
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
        configuration.IndexName = configuration.IndexName.RemoveWhitespacesUsingStringBuilder();

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

        indexInfo.ElasticSearchIndexItemRebuildHook = configuration.RebuildHook ?? string.Empty;
        indexInfo.ElasticSearchIndexItemStrategyName = configuration.StrategyName ?? string.Empty;
        indexInfo.ElasticSearchIndexItemChannelName = configuration.ChannelName ?? string.Empty;
        indexInfo.ElasticSearchIndexItemIndexName = configuration.IndexName ?? string.Empty;

        indexProvider.Set(indexInfo);

        foreach (var language in configuration.LanguageNames)
        {
            var languageInfo = new ElasticSearchIndexLanguageItemInfo
            {
                ElasticSearchIndexLanguageItemName = language,
                ElasticSearchIndexLanguageItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
            };

            languageProvider.Set(languageInfo);
        }

        foreach (var path in configuration.Paths)
        {
            var pathInfo = new ElasticSearchIncludedPathItemInfo
            {
                ElasticSearchIncludedPathItemAliasPath = path.AliasPath,
                ElasticSearchIncludedPathItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
            };
            pathProvider.Set(pathInfo);

            foreach (var contentType in path.ContentTypes)
            {
                var contentInfo = new ElasticSearchContentTypeItemInfo
                {
                    ElasticSearchContentTypeItemContentTypeName = contentType.ContentTypeName ?? "",
                    ElasticSearchContentTypeItemIncludedPathItemId = pathInfo.ElasticSearchIncludedPathItemId,
                    ElasticSearchContentTypeItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
                };
                contentInfo.Insert();
            }
        }

        RemoveUnusedReusableContentTypes(configuration);
        SetNewIndexReusableContentTypeItems(configuration, indexInfo);

        return true;
    }

    public bool TryEditAlias(ElasticSearchAliasConfigurationModel configuration)
    {
        configuration.AliasName = configuration.AliasName.RemoveWhitespacesUsingStringBuilder();

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
            var indexAliasIndexInfo = new ElasticSearchIndexAliasIndexItemInfo
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
        reusableContentTypeProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchReusableContentTypeItemInfo.ElasticSearchReusableContentTypeItemIndexItemId)} = {id}"));
        return true;
    }

    public bool TryDeleteAlias(int id)
    {
        indexAliasProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemId)} = {id}"));
        indexAliasIndexProvider.BulkDelete(new WhereCondition($"{nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexAliasId)} = {id}"));

        return true;
    }

    private void RemoveUnusedReusableContentTypes(ElasticSearchConfigurationModel configuration)
    {
        var removeReusableContentTypesQuery = reusableContentTypeProvider
            .Get()
            .WhereEquals(nameof(ElasticSearchReusableContentTypeItemInfo.ElasticSearchReusableContentTypeItemIndexItemId), configuration.Id)
            .WhereNotIn(nameof(ElasticSearchReusableContentTypeItemInfo.ElasticSearchReusableContentTypeItemContentTypeName), configuration.ReusableContentTypeNames.ToArray());

        reusableContentTypeProvider.BulkDelete(new WhereCondition(removeReusableContentTypesQuery));
    }


    private void SetNewIndexReusableContentTypeItems(ElasticSearchConfigurationModel configuration, ElasticSearchIndexItemInfo indexInfo)
    {
        var newReusableContentTypes = GetNewReusableContentTypesOnIndex(configuration);

        foreach (var reusableContentType in newReusableContentTypes)
        {
            var reusableContentTypeInfo = new ElasticSearchReusableContentTypeItemInfo
            {
                ElasticSearchReusableContentTypeItemContentTypeName = reusableContentType,
                ElasticSearchReusableContentTypeItemIndexItemId = indexInfo.ElasticSearchIndexItemId,
            };

            reusableContentTypeProvider.Set(reusableContentTypeInfo);
        }
    }

    private IEnumerable<string> GetNewReusableContentTypesOnIndex(ElasticSearchConfigurationModel configuration)
    {
        var existingReusableContentTypes = reusableContentTypeProvider
            .Get()
            .WhereEquals(nameof(ElasticSearchReusableContentTypeItemInfo.ElasticSearchReusableContentTypeItemIndexItemId), configuration.Id)
            .GetEnumerableTypedResult();

        return configuration.ReusableContentTypeNames.Where(x => !existingReusableContentTypes.Any(y => y.ElasticSearchReusableContentTypeItemContentTypeName == x));
    }
}
