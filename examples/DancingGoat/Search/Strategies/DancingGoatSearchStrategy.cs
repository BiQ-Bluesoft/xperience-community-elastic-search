using DancingGoat.Models;
using DancingGoat.Search.Models;
using DancingGoat.Search.Services;

using Elastic.Clients.Elasticsearch.Mapping;

using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.Strategies;

using Microsoft.IdentityModel.Tokens;

namespace DancingGoat.Search.Strategies;

public class DancingGoatSearchStrategy(
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler,
    StrategyHelper strategyHelper
    ) : BaseElasticSearchIndexingStrategy<DancingGoatSearchModel>
{
    public override async Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
    {
        var result = new DancingGoatSearchModel();

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }

        if (string.Equals(item.ContentTypeName, CafePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            // The implementation of GetPage<T>() is below
            var page = await strategyHelper.GetPage<CafePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName,
                CafePage.CONTENT_TYPE_NAME);

            if (page is null)
            {
                return null;
            }

            result.Title = page.CafeTitle ?? "";
            var rawContent = await webCrawler.CrawlWebPage(page!);
            result.Content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
        }
        else if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            // The implementation of GetPage<T>() is below
            var page = await strategyHelper.GetPage<ArticlePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName,
                ArticlePage.CONTENT_TYPE_NAME);

            if (page is null)
            {
                return null;
            }

            result.Title = page.ArticleTitle ?? "";
            var rawContent = await webCrawler.CrawlWebPage(page!);
            result.Content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
        }
        else if (string.Equals(item.ContentTypeName, HomePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await strategyHelper.GetPage<HomePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName,
                HomePage.CONTENT_TYPE_NAME);

            if (page is null || page.HomePageBanner.IsNullOrEmpty())
            {
                return null;
            }

            result.Title = page!.HomePageBanner.First().BannerHeaderText;
            var rawContent = await webCrawler.CrawlWebPage(page!);
            result.Content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
        }
        else
        {
            return null;
        }

        return result;
    }

    public override void Mapping(TypeMappingDescriptor<DancingGoatSearchModel> descriptor) =>
        descriptor
            .Properties(props => props
                .Keyword(x => x.Title)
                .Text(x => x.Content));
}
