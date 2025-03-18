using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Indexing;
using Kentico.Xperience.ElasticSearch.Tests.Data;

namespace Kentico.Xperience.ElasticSearch.Tests.Tests;
internal class IndexStoreTests
{

    [Test]
    public void AddAndGetIndex()
    {
        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());

        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.GetIndex("TestIndex", 1));

        Assert.Multiple(() =>
        {
            Assert.That(ElasticSearchIndexStore.Instance.GetIndex("TestIndex") is not null);
            Assert.That(ElasticSearchIndexStore.Instance.GetIndex(MockDataProvider.DefaultIndex) is not null);
        });
    }

    [Test]
    public void AddIndex_AlreadyExists()
    {
        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>());
        ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);

        var hasThrown = false;

        try
        {
            ElasticSearchIndexStore.Instance.AddIndex(MockDataProvider.Index);
        }
        catch
        {
            hasThrown = true;
        }

        Assert.That(hasThrown);
    }

    [Test]
    public void SetIndices()
    {
        var defaultIndex = new ElasticSearchConfigurationModel { IndexName = "DefaultIndex", Id = 0 };
        var simpleIndex = new ElasticSearchConfigurationModel { IndexName = "SimpleIndex", Id = 1 };

        ElasticSearchIndexStore.Instance.SetIndices(new List<ElasticSearchConfigurationModel>() { defaultIndex, simpleIndex });

        Assert.Multiple(() =>
        {
            Assert.That(ElasticSearchIndexStore.Instance.GetIndex(defaultIndex.IndexName) is not null);
            Assert.That(ElasticSearchIndexStore.Instance.GetIndex(simpleIndex.IndexName) is not null);
        });
    }
}
