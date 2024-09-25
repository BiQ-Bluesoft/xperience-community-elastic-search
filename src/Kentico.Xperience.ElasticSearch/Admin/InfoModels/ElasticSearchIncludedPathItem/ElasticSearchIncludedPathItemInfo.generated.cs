using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.ElasticSearch.Admin;

[assembly: RegisterObjectType(typeof(ElasticSearchIncludedPathItemInfo), ElasticSearchIncludedPathItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.ElasticSearch.Admin;

/// <summary>
/// Data container class for <see cref="ElasticSearchIncludedPathItemInfo"/>.
/// </summary>
[Serializable]
public partial class ElasticSearchIncludedPathItemInfo : AbstractInfo<ElasticSearchIncludedPathItemInfo, IInfoProvider<ElasticSearchIncludedPathItemInfo>>
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "kenticoelasticsearch.elasticsearchincludedpathitem";


    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IInfoProvider<ElasticSearchIncludedPathItemInfo>), OBJECT_TYPE, "KenticoElasticSearch.ElasticSearchIncludedPathItem", nameof(ElasticSearchIncludedPathItemId), null, nameof(ElasticSearchIncludedPathItemGuid), null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn = new List<ObjectDependency>()
        {
            new(nameof(ElasticSearchIncludedPathItemIndexItemId), ElasticSearchIndexItemInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
        },
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };


    /// <summary>
    /// ElasticSearch included path item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIncludedPathItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIncludedPathItemId)), 0);
        set => SetValue(nameof(ElasticSearchIncludedPathItemId), value);
    }

    /// <summary>
    /// ElasticSearch included path item guid.
    /// </summary>
    [DatabaseField]
    public virtual Guid ElasticSearchIncludedPathItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ElasticSearchIncludedPathItemGuid)), default);
        set => SetValue(nameof(ElasticSearchIncludedPathItemGuid), value);
    }


    /// <summary>
    /// Alias path.
    /// </summary>
    [DatabaseField]
    public virtual string ElasticSearchIncludedPathItemAliasPath
    {
        get => ValidationHelper.GetString(GetValue(nameof(ElasticSearchIncludedPathItemAliasPath)), String.Empty);
        set => SetValue(nameof(ElasticSearchIncludedPathItemAliasPath), value);
    }


    /// <summary>
    /// ElasticSearch index item id.
    /// </summary>
    [DatabaseField]
    public virtual int ElasticSearchIncludedPathItemIndexItemId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ElasticSearchIncludedPathItemIndexItemId)), 0);
        set => SetValue(nameof(ElasticSearchIncludedPathItemIndexItemId), value);
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
    protected ElasticSearchIncludedPathItemInfo(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }


    /// <summary>
    /// Creates an empty instance of the <see cref="ElasticSearchIncludedPathItemInfo"/> class.
    /// </summary>
    public ElasticSearchIncludedPathItemInfo()
        : base(TYPEINFO)
    {
    }


    /// <summary>
    /// Creates a new instances of the <see cref="ElasticSearchIncludedPathItemInfo"/> class from the given <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public ElasticSearchIncludedPathItemInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
