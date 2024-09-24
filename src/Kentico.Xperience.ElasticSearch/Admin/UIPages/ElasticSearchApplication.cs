using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.ElasticSearch.Admin;

[assembly: UIApplication(
    identifier: ElasticSearchApplicationPage.IDENTIFIER,
    type: typeof(ElasticSearchApplicationPage),
    slug: "elasticsearch",
    name: "Elastic AI Search",
    category: BaseApplicationCategories.DEVELOPMENT,
    icon: Icons.Magnifier,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// The root application page for the ElasticSearch integration.
/// </summary>
[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
[UIPermission(ElasticSearchIndexPermissions.REBUILD, "Rebuild")]
internal class ElasticSearchApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "Kentico.Xperience.Integrations.ElasticSearch";
}
