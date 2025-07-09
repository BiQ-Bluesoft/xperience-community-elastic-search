using DancingGoat.Models;

using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Indexing.Models;

namespace XperienceCommunity.ElasticSearch.Tests.Data;

internal static class MockDataProvider
{
    public static IndexEventWebPageItemModel WebModel(IndexEventWebPageItemModel item)
    {
        item.LanguageName = CzechLanguageName;
        item.ContentTypeName = ArticlePage.CONTENT_TYPE_NAME;
        item.Name = "Name";
        item.ContentTypeID = 1;
        item.ContentLanguageID = 1;
        item.WebsiteChannelName = DefaultChannel;
        item.WebPageItemTreePath = "/%";

        return item;
    }

    public static ElasticSearchIndexIncludedPath Path => new("/%")
    {
        ContentTypes = [new ElasticSearchIndexContentType(ArticlePage.CONTENT_TYPE_NAME, nameof(ArticlePage))]
    };


    public static ElasticSearchIndex Index => new(
        new ElasticSearchConfigurationModel()
        {
            IndexName = DefaultIndex,
            ChannelName = DefaultChannel,
            LanguageNames = new List<string>() { EnglishLanguageName, CzechLanguageName },
            Paths = new List<ElasticSearchIndexIncludedPath>() { Path },
            StrategyName = "strategy"
        },
        []
    );

    public static readonly string DefaultIndex = "SimpleIndex";
    public static readonly string DefaultChannel = "DefaultChannel";
    public static readonly string EnglishLanguageName = "en";
    public static readonly string CzechLanguageName = "cz";
    public static readonly int IndexId = 1;
    public static readonly string EventName = "publish";

    public static ElasticSearchIndex GetIndex(string indexName, int id) => new(
        new ElasticSearchConfigurationModel()
        {
            Id = id,
            IndexName = indexName,
            ChannelName = DefaultChannel,
            LanguageNames = new List<string>() { EnglishLanguageName, CzechLanguageName },
            Paths = new List<ElasticSearchIndexIncludedPath>() { Path }
        },
        []
    );
}
