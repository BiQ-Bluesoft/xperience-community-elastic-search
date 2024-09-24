using System.ComponentModel.DataAnnotations;
using System.Text;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Aliasing;

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
    private readonly IElasticSearchIndexAliasService elasticSearchIndexAliasService = elasticSearchIndexAliasService;

    protected async Task<ModificationResponse> ValidateAndProcess(ElasticSearchAliasConfigurationModel configuration)
    {
        configuration.AliasName = RemoveWhitespacesUsingStringBuilder(configuration.AliasName ?? "");

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
            var oldAliasName = StorageService.GetAliasDataOrNull(configuration.Id)!.AliasName;

            var edited = StorageService.TryEditAlias(configuration);

            if (edited)
            {
                ElasticSearchIndexAliasStore.SetAliases(StorageService);
                await elasticSearchIndexAliasService.EditAlias(oldAliasName, configuration.AliasName, configuration.IndexNames, default);

                return new ModificationResponse(ModificationResult.Success);
            }

            return new ModificationResponse(ModificationResult.Failure);
        }

        var created = !configuration.IndexNames.IsNullOrEmpty() && StorageService.TryCreateAlias(configuration);

        if (created)
        {
            ElasticSearchIndexAliasStore.Instance.AddAlias(new ElasticSearchIndexAlias(configuration));

            return new ModificationResponse(ModificationResult.Success);
        }

        return new ModificationResponse(ModificationResult.Failure);
    }

    protected static string RemoveWhitespacesUsingStringBuilder(string source)
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
}
