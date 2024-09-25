using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.ElasticSearch.Admin;

[assembly: RegisterObjectType(typeof(ElasticSearchContentTypeItemInfo), ElasticSearchContentTypeItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Data container class for <see cref="ElasticSearchContentTypeItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchContentTypeItemInfo : AbstractInfo<ElasticSearchContentTypeItemInfo, IInfoProvider<ElasticSearchContentTypeItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchcontenttypeitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchContentTypeItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchContentTypeItem", nameof(ElasticSearchContentTypeItemId), null, nameof(ElasticSearchContentTypeItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(ElasticSearchContentTypeItemIncludedPathItemId), ElasticSearchIncludedPathItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
            new(nameof(ElasticSearchContentTypeItemIndexItemId), ElasticSearchIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// ElasticSearch content type item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchContentTypeItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchContentTypeItemId)), 0);
        set => SetValue(nameof(ElasticSearchContentTypeItemId), value);
    }


    /// <summary>
    /// ElasticSearch content type item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchContentTypeItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchContentTypeItemGuid)), default);
        set => SetValue(nameof(ElasticSearchContentTypeItemGuid), value);
    }


    /// <summary>
    /// Content type name.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchContentTypeItemContentTypeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchContentTypeItemContentTypeName)), String.Empty);
        set => SetValue(nameof(ElasticSearchContentTypeItemContentTypeName), value);
    }


    /// <summary>
    /// ElasticSearch included path item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchContentTypeItemIncludedPathItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchContentTypeItemIncludedPathItemId)), 0);
        set => SetValue(nameof(ElasticSearchContentTypeItemIncludedPathItemId), value);
    }


    /// <summary>
    /// ElasticSearch index item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchContentTypeItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchContentTypeItemIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchContentTypeItemIndexItemId), value);
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
    /// Constructor for de-serialization.
    /// </summary>
    /// <param name="info">Serialization info.</param>
    /// <param name="context">Streaming context.</param>
    protected ElasticSearchContentTypeItemInfo(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }


    /// <summary>
    /// Creates an empty instance of the <see cref="ElasticSearchContentTypeItemInfo"/> class.
    /// </summary>
    public ElasticSearchContentTypeItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchContentTypeItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchContentTypeItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
