using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

using Microsoft.Extensions.DependencyInjection;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing.SearchClients;

public sealed class ElasticSearchIndexClientService(
    ElasticClient indexClient,
    IServiceProvider serviceProvider) : IElasticSearchIndexClientService
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
            await CreateIndexInternalAsync(indexName, elasticSearchStrategy, cancellationToken);
        }

        return indexClient;
    }

    public async Task EditIndexAsync(string oldIndexName, ElasticSearchConfigurationModel newConfiguration,
        CancellationToken cancellationToken)
    {
        var newIndex = ElasticSearchIndexStore.Instance.GetIndex(newConfiguration.IndexName) ??
            throw new InvalidOperationException($"Registered index with name '{oldIndexName}' doesn't exist.");
        var newIndexStrategy = serviceProvider.GetRequiredStrategy(newIndex);

        if (oldIndexName == newIndex.IndexName)
        {
            var updateMappingResponse = await indexClient.Indices
                .PutMappingAsync(new PutMappingRequest(newIndex.IndexName)
                {
                    Properties = newIndexStrategy
                        .MapAnnotatedProperties(new PropertiesDescriptor<IElasticSearchModel>()).Value
                }, cancellationToken);

            if (!updateMappingResponse.IsValid)
            {

            }
        }
        else
        {
            await DeleteIndexInternalAsync(oldIndexName, cancellationToken);
            await CreateIndexInternalAsync(newConfiguration.IndexName, newIndexStrategy, cancellationToken);
        }
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
            // TODO
        }
    }

    private async Task CreateIndexInternalAsync(string indexName, IElasticSearchIndexingStrategy strategy,
        CancellationToken cancellationToken)
    {
        var createResponse = await indexClient.Indices
                .CreateAsync(indexName, c => c
                    .Map(m => m
                        .Dynamic(false)
                        .Properties<IElasticSearchModel>(p => strategy
                            .MapAnnotatedProperties(p))), cancellationToken);

        if (!createResponse.IsValid)
        {
            // TODO
        }
    }
}
