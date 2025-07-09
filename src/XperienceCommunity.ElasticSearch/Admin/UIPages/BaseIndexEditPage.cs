using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Admin.Services;
using XperienceCommunity.ElasticSearch.Helpers.Extensions;
using XperienceCommunity.ElasticSearch.Indexing;
using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.SearchClients;
using XperienceCommunity.ElasticSearch.Indexing.Strategies;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace XperienceCommunity.ElasticSearch.Admin.UIPages;

internal abstract class BaseIndexEditPage : ModelEditPage<ElasticSearchConfigurationModel>
{
    protected readonly IElasticSearchConfigurationStorageService StorageService;
    private readonly IElasticSearchClient defaultElasticSearchClient;

    protected BaseIndexEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IElasticSearchConfigurationStorageService storageService,
        IElasticSearchClient defaultElasticSearchClient)
        : base(formItemCollectionProvider, formDataBinder)
    {
        StorageService = storageService;
        this.defaultElasticSearchClient = defaultElasticSearchClient;
    }

    protected async Task<ModificationResponse> ValidateAndProcess(ElasticSearchConfigurationModel configuration)
    {
        configuration.IndexName = configuration.IndexName.RemoveWhitespacesUsingStringBuilder();

        var context = new ValidationContext(configuration, null, null);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var valid = Validator.TryValidateObject(configuration, context, validationResults, true);

        if (!valid)
        {
            return new ModificationResponse(ModificationResult.Failure,
                validationResults
                    .Where(result => result.ErrorMessage is not null)
                    .Select(result => result.ErrorMessage!)
                    .ToList()
            );
        }

        if (StorageService.GetIndexIds().Exists(x => x == configuration.Id))
        {
            var oldIndex = StorageService.GetIndexDataOrNull(configuration.Id);

            if (StorageService.TryEditIndex(configuration))
            {
                ElasticSearchIndexStore.SetIndices(StorageService);

                var elasticRespone = await defaultElasticSearchClient.EditIndexAsync(oldIndex!.IndexName, configuration, default);
                if (!elasticRespone.IsSuccess)
                {
                    return new ModificationResponse(ModificationResult.Failure, [elasticRespone.ErrorMessage]);
                }

                return new ModificationResponse(ModificationResult.Success);
            }

            return new ModificationResponse(ModificationResult.Failure);
        }

        if (!string.IsNullOrWhiteSpace(configuration.IndexName) && StorageService.TryCreateIndex(configuration))
        {
            ElasticSearchIndexStore.Instance.AddIndex(new ElasticSearchIndex(configuration, StrategyStorage.Strategies));

            var elasticResponse = await defaultElasticSearchClient.CreateIndexAsync(configuration.IndexName);
            if (!elasticResponse.IsSuccess)
            {
                return new ModificationResponse(ModificationResult.Failure, [elasticResponse.ErrorMessage]);
            }
            return new ModificationResponse(ModificationResult.Success);
        }
        return new ModificationResponse(ModificationResult.Failure);
    }
}
