//using System.ComponentModel.DataAnnotations;
//using System.Text;

//using Kentico.Xperience.Admin.Base;
//using Kentico.Xperience.Admin.Base.Forms;
//using Kentico.Xperience.AzureSearch.Aliasing;

//using Microsoft.IdentityModel.Tokens;

//using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

//namespace Kentico.Xperience.AzureSearch.Admin;

//internal abstract class BaseIndexAliasEditPage(
//    IFormItemCollectionProvider formItemCollectionProvider,
//    IFormDataBinder formDataBinder,
//    IElasticSearchIndexAliasService elasticSearchIndexAliasService,
//    IElasticSearchConfigurationStorageService storageService) : ModelEditPage<ElasticSearchAliasConfigurationModel>(formItemCollectionProvider, formDataBinder)
//{
//    protected IElasticSearchConfigurationStorageService StorageService = storageService;
//    protected async Task<ModificationResponse> ValidateAndProcess(ElasticSearchAliasConfigurationModel configuration)
//    {
//        configuration.AliasName = RemoveWhitespacesUsingStringBuilder(configuration.AliasName ?? "");

//        var context = new ValidationContext(configuration, null, null);
//        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
//        var valid = Validator.TryValidateObject(configuration, context, validationResults, true);

//        if (!valid)
//        {
//            return new ModificationResponse(ModificationResult.Failure,
//                validationResults
//                    .Where(result => result.ErrorMessage is not null)
//                    .Select(result => result.ErrorMessage!)
//                    .ToList()
//            );
//        }

//        if (storageService.GetAliasIds().Exists(x => x == configuration.Id))
//        {
//            var oldAliasName = storageService.GetAliasDataOrNull(configuration.Id)!.AliasName;

//            var edited = storageService.TryEditAlias(configuration);

//            if (edited)
//            {
//                AzureSearchIndexAliasStore.SetAliases(storageService);
//                var indexName = configuration.IndexNames.First();
//                await elasticSearchIndexAliasService.EditAlias(indexName, oldAliasName, configuration.AliasName, default);

//                return new ModificationResponse(ModificationResult.Success);
//            }

//            return new ModificationResponse(ModificationResult.Failure);
//        }

//        var created = !configuration.IndexNames.IsNullOrEmpty() && storageService.TryCreateAlias(configuration);

//        if (created)
//        {
//            AzureSearchIndexAliasStore.Instance.AddAlias(new AzureSearchIndexAlias(configuration));

//            return new ModificationResponse(ModificationResult.Success);
//        }

//        return new ModificationResponse(ModificationResult.Failure);
//    }

//    protected static string RemoveWhitespacesUsingStringBuilder(string source)
//    {
//        var builder = new StringBuilder(source.Length);

//        for (var i = 0; i < source.Length; i++)
//        {
//            var c = source[i];

//            if (!char.IsWhiteSpace(c))
//            {
//                builder.Append(c);
//            }
//        }

//        return source.Length == builder.Length ? source : builder.ToString();
//    }
//}
