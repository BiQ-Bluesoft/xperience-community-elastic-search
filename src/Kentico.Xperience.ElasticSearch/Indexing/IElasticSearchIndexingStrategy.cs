using Nest;

namespace Kentico.Xperience.ElasticSearch.Indexing;

public interface IElasticSearchIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data
    /// </summary>
    /// <param name="item">The <see cref="IIndexEventItemModel"/> currently being indexed.</param>
    /// <returns>Modified ElasticSearch document.</returns>
    Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item);

    /// <summary>
    /// Triggered by modifications to a web page item, which is provided to determine what other items should be included for indexing
    /// </summary>
    /// <param name="changedItem">The web page item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToElasticSearchModelOrNull"/> for indexing</returns>
    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem);

    /// <summary>
    /// Triggered by modifications to a reusable content item, which is provided to determine what other items should be included for indexing
    /// </summary>
    /// <param name="changedItem">The reusable content item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToElasticSearchModelOrNull"/> for indexing</returns>
    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem);

    IPromise<IProperties> MapAnnotatedProperties(PropertiesDescriptor<IElasticSearchModel> descriptor);

    /// <summary>
    /// Called when uploading data to user defined <see cref="ElasticSearchIndex"/>.
    /// Expects an <see cref="IEnumerable{IElasticSearchModel}"/> created in <see cref="MapToElasticSearchModelOrNull"/>
    /// </summary>
    /// <returns>Number of uploaded documents</returns>
    Task<int> UploadDocuments(IEnumerable<IElasticSearchModel> models, ElasticClient searchClient, string indexName);
}
