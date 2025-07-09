using CMS.Core;

using DancingGoat.Models;

using XperienceCommunity.ElasticSearch.Admin.Models;
using XperienceCommunity.ElasticSearch.Indexing;
using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Tests.Data;

namespace XperienceCommunity.ElasticSearch.Tests.Tests;

internal class IndexedItemModelExtensionsTests
{

    [Test]
    public void IsIndexedByIndex()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);

        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        Assert.That(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WildCard()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();
        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new ElasticSearchIndexIncludedPath("/%") { ContentTypes = [new(ArticlePage.CONTENT_TYPE_NAME, nameof(ArticlePage))] };

        index.IncludedPaths = new List<ElasticSearchIndexIncludedPath>() { path };

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(index);

        Assert.That(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongWildCard()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();
        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new ElasticSearchIndexIncludedPath("/Index/%") { ContentTypes = [new(ArticlePage.CONTENT_TYPE_NAME, nameof(ArticlePage))] };

        index.IncludedPaths = new List<ElasticSearchIndexIncludedPath>() { path };

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongPath()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();
        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new ElasticSearchIndexIncludedPath("/Index") { ContentTypes = [new(ArticlePage.CONTENT_TYPE_NAME, nameof(ArticlePage))] };

        index.IncludedPaths = new List<ElasticSearchIndexIncludedPath>() { path };

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongContentType()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();

        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        model.ContentTypeName = "DancingGoat.HomePage";

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongIndex()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();

        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!MockDataProvider.WebModel(model).IsIndexedByIndex(log, "NewIndex", MockDataProvider.EventName));
    }

    [Test]
    public void WrongLanguage()
    {
        Service.InitializeContainer();
        var log = Substitute.For<IEventLogService>();

        var fixture = new Fixture();
        var item = fixture.Create<IndexEventWebPageItemModel>();

        var model = MockDataProvider.WebModel(item);
        model.LanguageName = "sk";

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }
}
