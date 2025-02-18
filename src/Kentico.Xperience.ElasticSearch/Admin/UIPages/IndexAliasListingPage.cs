using CMS.Core;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.ElasticSearch.Admin;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Aliasing;
using Kentico.Xperience.ElasticSearch.Indexing;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

using Microsoft.Extensions.Options;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: "indexes",
   uiPageType: typeof(IndexAliasListingPage),
   name: "List of registered Elastic Search index aliases",
   templateName: TemplateNames.LISTING,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// An admin UI page that displays statistics about the registered ElasticSearch indexes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IndexListingPage"/> class.
/// </remarks>
[UIEvaluatePermission(SystemPermissions.VIEW)]
internal class IndexAliasListingPage(
    IElasticSearchClient elasticSearchClient,
    IElasticSearchIndexAliasService elasticSearchIndexAliasService,
    IOptions<ElasticSearchOptions> elasticSearchOptions,
    IPageLinkGenerator pageLinkGenerator,
    IElasticSearchConfigurationStorageService configurationStorageService) : ListingPage
{
    private readonly ElasticSearchOptions elasticSearchOptions = elasticSearchOptions.Value;

    protected override string ObjectType => ElasticSearchIndexAliasItemInfo.OBJECT_TYPE;

    /// <inheritdoc/>
    public override async Task ConfigurePage()
    {
        if (!elasticSearchOptions.SearchServiceEnabled)
        {
            PageConfiguration.Callouts =
            [
                new()
                {
                    Headline = "Indexing is disabled",
                    Content = "Indexing is disabled. See <a target='_blank' href='https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch'>our instructions</a> to read more about ElasticSearch alias indexes.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            ];
        }
        else
        {
            if (!ElasticSearchIndexAliasStore.Instance.GetAllAliases().Any())
            {
                PageConfiguration.Callouts =
                [
                    new()
                    {
                        Headline = "No aliases",
                        Content = "No ElasticSearch index aliases registered. See <a target='_blank' href='https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch'>our instructions</a> to read more about creating and registering ElasticSearch alias indexes.",
                        ContentAsHtml = true,
                        Type = CalloutType.FriendlyWarning,
                        Placement = CalloutPlacement.OnDesk
                    }
                ];
            }

            PageConfiguration.ColumnConfigurations
                .AddColumn(nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemId), "ID", defaultSortDirection: SortTypeEnum.Asc, sortable: true)
                .AddColumn(nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemIndexAliasName), "Name", sortable: true, searchable: true);

            PageConfiguration.AddEditRowAction<IndexAliasEditPage>();
            PageConfiguration.TableActions.AddCommand("Rebuild Aliased Index", nameof(Rebuild), icon: Icons.RotateRight);
            PageConfiguration.TableActions.AddDeleteAction(nameof(Delete), "Delete Alias");
            PageConfiguration.HeaderActions.AddLink<IndexAliasCreatePage>("Create Alias");
            PageConfiguration.HeaderActions.AddLink<IndexListingPage>("Indexes");
        }

        await base.ConfigurePage();
    }

    /// <summary>
    /// A page command which rebuilds an ElasticSearch index.
    /// </summary>
    /// <param name="id">The ID of the row whose action was performed, which corresponds with the internal
    /// <see cref="ElasticSearchIndex.Identifier"/> to rebuild.</param>
    /// <param name="cancellationToken">The cancellation token for the action.</param>
    [PageCommand(Permission = ElasticSearchIndexPermissions.REBUILD)]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id, CancellationToken cancellationToken)
    {
        var result = new RowActionResult(false);
        var alias = ElasticSearchIndexAliasStore.Instance.GetAlias(id);

        if (alias == null)
        {
            return ResponseFrom(result)
                .AddErrorMessage(string.Format("Error loading ElasticSearch index alias with identifier {0}.", id));
        }

        foreach (var indexName in alias.IndexNames)
        {
            var index = ElasticSearchIndexStore.Instance.GetIndex(indexName);

            if (index is null)
            {
                return ResponseFrom(result)
                    .AddErrorMessage(string.Format("Error loading ElasticSearch aliased index with name {0}.", indexName));
            }

            try
            {
                await elasticSearchClient.StartRebuildAsync(index.IndexName, cancellationToken);
            }
            catch (Exception ex)
            {
                EventLogService.LogException(nameof(IndexAliasListingPage), nameof(Rebuild), ex);

                return ResponseFrom(result)
                   .AddErrorMessage(string.Format("Errors occurred while rebuilding the '{0}' index. Please check the Event Log for more details.", index.IndexName));
            }
        }
        return ResponseFrom(result)
                    .AddSuccessMessage("Indexing in progress. Visit your ElasticSearch dashboard for details about the indexing process.");
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<INavigateResponse> Delete(int id, CancellationToken cancellationToken)
    {
        var response = NavigateTo(pageLinkGenerator.GetPath<IndexAliasListingPage>());
        var alias = ElasticSearchIndexAliasStore.Instance.GetAlias(id);
        if (alias == null)
        {
            return response
                .AddErrorMessage(string.Format("Error deleting ElasticSearch index alias with identifier {0}.", id));
        }
        try
        {
            var res = configurationStorageService.TryDeleteAlias(id);

            if (res)
            {
                ElasticSearchIndexAliasStore.SetAliases(configurationStorageService);

                await elasticSearchIndexAliasService.DeleteAliasAsync(alias.AliasName, cancellationToken);
            }
            else
            {
                return response
                    .AddErrorMessage(string.Format("Error deleting ElasticSearch index with identifier {0}.", id));
            }

            return response.AddSuccessMessage("Index deletion in progress. Visit your Elastic dashboard for details about your indexes.");
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), nameof(Delete), ex);

            return response
               .AddErrorMessage(string.Format("Errors occurred while deleting the '{0}' index. Please check the Event Log for more details.", alias.IndexNames));
        }
    }
}
