using CMS.Core;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace Kentico.Xperience.ElasticSearch.Indexing.Strategies;

/// <summary>
/// Default indexing strategy that provides simple indexing.
/// Search model <typeparamref name="TSearchModel"/> used for Index definition and data retrieval.
/// </summary>
public class BaseElasticSearchIndexingStrategy<TSearchModel>() : IElasticSearchIndexingStrategy where TSearchModel : class, IElasticSearchModel, new()
{
    private readonly IEventLogService eventLogService = Service.Resolve<IEventLogService>();

    /// <inheritdoc />
    public virtual Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
    {
        if (item.IsSecured)
        {
            return Task.FromResult<IElasticSearchModel?>(null);
        }

        var indexDocument = new TSearchModel()
        {
            Name = item.Name
        };

        return Task.FromResult<IElasticSearchModel?>(indexDocument);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem) => await Task.FromResult(new List<IIndexEventItemModel>() { changedItem });

    /// <inheritdoc />
    public virtual async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem) => await Task.FromResult(new List<IIndexEventItemModel>());

    /// <inheritdoc />
    public async Task<int> UploadDocumentsAsync(IEnumerable<IElasticSearchModel> models, ElasticsearchClient searchClient, string indexName)
    {
        var bulkIndexResponse = await searchClient.BulkAsync(bulk =>
        {
            foreach (var model in models)
            {
                bulk.Index((TSearchModel)model, idx => idx
                    .Index(indexName)
                    .Id(model.GetId())
                );
            }
        });

        if (bulkIndexResponse.Errors)
        {
            var failedItems = bulkIndexResponse.ItemsWithErrors;
            foreach (var item in failedItems)
            {
                // Additional work - Discuss whether exception should be thrown or logging the error is enough.
                eventLogService.LogError(
                    nameof(UploadDocumentsAsync),
                    "ELASTIC_SEARCH",
                    $"Unable to upload document {item.Id} to index with name {indexName}. Operation failed errors: {item.Error?.Reason}");
            }
        }
        return bulkIndexResponse.Items.Count(x => x.IsValid);
    }

    /// <inheritdoc />
    public async Task CreateIndexInternalAsync(ElasticsearchClient indexClient, string indexName, CancellationToken cancellationToken)
    {
        var createResponse = await indexClient.Indices.CreateAsync<TSearchModel>(indexName, cancellationToken);

        if (!createResponse.IsValidResponse)
        {
            // Additional work - Discuss whether exception should be thrown or logging the error is enough.
            eventLogService.LogError(
                nameof(CreateIndexInternalAsync),
                "ELASTIC_SEARCH",
                $"Unable to create index with name: {indexName}. Operation failed with error: {createResponse.DebugInformation}");
        }
    }
}
