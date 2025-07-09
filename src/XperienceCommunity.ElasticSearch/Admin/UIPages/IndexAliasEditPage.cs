using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.Forms.Internal;

using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Admin.Services;
using XperienceCommunity.ElasticSearch.Admin.UIPages;
using XperienceCommunity.ElasticSearch.Aliasing;

[assembly: UIPage(
   parentType: typeof(IndexAliasListingPage),
   slug: PageParameterConstants.PARAMETERIZED_SLUG,
   uiPageType: typeof(IndexAliasEditPage),
   name: "Edit index alias",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace XperienceCommunity.ElasticSearch.Admin.UIPages;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class IndexAliasEditPage(IFormItemCollectionProvider formItemCollectionProvider,
             IFormDataBinder formDataBinder,
             IElasticSearchConfigurationStorageService storageService,
             IElasticSearchIndexAliasService elasticSearchIndexAliasService
    ) : BaseIndexAliasEditPage(formItemCollectionProvider, formDataBinder, elasticSearchIndexAliasService, storageService)
{
    private ElasticSearchAliasConfigurationModel? model;

    [PageParameter(typeof(IntPageModelBinder))]
    public int IndexIdentifier { get; set; }

    protected override ElasticSearchAliasConfigurationModel Model
    {
        get
        {
            model ??= StorageService.GetAliasDataOrNull(IndexIdentifier) ?? new();

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ElasticSearchAliasConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = await ValidateAndProcess(model);

        var response = ResponseFrom(new FormSubmissionResult(
            result.ModificationResult == ModificationResult.Success
                ? FormSubmissionStatus.ValidationSuccess
                : FormSubmissionStatus.ValidationFailure));

        if (result.ModificationResult == ModificationResult.Failure)
        {
            if (result.ErrorMessages is not null)
            {
                result.ErrorMessages.ForEach(errorMessage => response.AddErrorMessage(errorMessage));
            }
            else
            {
                response.AddErrorMessage("Could not edit index alias.");
            }
        }
        else
        {
            response.AddSuccessMessage("Index alias edited");
        }

        return response;
    }
}
