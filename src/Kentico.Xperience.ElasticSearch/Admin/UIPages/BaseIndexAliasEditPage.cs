using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Aliasing;
using Kentico.Xperience.ElasticSearch.Helpers;
using Kentico.Xperience.ElasticSearch.Helpers.Extensions;

using Microsoft.IdentityModel.Tokens;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Kentico.Xperience.ElasticSearch.Admin;

internal abstract class BaseIndexAliasEditPage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService,
    IElasticSearchConfigurationStorageService storageService
    ) : ModelEditPage<ElasticSearchAliasConfigurationModel>(formItemCollectionProvider, formDataBinder)
{
    protected IElasticSearchConfigurationStorageService StorageService = storageService;

    protected async Task<ModificationResponse> ValidateAndProcess(ElasticSearchAliasConfigurationModel configuration)
    {
        configuration.AliasName = configuration.AliasName.RemoveWhitespacesUsingStringBuilder();

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

        if (StorageService.GetAliasIds().Exists(x => x == configuration.Id))
        {
            return await ProcessExistingAlias(configuration);
        }

        return await ProcessNewAlias(configuration);
    }

    private async Task<ModificationResponse> ProcessNewAlias(ElasticSearchAliasConfigurationModel configuration)
    {
        if (!configuration.IndexNames.IsNullOrEmpty() && StorageService.TryCreateAlias(configuration))
        {
            var elasticResponse = await elasticSearchIndexAliasService.CreateAliasAsync(configuration.AliasName, configuration.IndexNames, default);
            if (!elasticResponse.IsSuccess)
            {
                StorageService.TryDeleteAlias(configuration.Id);
                return new ModificationResponse(ModificationResult.Failure, [elasticResponse.ErrorMessage]);
            }

            ElasticSearchIndexAliasStore.Instance.AddAlias(new ElasticSearchIndexAlias(configuration));
            return new ModificationResponse(ModificationResult.Success);
        }

        return new ModificationResponse(ModificationResult.Failure);
    }

    private async Task<ModificationResponse> ProcessExistingAlias(ElasticSearchAliasConfigurationModel configuration)
    {
        var oldAliasName = StorageService.GetAliasDataOrNull(configuration.Id)!.AliasName;

        if (StorageService.TryEditAlias(configuration))
        {
            var elasticResponse = await elasticSearchIndexAliasService.EditAliasAsync(oldAliasName, configuration.AliasName, configuration.IndexNames, default);
            if (!elasticResponse.IsSuccess)
            {
                return new ModificationResponse(ModificationResult.Failure, [elasticResponse.ErrorMessage]);
            }

            ElasticSearchIndexAliasStore.SetAliases(StorageService);
            return new ModificationResponse(ModificationResult.Success);
        }

        return new ModificationResponse(ModificationResult.Failure);
    }
}
