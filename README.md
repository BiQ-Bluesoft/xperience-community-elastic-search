# Xperience by Kentico Elastic Search

## Description

This integration enables you to create [ElasticSearch](https://www.elastic.co/) search indexes to index content of pages ([content types](https://docs.xperience.io/x/gYHWCQ) with the 'Page' feature enabled) from the Xperience content tree using a code-first approach. To provide a search interface for the indexed content, developers can use the [ElasticSearch .NET](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/introduction.html), [ElasticSearch JavaScript](https://www.elastic.co/guide/en/elasticsearch/client/javascript-api/current/index.html) library.

### Dependencies

- [ASP.NET Core 8.0](https://dotnet.microsoft.com/en-us/download)
- [Xperience by Kentico](https://docs.xperience.io/xp/changelog)
- [Elasticsearch .NET Client](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)

## Package Installation

Add the package to your application using the .NET CLI

```powershell
dotnet add package XperienceCommunity.ElasticSearch
```

## Quick Start

1. Add configuration from your Elastic search to the ASP.NET Core `appsettings.json` file:

   ```json
   "CMSElasticSearch": {
    "SearchServiceEndPoint": "<your index application url>",
    "SearchServiceUsername": "<your index application username>",
    "SearchServicePassword": "<your index application password>"
    }
   ```

2. Define a custom `BaseElasticSearchIndexingStrategy<TSearchModel>` implementation to customize how content pages/content items are processed for the index. See [`Custom-index-strategy.md`](docs/Custom-index-strategy.md)
3. Add this library to the application services, registering your custom `BaseElasticSearchIndexingStrategy` with type parameter `GlobalSearchModel` and Elastic search services

   ```csharp
   // Program.cs
   services.AddXperienceCommunityElasticSearch(builder =>
    {
        builder.RegisterStrategy<GlobalElasticSearchStrategy, GlobalSearchModel>("DefaultStrategy");
    }, configuration);
   ```

4. Create an index in Xperience's Administration within the Search application added by this library.
   ![Administration search edit form](https://raw.githubusercontent.com/BiQ-Bluesoft/xperience-community-elastic-search/refs/heads/main/images/xperience-administration-search-index-edit-form.png)
5. Rebuild the index in Xperience's Administration within the Search application added by this library.
   ![Administration search edit form](https://raw.githubusercontent.com/BiQ-Bluesoft/xperience-community-elastic-search/refs/heads/main/images/xperience-administration-search-index-list.png)
6. Display the results on your site with a Razor View 👍.

## Full Instructions

View the [Usage Guide](./docs/Usage-Guide.md) for more detailed instructions.

## Contributing

Instructions and technical details for contributing to **this** project can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.
