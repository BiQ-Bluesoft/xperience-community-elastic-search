using CMS.Core;

using Kentico.Xperience.ElasticSearch.Admin.Models;

using Microsoft.Extensions.DependencyInjection;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

public sealed class ElasticSearchIndexClientService(
    ElasticClient indexClient,
    IServiceProvider serviceProvider,
    IEventLogService eventLogService) : IElasticSearchIndexClientService
{

    /// <inheritdoc />
    public async Task<ElasticClient> InitializeIndexClient(string indexName, CancellationToken cancellationToken)
    {
        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var elasticSearchStrategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);

        var indexExistsInElastic = (await indexClient.Indices.ExistsAsync(indexName, ct: cancellationToken))?.Exists ?? false;
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

        var deleteIndexResponse = await indexClient.Indices.DeleteAsync(indexName, null, cancellationToken);
        if (!deleteIndexResponse.IsValid)
        {
            // Additional work - Discuss whether exception should be thrown or logging the error is enough.
            eventLogService.LogError(
                nameof(DeleteIndexInternalAsync),
                "ELASTIC_SEARCH",
                $"Unable to delete index with name: {indexName}. Operation failed with error: {deleteIndexResponse.OriginalException}");
        }
    }
}
