using XperienceCommunity.ElasticSearch.Admin.Models;

namespace XperienceCommunity.ElasticSearch.Admin.Services;

public interface IElasticSearchConfigurationStorageService
{
    bool TryCreateIndex(ElasticSearchConfigurationModel configuration);
    bool TryCreateAlias(ElasticSearchAliasConfigurationModel configuration);
    bool TryEditIndex(ElasticSearchConfigurationModel configuration);
    bool TryEditAlias(ElasticSearchAliasConfigurationModel configuration);
    bool TryDeleteIndex(int id);
    bool TryDeleteAlias(int id);
    ElasticSearchConfigurationModel? GetIndexDataOrNull(int indexId);
    ElasticSearchAliasConfigurationModel? GetAliasDataOrNull(int aliasId);
    List<int> GetIndexIds();
    List<int> GetAliasIds();
    IEnumerable<ElasticSearchConfigurationModel> GetAllIndexData();
    IEnumerable<ElasticSearchAliasConfigurationModel> GetAllAliasData();
}
