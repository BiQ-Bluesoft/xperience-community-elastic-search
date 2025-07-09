using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Admin.Services;
using XperienceCommunity.ElasticSearch.Admin.UIPages;
using XperienceCommunity.ElasticSearch.Indexing.SearchClients;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: PageParameterConstants.PARAMETERIZED_SLUG,
   uiPageType: typeof(IndexEditPage),
   name: "Edit index",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace XperienceCommunity.ElasticSearch.Admin.UIPages;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class IndexEditPage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IElasticSearchConfigurationStorageService storageService,
    IElasticSearchClient defaultElasticSearchClient) : BaseIndexEditPage(formItemCollectionProvider, formDataBinder, storageService, defaultElasticSearchClient)
{
    private ElasticSearchConfigurationModel? model;

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
                response.AddErrorMessage("Could not edit index.");
            }
        }
        else
        {
            response.AddSuccessMessage("Index edited. Rebuild of index started.");
        }

        return response;
    }
}
