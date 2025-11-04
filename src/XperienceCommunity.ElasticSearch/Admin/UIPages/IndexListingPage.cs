using CMS.Core;
using CMS.Helpers;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Options;

using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchIndexItem;
using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Admin.Services;
using XperienceCommunity.ElasticSearch.Admin.UIPages;
using XperienceCommunity.ElasticSearch.Helpers.Constants;
using XperienceCommunity.ElasticSearch.Indexing;
using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.SearchClients;

[assembly: UIPage(
   parentType: typeof(ElasticSearchApplicationPage),
   slug: "indexes",
   uiPageType: typeof(IndexListingPage),
   name: "List of registered Elastic Search indices",
   templateName: TemplateNames.LISTING,
   order: UIPageOrder.NoOrder)]

namespace XperienceCommunity.ElasticSearch.Admin.UIPages;

/// <summary>
/// An admin UI page that displays statistics about the registered ElasticSearch indexes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IndexListingPage"/> class.
/// </remarks>
[UIEvaluatePermission(SystemPermissions.VIEW)]
internal class IndexListingPage(
    IElasticSearchClient elasticSearchClient,
    IPageLinkGenerator pageLinkGenerator,
    IOptions<ElasticSearchOptions> elasticSearchOptions,
    IElasticSearchConfigurationStorageService configurationStorageService) : ListingPage
{
    private readonly ElasticSearchOptions elasticSearchOptions = elasticSearchOptions.Value;

    protected override string ObjectType => ElasticSearchIndexItemInfo.OBJECT_TYPE;

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
                    Content = "Indexing is disabled. See <a target='_blank' href='https://github.com/BiQ-Bluesoft/xperience-community-elastic-search'>our instructions</a> to read more about ElasticSearch alias indexes.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            ];
        }
        else
        {

            if (!ElasticSearchIndexStore.Instance.GetAllIndices().Any())
            {
                PageConfiguration.Callouts =
                [
                    new()
                    {
                        Headline = "No indexes",
                        Content = "No ElasticSearch indexes registered. See <a target='_blank' href='https://github.com/BiQ-Bluesoft/xperience-community-elastic-search'>our instructions</a> to read more about creating and registering ELasticSearch indexes.",
                        ContentAsHtml = true,
                        Type = CalloutType.FriendlyWarning,
                        Placement = CalloutPlacement.OnDesk
                    }
                ];
            }

            PageConfiguration.ColumnConfigurations
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId), "ID", defaultSortDirection: SortTypeEnum.Asc, sortable: true)
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemIndexName), "Name", sortable: true, searchable: true)
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemChannelName), "Channel", searchable: true, sortable: true)
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemStrategyName), "Index Strategy", searchable: true, sortable: true)
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId), "Entries", sortable: true)
                .AddColumn(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemLastRebuild), "Last Rebuild", sortable: true);

            PageConfiguration.AddEditRowAction<IndexEditPage>();
            PageConfiguration.TableActions.AddCommand("Rebuild", nameof(Rebuild), icon: Icons.RotateRight);
            PageConfiguration.TableActions.AddDeleteAction(nameof(Delete), "Delete");
            PageConfiguration.HeaderActions.AddLink<IndexCreatePage>("Create Index");
            PageConfiguration.HeaderActions.AddLink<IndexAliasListingPage>("Index Aliases");
        }

        await base.ConfigurePage();
    }

    protected override async Task<LoadDataResult> LoadData(LoadDataSettings settings, CancellationToken cancellationToken)
    {
        if (!elasticSearchOptions.SearchServiceEnabled)
        {
            return new();
        }

        var result = await base.LoadData(settings, cancellationToken);

        var statistics = await elasticSearchClient.GetStatisticsAsync(default);
        // Add statistics for indexes that are registered but not created in ElasticSearch
        AddMissingStatistics(ref statistics);

        if (PageConfiguration.ColumnConfigurations is not List<ColumnConfiguration> columns)
        {
            return result;
        }

        var entriesColIndex = columns.FindIndex(c => c.Caption == "Entries");

        foreach (var row in result.Rows)
        {
            if (row.Cells is not List<Cell> cells)
            {
                continue;
            }

            var stats = GetStatistic(row, statistics);

            if (stats is null)
            {
                continue;
            }

            if (cells[entriesColIndex] is StringCell entriesCell)
            {
                entriesCell.Value = stats.Entries.ToString();
            }
        }

        return result;
    }

    private ElasticSearchIndexStatisticsViewModel? GetStatistic(Row row, ICollection<ElasticSearchIndexStatisticsViewModel> statistics)
    {
        var indexId = ValidationHelper.GetInteger(row.Identifier, 0);
        var indexName = ElasticSearchIndexStore.Instance.GetIndex(indexId) is ElasticSearchIndex index
            ? index.IndexName
            : string.Empty;

        return statistics.FirstOrDefault(s => string.Equals(s.Name, indexName, StringComparison.OrdinalIgnoreCase));
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
        var index = ElasticSearchIndexStore.Instance.GetIndex(id);

        if (index is null)
        {
            return ResponseFrom(result)
                .AddErrorMessage(string.Format("Error loading ElasticSearch index with identifier {0}.", id));
        }
        try
        {
            var elasticResponse = await elasticSearchClient.StartRebuildAsync(index.IndexName, cancellationToken);
            if (!elasticResponse.IsSuccess)
            {
                return ResponseFrom(result)
                    .AddErrorMessage($"Errors occurred while rebuilding the '{index.IndexName}' index. Please check the Event Log for more details.");
            }

            return ResponseFrom(result)
                .AddSuccessMessage("Indexing in progress. Visit your ElasticSearch dashboard for details about the indexing process.");
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), EventLogConstants.ElasticRebuildEventCode, ex);

            return ResponseFrom(result)
               .AddErrorMessage(string.Format("Errors occurred while rebuilding the '{0}' index. Please check the Event Log for more details.", index.IndexName));
        }
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<INavigateResponse> Delete(int id, CancellationToken cancellationToken)
    {
        var response = NavigateTo(pageLinkGenerator.GetPath<IndexListingPage>());
        var index = ElasticSearchIndexStore.Instance.GetIndex(id);
        if (index == null)
        {
            return response
                .AddErrorMessage(string.Format("Error deleting ElasticSearch index with identifier {0}.", id));
        }
        try
        {
            if (configurationStorageService.TryDeleteIndex(id))
            {
                var elasticResponse = await elasticSearchClient.DeleteIndexAsync(index.IndexName, cancellationToken);
                if (!elasticResponse.IsSuccess)
                {
                    response.AddErrorMessage(elasticResponse.ErrorMessage);
                }
                ElasticSearchIndexStore.SetIndices(configurationStorageService);
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
            EventLogService.LogException(nameof(IndexListingPage), EventLogConstants.ElasticDeleteEventCode, ex);
            return response
               .AddErrorMessage(string.Format("Errors occurred while deleting the '{0}' index. Please check the Event Log for more details.", index.IndexName));
        }
    }

    private static void AddMissingStatistics(ref ICollection<ElasticSearchIndexStatisticsViewModel> statistics)
    {
        foreach (var indexName in ElasticSearchIndexStore.Instance.GetAllIndices().Select(i => i.IndexName))
        {
            if (!statistics.Any(stat => stat.Name?.Equals(indexName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                statistics.Add(new ElasticSearchIndexStatisticsViewModel
                {
                    Name = indexName,
                    Entries = 0,
                });
            }
        }
    }
}
