using DancingGoat.Models;
using DancingGoat.Search.Models;
using DancingGoat.Search.Services;

using Elastic.Clients.Elasticsearch.Mapping;

using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

using Microsoft.IdentityModel.Tokens;

namespace DancingGoat.Search.Strategies;

public class DancingGoatSimpleSearchStrategy(StrategyHelper strategyHelper) : BaseElasticSearchIndexingStrategy<DancingGoatSimpleSearchModel>
{
    public override async Task<IElasticSearchModel?> MapToElasticSearchModelOrNull(IIndexEventItemModel item)
    {
        var result = new DancingGoatSimpleSearchModel();

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }

        if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
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

            result.Title = page.ArticleTitle ?? string.Empty;
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
        }
        else
        {
            return null;
        }

        return result;
    }

    public override void Mapping(TypeMappingDescriptor<DancingGoatSimpleSearchModel> descriptor) =>
        descriptor
            .Properties(props => props
                .Text(x => x.Title) // searching for the part of the title will work too
                .Keyword(x => x.Url)); // only searching for the whole url will work
}
