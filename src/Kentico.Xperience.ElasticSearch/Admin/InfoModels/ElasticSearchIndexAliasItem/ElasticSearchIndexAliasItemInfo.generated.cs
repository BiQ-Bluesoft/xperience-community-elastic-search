using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.ElasticSearch.Admin;

[assembly: RegisterObjectType(typeof(ElasticSearchIndexAliasItemInfo), ElasticSearchIndexAliasItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Data container class for <see cref="ElasticSearchIndexAliasItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchIndexAliasItemInfo : AbstractInfo<ElasticSearchIndexAliasItemInfo, IInfoProvider<ElasticSearchIndexAliasItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchindexaliasitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchIndexAliasItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchIndexAliasItem", nameof(ElasticSearchIndexAliasItemId), null, nameof(ElasticSearchIndexAliasItemGuid), nameof(ElasticSearchIndexAliasItemIndexAliasName), null, null, null, null)
    {
        TouchCacheDependencies = true,
        ContinuousIntegrationSettings =
        {
            Enabled = true,
        },
    };


    /// <summary>
    /// ElasticSearch indexalias item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexAliasItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexAliasItemId)), 0);
        set => SetValue(nameof(ElasticSearchIndexAliasItemId), value);
    }


    /// <summary>
    /// ElasticSearch indexalias item Guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchIndexAliasItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchIndexAliasItemGuid)), default);
        set => SetValue(nameof(ElasticSearchIndexAliasItemGuid), value);
    }


    /// <summary>
    /// IndexAlias name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexAliasItemIndexAliasName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexAliasItemIndexAliasName)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexAliasItemIndexAliasName), value);
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
    /// Creates an empty instance of the <see cref="ElasticSearchIndexAliasItemInfo"/> class.
    /// </summary>
    public ElasticSearchIndexAliasItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchIndexAliasItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchIndexAliasItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
