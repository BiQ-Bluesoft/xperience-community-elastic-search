using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.ElasticSearch.Admin;

[assembly: RegisterObjectType(typeof(ElasticSearchIndexItemInfo), ElasticSearchIndexItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Data container class for <see cref="ElasticSearchIndexItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchIndexItemInfo : AbstractInfo<ElasticSearchIndexItemInfo, IInfoProvider<ElasticSearchIndexItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchindexitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchIndexItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchIndexItem", nameof(ElasticSearchIndexItemId), null, nameof(ElasticSearchIndexItemGuid), nameof(ElasticSearchIndexItemIndexName), null, null, null, null)
    {
        TouchCacheDependencies = true,
        ContinuousIntegrationSettings =
        {
            Enabled = true,
        },
    };


    /// <summary>
    /// ElasticSearch index item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchIndexItemId), value);
    }


    /// <summary>
    /// ElasticSearch index item Guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchIndexItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchIndexItemGuid)), default);
        set => SetValue(nameof(ElasticSearchIndexItemGuid), value);
    }


    /// <summary>
    /// Index name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexItemIndexName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexItemIndexName)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexItemIndexName), value);
    }


    /// <summary>
    /// Channel name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexItemChannelName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexItemChannelName)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexItemChannelName), value);
    }


    /// <summary>
    /// Strategy name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexItemStrategyName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexItemStrategyName)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexItemStrategyName), value);
    }


    /// <summary>
    /// Rebuild hook.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexItemRebuildHook
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexItemRebuildHook)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexItemRebuildHook), value, String.Empty);
    }

    /// <summary>
    /// Last rebuild time.
    /// </summary>
    [DatabaseField]
    public virtual DateTime ElasticSearchIndexItemLastRebuild
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ElasticSearchIndexItemLastRebuild)), DateTime.MinValue);
        set => SetValue(nameof(ElasticSearchIndexItemLastRebuild), value, DateTime.MinValue);
    }

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject()
    {
        Provider.Delete(this);
    }


    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject()
    {
        Provider.Set(this);
    }

    /// <summary>
    /// Creates an empty instance of the <see cref="ElasticSearchIndexItemInfo"/> class.
    /// </summary>
    public ElasticSearchIndexItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchIndexItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchIndexItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
