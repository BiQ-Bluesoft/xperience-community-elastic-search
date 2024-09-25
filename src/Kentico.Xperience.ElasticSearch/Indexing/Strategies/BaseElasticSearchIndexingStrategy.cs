using System.Reflection;

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

    public IPromise<IProperties> MapAnnotatedProperties(PropertiesDescriptor<IElasticSearchModel> descriptor)
    {
        var type = typeof(TSearchModel);

        foreach (var prop in type.GetProperties())
        {

            // Ignore Attribute
            var ignoreAttr = prop.GetCustomAttribute<IgnoreAttribute>();
            if (ignoreAttr != null)
            {
                continue;
            }

            // Text Attribute
            var textAttr = prop.GetCustomAttribute<TextAttribute>();
            if (textAttr != null)
            {
                descriptor.Text(t => t
                    .Name(textAttr.Name ?? prop.Name)
                    .Analyzer(textAttr.Analyzer)
                    .SearchAnalyzer(textAttr.SearchAnalyzer)
                );
                continue;
            }

            // Keyword Attribute
            var keywordAttr = prop.GetCustomAttribute<KeywordAttribute>();
            if (keywordAttr != null)
            {
                descriptor.Keyword(k => k.Name(keywordAttr.Name ?? prop.Name));
                continue;
            }

            // Number Attribute
            var numberAttr = prop.GetCustomAttribute<NumberAttribute>();
            if (numberAttr != null)
            {
                descriptor.Number(n => n
                    .Name(numberAttr.Name ?? prop.Name)
                );
                continue;
            }

            // Date Attribute
            var dateAttr = prop.GetCustomAttribute<DateAttribute>();
            if (dateAttr != null)
            {
                descriptor.Date(d => d
                    .Name(dateAttr.Name ?? prop.Name)
                    .Format(dateAttr.Format)
                );
                continue;
            }

            // GeoPoint Attribute
            var geoAttr = prop.GetCustomAttribute<GeoPointAttribute>();
            if (geoAttr != null)
            {
                descriptor.GeoPoint(d => d
                    .Name(geoAttr.Name ?? prop.Name)
                );
                continue;
            }

            // Join Attribute
            var joinAttr = prop.GetCustomAttribute<JoinAttribute>();
            if (joinAttr != null)
            {
                descriptor.Join(j => j
                    .Name(joinAttr.Name ?? prop.Name));
                continue;
            }

            // Completion Attribute
            var completionAttr = prop.GetCustomAttribute<CompletionAttribute>();
            if (completionAttr != null)
            {
                descriptor.Completion(c => c
                    .Name(completionAttr.Name ?? prop.Name));
            }
        }

        return descriptor;
    }
}
