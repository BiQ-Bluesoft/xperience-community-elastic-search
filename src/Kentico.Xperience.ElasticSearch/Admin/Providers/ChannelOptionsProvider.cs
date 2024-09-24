using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.ElasticSearch.Admin;

internal class ChannelOptionsProvider(IInfoProvider<ChannelInfo> channelInfoProvider) : IDropDownOptionsProvider
{
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        (await channelInfoProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), nameof(ChannelType.Website))
            .GetEnumerableTypedResultAsync())
            .Select(x => new DropDownOptionItem()
            {
                Value = x.ChannelName,
                Text = x.ChannelDisplayName
            });
}
