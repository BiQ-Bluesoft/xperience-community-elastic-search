using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Indexing;

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
internal class IndexCreatePage : BaseIndexEditPage
{
    private readonly IPageUrlGenerator pageUrlGenerator;
    private ElasticSearchConfigurationModel? model = null;

    public IndexCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IElasticSearchConfigurationStorageService storageService,
        IPageUrlGenerator pageUrlGenerator
        /*IElasticSearchIndexClientService searchClientService*/)
        : base(formItemCollectionProvider, formDataBinder, storageService/*, searchClientService*/) => this.pageUrlGenerator = pageUrlGenerator;

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

            var successResponse = NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(index.Identifier.ToString()))
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
