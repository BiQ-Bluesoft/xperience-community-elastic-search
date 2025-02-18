using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Helpers.Extensions;
using Kentico.Xperience.ElasticSearch.Indexing;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchClients;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Kentico.Xperience.ElasticSearch.Admin;

internal abstract class BaseIndexEditPage : ModelEditPage<ElasticSearchConfigurationModel>
{
    protected readonly IElasticSearchConfigurationStorageService StorageService;
    private readonly IElasticSearchIndexClientService indexClientService;

    protected BaseIndexEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IElasticSearchConfigurationStorageService storageService,
        IElasticSearchIndexClientService indexClientService
        )
        : base(formItemCollectionProvider, formDataBinder)
    {
        this.indexClientService = indexClientService;
        StorageService = storageService;
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
            var edited = StorageService.TryEditIndex(configuration);

            if (edited)
            {
                ElasticSearchIndexStore.SetIndices(StorageService);
                await indexClientService.EditIndexAsync(oldIndex!.IndexName, configuration, default);

                return new ModificationResponse(ModificationResult.Success);
            }

            return new ModificationResponse(ModificationResult.Failure);
        }

        var created = !string.IsNullOrWhiteSpace(configuration.IndexName) && StorageService.TryCreateIndex(configuration);

        if (created)
        {
            ElasticSearchIndexStore.Instance.AddIndex(new ElasticSearchIndex(configuration, StrategyStorage.Strategies));

            return new ModificationResponse(ModificationResult.Success);
        }

        return new ModificationResponse(ModificationResult.Failure);
    }
}
