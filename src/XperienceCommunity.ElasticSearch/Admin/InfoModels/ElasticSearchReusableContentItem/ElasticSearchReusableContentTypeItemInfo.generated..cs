using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchIndexItem;
using XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchReusableContentItem;

[assembly: RegisterObjectType(typeof(ElasticSearchReusableContentTypeItemInfo), ElasticSearchReusableContentTypeItemInfo.OBJECT_TYPE)]

namespace XperienceCommunity.ElasticSearch.Admin.InfoModels.ElasticSearchReusableContentItem;

/// <summary>
/// Data container class for <see cref="ElasticSearchReusableContentTypeItemInfo"/>.
/// </summary>
[Serializable]
public class ElasticSearchReusableContentTypeItemInfo : AbstractInfo<ElasticSearchReusableContentTypeItemInfo, IInfoProvider<ElasticSearchReusableContentTypeItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "xperiencecommunity.elasticsearchreusablecontenttypeitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchReusableContentTypeItemInfo>), OBJECT_TYPE, "XperienceCommunity.ElasticSearchReusableContentTypeItem", nameof(ElasticSearchReusableContentTypeItemId), null, nameof(ElasticSearchReusableContentTypeItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(ElasticSearchReusableContentTypeItemIndexItemId), ElasticSearchIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// ElasticSearch reusable content type item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchReusableContentTypeItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchReusableContentTypeItemId)), 0);
        set => SetValue(nameof(ElasticSearchReusableContentTypeItemId), value);
    }


    /// <summary>
    /// ElasticSearch reusable content type item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchReusableContentTypeItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchReusableContentTypeItemGuid)), default);
        set => SetValue(nameof(ElasticSearchReusableContentTypeItemGuid), value);
    }


    /// <summary>
    /// Reusable content type name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchReusableContentTypeItemContentTypeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchReusableContentTypeItemContentTypeName)), string.Empty);
        set => SetValue(nameof(ElasticSearchReusableContentTypeItemContentTypeName), value);
    }


    /// <summary>
    /// ElasticSearch index item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchReusableContentTypeItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchReusableContentTypeItemIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchReusableContentTypeItemIndexItemId), value);
    }


    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() => Provider.Delete(this);


    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() => Provider.Set(this);

    /// <summary>
    /// Creates an empty instance of the <see cref="ElasticSearchReusableContentTypeItemInfo"/> class.
    /// </summary>
    public ElasticSearchReusableContentTypeItemInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchReusableContentTypeItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchReusableContentTypeItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
