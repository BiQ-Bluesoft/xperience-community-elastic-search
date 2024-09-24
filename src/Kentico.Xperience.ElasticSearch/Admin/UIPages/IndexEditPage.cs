using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Indexing;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: PageParameterConstants.PARAMETERIZED_SLUG,
   uiPageType: typeof(IndexEditPage),
   name: "Edit index",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.ElasticSearch.Admin;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class IndexEditPage(
    Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IElasticSearchConfigurationStorageService storageService,
    IElasticSearchIndexClientService indexClientService) : BaseIndexEditPage(formItemCollectionProvider, formDataBinder, storageService, indexClientService)
{
    private ElasticSearchConfigurationModel? model = null;

    [PageParameter(typeof(IntPageModelBinder))]
    public int IndexIdentifier { get; set; }

    protected override ElasticSearchConfigurationModel Model
    {
        get
        {
            model ??= StorageService.GetIndexDataOrNull(IndexIdentifier) ?? new();

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ElasticSearchConfigurationModel model, ICollection<IFormItem> formItems)
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
                response.AddErrorMessage("Could not create index.");
            }
        }
        else
        {
            response.AddSuccessMessage("Index edited");
        }

        return response;
    }
}
