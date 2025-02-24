using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.ElasticSearch.Admin;

[assembly: RegisterObjectType(typeof(ElasticSearchIndexAliasIndexItemInfo), ElasticSearchIndexAliasIndexItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Data container class for <see cref="ElasticSearchIndexAliasIndexItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchIndexAliasIndexItemInfo : AbstractInfo<ElasticSearchIndexAliasIndexItemInfo, IInfoProvider<ElasticSearchIndexAliasIndexItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchindexaliasindexitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchIndexAliasIndexItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchIndexAliasIndexItem", nameof(ElasticSearchIndexAliasIndexItemId), null, nameof(ElasticSearchIndexAliasIndexItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(ElasticSearchIndexAliasIndexItemIndexAliasId), ElasticSearchIndexAliasItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
            new(nameof(ElasticSearchIndexAliasIndexItemIndexItemId), ElasticSearchIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true,
        }
    };


    /// <summary>
    /// ElasticSearch indexalias item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexAliasIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexAliasIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchIndexAliasIndexItemId), value);
    }


    /// <summary>
    /// ElasticSearch indexalias item Guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchIndexAliasIndexItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchIndexAliasIndexItemGuid)), default);
        set => SetValue(nameof(ElasticSearchIndexAliasIndexItemGuid), value);
    }


    /// <summary>
    /// IndexAlias name.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexAliasIndexItemIndexAliasId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexAliasIndexItemIndexAliasId)), 0);
        set => SetValue(nameof(ElasticSearchIndexAliasIndexItemIndexAliasId), value);
    }


    /// <summary>
    /// Strategy name.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexAliasIndexItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexAliasIndexItemIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchIndexAliasIndexItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="ElasticSearchIndexAliasIndexItemInfo"/> class.
    /// </summary>
    public ElasticSearchIndexAliasIndexItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchIndexAliasIndexItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchIndexAliasIndexItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
