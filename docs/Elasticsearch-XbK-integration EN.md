# Xperience by Kentico + Elasticsearch

Search is becoming a key element of nearly every web application today. Xperience by Kentico offers several ways to effectively perform full-text search and filter content managed in the CMS.

Aside from paid cloud-based tools like Azure AI Search, Algolia, the only free on-premise option until now has been Lucene-based search integration. This limitation motivated us to implement Elasticsearch as an alternative tool that supports on-premise hosting. Elasticsearch is completely free under the Elastic License 2.0 and [open-source AGPL](https://www.elastic.co/blog/elasticsearch-is-open-source-again).

Elasticsearch is a distributed system built on Apache Lucene. It excels in scalability, flexibility, and a broad range of capabilities for processing large volumes of data and complex search queries.

[comment]: <> "In addition to the free option, it is also available as a fully managed cloud service, making it a universal solution for both smaller projects and large enterprise applications."

# Elasticsearch for XbyK

Integrating an on-premise Elasticsearch instance can be done in a few straightforward steps. In this article, we’ll walk through the necessary steps to set up Elasticsearch, configure indexing and data mapping from XbyK, and implement the search functionality.

## 1. Installing Packages

First, add the NuGet package by running the following command in your terminal:

```
dotnet add package XperienceCommunity.ElasticSearch
```

## 2. Elasticsearch Configuration

> **Note:** This article assumes that Elasticsearch is already running on your device. Setting up Elasticsearch for local development is straightforward—see the [official quickstart guide](https://www.elastic.co/docs/deploy-manage/deploy/self-managed/local-development-installation-quickstart) for easy installation instructions.

Next, add the following configuration to your application’s `appsettings.json`, including the endpoint of the running Elasticsearch instance and authentication credentials. Authentication can be done using either a username and password or an API key, which can be generated in the Kibana interface.

```json
"CMSElasticSearch": {
  "SearchServiceEnabled": true,
  "SearchServiceEndPoint": "<your index application url>",
  "SearchServiceAPIKey": "<your API Key for Elasticsearch>"
}
```

Alternatively, you can use a username and password instead of an API key:

```json
"CMSElasticSearch": {
  // ...
  "SearchServiceUsername": "<your index application username>",
  "SearchServicePassword": "<your index application password>"
}
```

## 3. Creating the Model and Strategy

The core functionality of this library is based on the concept of a custom indexing strategy, which fully adapts to your content model and desired search behavior. This strategy lets you define exactly what data gets indexed, how it is mapped into Elasticsearch, and how it reacts to content changes. The following steps show how to configure this process using provided interfaces and methods.

### Custom Index Model

Define your own search model by extending the `BaseElasticSearchModel` provided by the library. This model will be used to create the index in Elasticsearch.

```csharp
public class DancingGoatSearchModel : BaseElasticSearchModel
{
    public string Title { get; set; }

    public string Content { get; set; }
}
```

### Implementing the Indexing Strategy

Create a custom implementation of `BaseElasticSearchIndexingStrategy<TSearchModel>` to customize how web page or content items are processed for indexing.

```csharp
public class DancingGoatSearchStrategy(...) : BaseElasticSearchIndexingStrategy<DancingGoatSearchModel>
{
    ...
}
```

#### Configuring Fields (TypeMapping)

Define how the fields of your model are stored in the Elasticsearch index. Override the `Mapping(TypeMappingDescriptor<TSearchModel> descriptor)` method to specify data types and behavior—e.g., whether a field is used for full-text search (`text`) or exact filtering (`keyword`).

```csharp
public override void Mapping(TypeMappingDescriptor<DancingGoatSearchModel> descriptor) =>
    descriptor
        .Properties(props => props
            .Keyword(x => x.Title)
            .Text(x => x.Content));
```

You can find the complete list of types in the [official Elasticsearch documentation](https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/field-data-types).

#### Mapping Content to the Search Model

Next, define how content properties map to your custom index model. Override the method `Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)` and implement mapping to your `BaseElasticSearchModel`-based class (in this case, `DancingGoatSearchModel`). Common base properties are mapped automatically, so you only need to handle custom fields.

Here’s an example showing how to map an `ArticlePage` with an `ArticleTitle` property and the page’s raw content to `DancingGoatSearchModel`:

```csharp
public override async Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
{
    var result = new DancingGoatSearchModel();

    if (item is not IndexEventWebPageItemModel indexedPage)
    {
        return null;
    }

    if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
    {
        var page = await strategyHelper.GetPage<ArticlePage>(
            indexedPage.ItemGuid,
            indexedPage.WebsiteChannelName,
            indexedPage.LanguageName,
            ArticlePage.CONTENT_TYPE_NAME);

        if (page is null)
        {
            return null;
        }

        result.Title = page.ArticleTitle ?? string.Empty;
        var rawContent = await webCrawler.CrawlWebPage(page!);
        result.Content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
    }

    return result;
}
```

`IIndexEventItemModel` is an abstract type representing an item being processed for indexing. This includes `IndexEventWebPageItemModel` for web page items and `IndexEventReusableItemModel` for reusable content items.

You can retrieve content using methods like `GetPage<T>` from the `StrategyHelper` class, as shown in this example:
[Data retrieval during indexing](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#data-retrieval-during-indexing).

#### Updating Related Content in the Index

Direct edits to a CMS item automatically trigger events that update the corresponding index record. But what if related content changes—such as a reusable item used on multiple pages?

In such cases, implement logic to determine which items must be reindexed due to the change. Use the `FindItemsToReindex` method for this purpose. All returned items will be passed to `MapToElasticSearchModelOrNull(IIndexEventItemModel item)` for reindexing.

This method is essential for maintaining data consistency when related content is updated. Example implementation:
[Keeping indexed related content up to date](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Custom-index-strategy.md?ref_type=heads#keeping-indexed-related-content-up-to-date).

#### Registering the Strategy

Register your custom strategy using dependency injection (DI):

```csharp
services.AddKenticoElasticSearch(builder =>
{
    builder.RegisterStrategy<DancingGoatSearchStrategy, DancingGoatSearchModel>(nameof(DancingGoatSearchStrategy));
}, configuration);
```

## 4. Index Configuration in the XbyK Admin Interface

Next, create the index in the Xperience admin interface using the _Elastic Search_ application added by this library. Here, set the index name, choose the indexing strategy, select language variants, channels, and content types to be indexed.

![XbyK create index](/images/xperience-administration-search-index-edit-form.png)

After configuration, run the **Rebuild** action from the list of registered Elastic Search indices to populate the index.

![XbyK rebuild index](/images/xperience-administration-search-index-list.png)

After this step, the index should be populated with items (based on your `DancingGoatSearchStrategy`) and ready for searching and filtering.

Documentation link:
[https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Indexes.md](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Indexes.md)

## 5. Performing Searches

The final step is implementing the search functionality.

Use the `IElasticSearchQueryClientService` to perform a search with custom search options. Define your search parameters and query data from the Elasticsearch index:

```csharp
var index = searchClientService.CreateSearchClientForQueries(indexName);

page = Math.Max(page, 1);
pageSize = Math.Max(1, pageSize);

var request = new SearchRequest(indexName)
{
    From = (page - 1) * pageSize,
    Size = pageSize,
    Query = string.IsNullOrEmpty(searchText)
        ? new MatchAllQuery()
        : new MultiMatchQuery()
        {
            Fields = new[]
            {
                nameof(DancingGoatSearchModel.Title).ToLower(),
            },
            Query = searchText,
        },
    TrackTotalHits = new TrackHits(true)
};

var response = await index.SearchAsync<DancingGoatSearchModel>(request);
```

The search uses the standard Elasticsearch .NET Client (v8) and supports both Fluent API and Object Initializer API. Differences between these approaches are illustrated here:
[https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/query](https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/query)

## Conclusion

Integrating Elasticsearch into Xperience by Kentico expands the search capabilities and offers a level of flexibility not available with other full-text tools. This integration allows fast indexing, advanced querying, and on-premise hosting—an important benefit for users who need full control over their data. As the technology evolves, Elasticsearch is expected to become an even more attractive solution for search use cases in Xperience by Kentico.

We hope this article helped you better understand how to integrate Elasticsearch with Xperience by Kentico. However, the possibilities of this integration go beyond what’s covered here. The documentation includes examples for [web page crawling](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Scraping-web-page-content.md?ref_type=heads#scraping-web-page-content) and managing [index aliases](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch/-/blob/IN-654-Elastic-search-from-azure/docs/Managing-Aliases.md?ref_type=heads#managing-aliases).

You can find a more detailed guide for creating a custom indexing strategy, including code snippets, data mapping techniques, and integration with Kentico Xperience directly in the official GitLab repository:
[https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch](https://gitlab.bluesoft.cz/oss/xperience-by-kentico-elasticsearch)
