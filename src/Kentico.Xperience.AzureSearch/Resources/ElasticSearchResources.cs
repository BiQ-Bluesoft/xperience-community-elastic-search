using CMS.Base;
using CMS.Localization;

using Kentico.Xperience.AzureSearch.Resources;

[assembly: RegisterLocalizationResource(typeof(ElasticSearchResources), SystemContext.SYSTEM_CULTURE_NAME)]
namespace Kentico.Xperience.AzureSearch.Resources;

internal class ElasticSearchResources
{
    public ElasticSearchResources()
    {
    }
}
