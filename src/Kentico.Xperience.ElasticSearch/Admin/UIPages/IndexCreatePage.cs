using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Indexing;
using Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: "create",
   uiPageType: typeof(IndexCreatePage),
   name: "Create index",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.ElasticSearch.Admin;

[UIEvaluatePermission(SystemPermissions.CREATE)]
internal class IndexCreatePage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IElasticSearchConfigurationStorageService storageService,
    IPageLinkGenerator pageLinkGenerator,
    IElasticSearchClient defaultElasticSearchClient) : BaseIndexEditPage(formItemCollectionProvider, formDataBinder, storageService, defaultElasticSearchClient)
{
    private ElasticSearchConfigurationModel? model;

    protected override ElasticSearchConfigurationModel Model
    {
        get
        {
            model ??= new();

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ElasticSearchConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = await ValidateAndProcess(model);

        if (result.ModificationResult == ModificationResult.Success)
        {
            var index = ElasticSearchIndexStore.Instance.GetRequiredIndex(model.IndexName);

            var pageParameters = new PageParameterValues
            {
                { typeof(IndexEditPage), index.Identifier}
            };
            var successResponse = NavigateTo(pageLinkGenerator.GetPath<IndexEditPage>(pageParameters))
                .AddSuccessMessage("Index created.");

            return successResponse;
        }

        var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));

        if (result.ErrorMessages is not null)
        {
            result.ErrorMessages.ForEach(errorMessage => errorResponse.AddErrorMessage(errorMessage));
        }
        else
        {
            errorResponse.AddErrorMessage("Could not create index.");
        }

        return errorResponse;
    }
}
