using CMS;
using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using XperienceCommunity.ElasticSearch.Admin;
using XperienceCommunity.ElasticSearch.Admin.Services;
using XperienceCommunity.ElasticSearch.Aliasing;
using XperienceCommunity.ElasticSearch.Indexing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: RegisterModule(typeof(ElasticSearchAdminModule))]

namespace XperienceCommunity.ElasticSearch.Admin;

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

        var services = parameters.Services;

        var options = services.GetRequiredService<IOptions<ElasticSearchOptions>>();

        if (!options.Value?.SearchServiceEnabled ?? false)
        {
            return;
        }

        RegisterClientModule("xperience-community", "xperience-community-elasticsearch");

        installer = services.GetRequiredService<ElasticSearchModuleInstaller>();
        storageService = services.GetRequiredService<IElasticSearchConfigurationStorageService>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        installer.Install();

        ElasticSearchIndexStore.SetIndices(storageService);
        ElasticSearchIndexAliasStore.SetAliases(storageService);
    }
}
