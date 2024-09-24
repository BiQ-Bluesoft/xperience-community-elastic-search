using System.ComponentModel.DataAnnotations;
using System.Text;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Indexing;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Kentico.Xperience.ElasticSearch.Admin;

internal abstract class BaseIndexEditPage : ModelEditPage<ElasticSearchConfigurationModel>
{
    protected readonly IElasticSearchConfigurationStorageService StorageService;
    //private readonly IElasticSearchIndexClientService indexClientService;

    protected BaseIndexEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IElasticSearchConfigurationStorageService storageService
        //IElasticSearchIndexClientService indexClientService
        )
        : base(formItemCollectionProvider, formDataBinder) =>
        //this.indexClientService = indexClientService;
        StorageService = storageService;

    protected async Task<ModificationResponse> ValidateAndProcess(ElasticSearchConfigurationModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

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
                ElasticSearchIndexStore.SetIndicies(StorageService);
                //await indexClientService.EditIndex(oldIndex!.IndexName, configuration, default);

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
