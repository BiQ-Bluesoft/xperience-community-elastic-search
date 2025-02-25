using CMS.Core;

using Elastic.Clients.Elasticsearch;

using Kentico.Xperience.ElasticSearch.Helpers;
using Kentico.Xperience.ElasticSearch.Helpers.Constants;
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
                eventLogService.LogError(
                    nameof(BaseElasticSearchIndexingStrategy<TSearchModel>),
                    EventLogConstants.ElasticItemsAddEventCode,
                    $"Unable to upload document {item.Id} to index with name {indexName}. Operation failed errors: {item.Error?.Reason}");
            }
        }
        return bulkIndexResponse.Items.Count(x => x.IsValid);
    }

    /// <inheritdoc />
    public async Task<ElasticSearchResponse> CreateIndexInternalAsync(ElasticsearchClient indexClient, string indexName, CancellationToken cancellationToken)
    {
        eventLogService.LogInformation(
            nameof(BaseElasticSearchIndexingStrategy<TSearchModel>),
            EventLogConstants.ElasticCreateEventCode,
            $"Creation of index {indexName} started");

        var createResponse = await indexClient.Indices.CreateAsync<TSearchModel>(indexName, cancellationToken);
        if (!createResponse.IsValidResponse)
        {
            eventLogService.LogError(
                nameof(BaseElasticSearchIndexingStrategy<TSearchModel>),
                EventLogConstants.ElasticCreateEventCode,
                $"Unable to create index with name: {indexName}. Operation failed with error: {createResponse.DebugInformation}");
            return ElasticSearchResponse.Failure($"Unable to create index with name: {indexName}.");
        }
        return ElasticSearchResponse.Success();
    }
}
