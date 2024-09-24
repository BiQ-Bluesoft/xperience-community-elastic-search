using CMS.Base;
using CMS.Localization;

using Kentico.Xperience.ElasticSearch.Resources;

[assembly: RegisterLocalizationResource(typeof(ElasticSearchResources), SystemContext.SYSTEM_CULTURE_NAME)]
namespace Kentico.Xperience.ElasticSearch.Resources;

internal class ElasticSearchResources
{
    public ElasticSearchResources()
    {
    }
}
