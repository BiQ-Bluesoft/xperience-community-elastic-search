using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Aliasing;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
   parentType: typeof(IndexAliasListingPage),
   slug: "create",
   uiPageType: typeof(IndexAliasCreatePage),
   name: "Create alias",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.ElasticSearch.Admin;

[UIEvaluatePermission(SystemPermissions.CREATE)]
internal class IndexAliasCreatePage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService,
    IElasticSearchConfigurationStorageService storageService,
    IPageLinkGenerator pageLinkGenerator) : BaseIndexAliasEditPage(formItemCollectionProvider, formDataBinder, elasticSearchIndexAliasService, storageService)
{

    private ElasticSearchAliasConfigurationModel? model;

    protected override ElasticSearchAliasConfigurationModel Model
    {
        get
        {
            model ??= new();

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ElasticSearchAliasConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = await ValidateAndProcess(model);

        if (result.ModificationResult == ModificationResult.Success)
        {
            var alias = ElasticSearchIndexAliasStore.Instance.GetRequiredAlias(model.AliasName);
            var pageParameters = new PageParameterValues
            {
                { typeof(IndexAliasEditPage), alias.Identifier }
            };

            var successResponse = NavigateTo(pageLinkGenerator.GetPath<IndexAliasEditPage>(pageParameters))
                .AddSuccessMessage("Index alias created.");
            return successResponse;
        }

        return CreateErrorResponse(result);
    }

    private ICommandResponse CreateErrorResponse(ModificationResponse result)
    {
        var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));

        if (result.ErrorMessages is not null)
        {
            result.ErrorMessages.ForEach(errorMessage => errorResponse.AddErrorMessage(errorMessage));
        }
        else
        {
            errorResponse.AddErrorMessage("Could not create index alias.");
        }

        return errorResponse;
    }
}
