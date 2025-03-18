using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

namespace Kentico.Xperience.ElasticSearch.Admin.Providers;

internal class IndexingStrategyOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult(StrategyStorage.Strategies.Keys.Select(x => new DropDownOptionItem
        {
            Value = x,
            Text = x
        }));
}
