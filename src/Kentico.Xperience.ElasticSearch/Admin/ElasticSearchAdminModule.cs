using CMS;
using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Indexing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: RegisterModule(typeof(ElasticSearchAdminModule))]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Manages administration features and integration.
/// </summary>
internal class ElasticSearchAdminModule : AdminModule
{
    private IElasticSearchConfigurationStorageService storageService = null!;
    private ElasticSearchModuleInstaller installer = null!;

    public ElasticSearchAdminModule() : base(nameof(ElasticSearchAdminModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var options = Service.Resolve<IOptions<ElasticSearchOptions>>();

        if (!options.Value?.SearchServiceEnabled ?? false)
        {
            return;
        }

        RegisterClientModule("kentico", "xperience-integrations-elasticsearch");

        var services = parameters.Services;

        installer = services.GetRequiredService<ElasticSearchModuleInstaller>();
        storageService = services.GetRequiredService<IElasticSearchConfigurationStorageService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        installer.Install();

        ElasticSearchIndexStore.SetIndicies(storageService);
        //AzureSearchIndexAliasStore.SetAliases(storageService);
    }
}
