using Kentico.Xperience.Admin.Base.FormAnnotations;

using XperienceCommunity.ElasticSearch.Indexing.Strategies;

namespace XperienceCommunity.ElasticSearch.Admin.Providers;

internal class IndexingStrategyOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult(StrategyStorage.Strategies.Keys.Select(x => new DropDownOptionItem
        {
            Value = x,
            Text = x
        }));
}
