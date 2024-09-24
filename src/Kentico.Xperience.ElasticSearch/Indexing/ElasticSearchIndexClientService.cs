//using Kentico.Xperience.AzureSearch.Admin;

using Microsoft.Extensions.DependencyInjection;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing;

public sealed class ElasticSearchIndexClientService : IElasticSearchIndexClientService
{
    private readonly ElasticClient indexClient;
    private readonly IServiceProvider serviceProvider;

    public ElasticSearchIndexClientService(ElasticClient indexClient, IServiceProvider serviceProvider)
    {
        this.indexClient = indexClient;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<ElasticClient> InitializeIndexClient(string indexName, CancellationToken cancellationToken)
    {
        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName) ??
            throw new InvalidOperationException($"Registered index with name '{indexName}' doesn't exist.");

        var elasticSearchStrategy = serviceProvider.GetRequiredStrategy(elasticSearchIndex);

        // TODO Add check if index already exists
        var createResponse = await indexClient.Indices
            .CreateAsync(indexName, c => c
                .Map(m => m
                    .Dynamic(false)
                    .Properties<IElasticSearchModel>(p => elasticSearchStrategy
                        .MapAnnotatedProperties(p))), cancellationToken);

        if (!createResponse.IsValid)
        {
            // TODO
        }
        return indexClient;
    }

    /// <inheritdoc />
    //public async Task EditIndex(string oldIndexName, ElasticSearchConfigurationModel newIndexConfiguration, CancellationToken cancellationToken)
    //{
    //    var oldIndex = ElasticSearchIndexStore.Instance.GetIndex(oldIndexName) ??
    //        throw new InvalidOperationException($"Registered index with name '{oldIndexName}' doesn't exist.");
    //    var oldStrategy = serviceProvider.GetRequiredStrategy(oldIndex);
    //    var oldSearchFields = oldStrategy.GetSearchFields();

    //    var newIndex = ElasticSearchIndexStore.Instance.GetIndex(newIndexConfiguration.IndexName) ??
    //        throw new InvalidOperationException($"Registered index with name '{oldIndexName}' doesn't exist.");
    //    var newStrategy = serviceProvider.GetRequiredStrategy(newIndex);
    //    var newSearchFields = newStrategy.GetSearchFields();

    //    if (Enumerable.SequenceEqual(oldSearchFields, newSearchFields, new AzureSearchIndexComparer()))
    //    {
    //        await DeleteIndex(oldIndexName, cancellationToken);
    //    }

    //    await CreateOrUpdateIndexInternal(newSearchFields, newStrategy, newIndex.IndexName, cancellationToken);
    //}

    private async Task DeleteIndex(string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        await indexClient.Indices.DeleteAsync(indexName, null, cancellationToken);
    }
}
