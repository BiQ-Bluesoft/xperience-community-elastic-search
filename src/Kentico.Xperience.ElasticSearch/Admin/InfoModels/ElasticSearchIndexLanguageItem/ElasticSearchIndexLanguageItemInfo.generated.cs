using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.ElasticSearch.Admin.InfoModels.ElasticSearchIndexItem;
using Kentico.Xperience.ElasticSearch.Admin.InfoModels.ElasticSearchIndexLanguageItem;

[assembly: RegisterObjectType(typeof(ElasticSearchIndexLanguageItemInfo), ElasticSearchIndexLanguageItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin.InfoModels.ElasticSearchIndexLanguageItem;

/// <summary>
/// Data container class for <see cref="ElasticSearchIndexLanguageItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchIndexLanguageItemInfo : AbstractInfo<ElasticSearchIndexLanguageItemInfo, IInfoProvider<ElasticSearchIndexLanguageItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchindexlanguageitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchIndexLanguageItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchIndexLanguageItem", nameof(ElasticSearchIndexLanguageItemID), null, nameof(ElasticSearchIndexLanguageItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(ElasticSearchIndexLanguageItemIndexItemId), ElasticSearchIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// Indexed language id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexLanguageItemID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexLanguageItemID)), 0);
        set => SetValue(nameof(ElasticSearchIndexLanguageItemID), value);
    }


    /// <summary>
    /// Indexed language id.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchIndexLanguageItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchIndexLanguageItemGuid)), default);
        set => SetValue(nameof(ElasticSearchIndexLanguageItemGuid), value);
    }


    /// <summary>
    /// Code.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIndexLanguageItemName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIndexLanguageItemName)), String.Empty);
        set => SetValue(nameof(ElasticSearchIndexLanguageItemName), value);
    }


    /// <summary>
    /// ElasticSearch index item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIndexLanguageItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIndexLanguageItemIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchIndexLanguageItemIndexItemId), value);
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
    /// Creates an empty instance of the <see cref="ElasticSearchIndexLanguageItemInfo"/> class.
    /// </summary>
    public ElasticSearchIndexLanguageItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchIndexLanguageItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchIndexLanguageItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
