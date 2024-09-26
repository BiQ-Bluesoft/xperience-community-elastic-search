using Kentico.Xperience.ElasticSearch.Indexing.Models;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing.Strategies;

/// <summary>
/// Default indexing strategy that provides simple indexing.
/// Search model <typeparamref name="TSearchModel"/> used for Index definition and data retrieval.
/// </summary>
public class BaseElasticSearchIndexingStrategy<TSearchModel>() : IElasticSearchIndexingStrategy where TSearchModel : class, IElasticSearchModel, new()
{
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
    public async Task<int> UploadDocuments(IEnumerable<IElasticSearchModel> models, ElasticClient searchClient, string indexName)
    {
        var bulkDescriptor = new BulkDescriptor();
        foreach (var model in models)
        {
            bulkDescriptor.Index<IElasticSearchModel>(op => op
                .Index(indexName)
                .Document(model));
        }
        var bulkResponse = await searchClient.BulkAsync(bulkDescriptor);
        if (!bulkResponse.IsValid)
        {
            var failedItems = bulkResponse.ItemsWithErrors;
            foreach (var item in failedItems)
            {
                // TODO
                Console.WriteLine($"Operation {item.Operation} failed for document {item.Id} with error: {item.Error?.Reason}");
            }
        }
        return bulkResponse.Items.Count(x => x.IsValid);
    }

    public async Task CreateIndexInternalAsync(ElasticClient indexClient, string indexName, CancellationToken cancellationToken)
    {
        var createResponse = await indexClient.Indices
                .CreateAsync(indexName, c => c
                    .Map<TSearchModel>(m => m.AutoMap()), cancellationToken);

        if (!createResponse.IsValid)
        {
            //TODO
        }
    }
}
