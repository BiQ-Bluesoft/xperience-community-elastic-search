using CMS.Core;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Helpers.Constants;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

public sealed class ElasticSearchIndexClientService(
    ElasticsearchClient indexClient,
    IServiceProvider serviceProvider,
    IEventLogService eventLogService) : IElasticSearchIndexClientService
{

    /// <inheritdoc />
    public async Task<ElasticsearchClient> InitializeIndexClient(string indexName, CancellationToken cancellationToken)
    {
        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var elasticSearchStrategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);

        var indexExistsInElastic = (await indexClient.Indices.ExistsAsync(indexName, cancellationToken))?.Exists ?? false;
        if (!indexExistsInElastic)
        {
            await elasticSearchStrategy.CreateIndexInternalAsync(indexClient, indexName, cancellationToken);
        }

        return indexClient;
    }

    /// <inheritdoc />
    public async Task EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration,
        CancellationToken cancellationToken)
    {
        var newIndex = ElasticSearchIndexStore.Instance.GetIndex(newConfiguration.IndexName) ??
            throw new InvalidOperationException($"Registered index with name '{oldIndexName}' doesn't exist.");
        var newIndexStrategy = serviceProvider.GetRequiredStrategy(newIndex);

        await DeleteIndexInternalAsync(oldIndexName, cancellationToken);
        await newIndexStrategy.CreateIndexInternalAsync(indexClient, newConfiguration.IndexName, cancellationToken);
    }

    private async Task DeleteIndexInternalAsync(string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        eventLogService.LogInformation(
            nameof(ElasticSearchIndexClientService),
            EventLogConstants.ElasticDeleteEventCode,
            $"Deletion of index {indexName} started.");

        var deleteIndexResponse = await indexClient.Indices.DeleteAsync(indexName, cancellationToken);
        if (!deleteIndexResponse.IsValidResponse)
        {
            eventLogService.LogError(
                nameof(DeleteIndexInternalAsync),
                EventLogConstants.ElasticDeleteEventCode,
                $"Unable to delete index with name: {indexName}. Operation failed with error: {deleteIndexResponse.DebugInformation}");
        }
    }
}
