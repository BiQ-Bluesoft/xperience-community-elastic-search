using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Kentico.Xperience.AzureSearch.Admin;

internal class ElasticSearchModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> resourceProvider;

    public ElasticSearchModuleInstaller(IInfoProvider<ResourceInfo> resourceProvider) => this.resourceProvider = resourceProvider;

    public void Install()
    {
        var resource = resourceProvider.Get("CMS.Integration.ElasticSearch")
            ?? new ResourceInfo();

        InitializeResource(resource);
        InstallElasticSearchItemInfo(resource);
        InstallElasticSearchIndexAliasItemInfo(resource);
        InstallElasticSearchIndexAliasIndexItemInfo(resource);
        InstallElasticSearchLanguageInfo(resource);
        InstallElasticSearchIndexPathItemInfo(resource);
        InstallElasticSearchContentTypeItemInfo(resource);
    }

    public ResourceInfo InitializeResource(ResourceInfo resource)
    {
        resource.ResourceDisplayName = "Kentico Integration - ElasticSearch";

        // Prefix ResourceName with "CMS" to prevent C# class generation
        // Classes are already available through the library itself
        resource.ResourceName = "CMS.Integration.ElasticSearch";
        resource.ResourceDescription = "Kentico ElasticSearch custom data";
        resource.ResourceIsInDevelopment = false;
        if (resource.HasChanged)
        {
            resourceProvider.Set(resource);
        }

        return resource;
    }

    public void InstallElasticSearchItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchIndexItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchIndexItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchIndexItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchIndexItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Index Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemIndexName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemChannelName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemStrategyName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexItemInfo.ElasticSearchIndexItemRebuildHook),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    public void InstallElasticSearchIndexAliasItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchIndexAliasItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchIndexAliasItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchIndexAliasItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchIndexAliasItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Index Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexAliasItemInfo.ElasticSearchIndexAliasItemIndexAliasName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    public void InstallElasticSearchIndexAliasIndexItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchIndexAliasIndexItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchIndexAliasIndexItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchIndexAliasIndexItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchIndexAliasIndexItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Index Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexAliasId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "integer",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexAliasIndexItemInfo.ElasticSearchIndexAliasIndexItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "integer",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    public void InstallElasticSearchIndexPathItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchIncludedPathItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchIncludedPathItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchIncludedPathItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchIncludedPathItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Path Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemAliasPath),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIncludedPathItemInfo.ElasticSearchIncludedPathItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = ElasticSearchIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    public void InstallElasticSearchLanguageInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchIndexLanguageItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchIndexLanguageItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchIndexLanguageItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchIndexLanguageItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Indexed Language Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemID));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchIndexLanguageItemInfo.ElasticSearchIndexLanguageItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = ElasticSearchIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    public void InstallElasticSearchContentTypeItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ElasticSearchContentTypeItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(ElasticSearchContentTypeItemInfo.OBJECT_TYPE);

        info.ClassName = ElasticSearchContentTypeItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ElasticSearchContentTypeItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "ElasticSearch Type Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemContentTypeName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
            IsUnique = false
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIncludedPathItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = ElasticSearchIncludedPathItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemGuid),
            Enabled = true,
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(ElasticSearchContentTypeItemInfo.ElasticSearchContentTypeItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = ElasticSearchIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    /// <summary>
    /// Ensure that the form is upserted with any existing form
    /// </summary>
    /// <param name="info"></param>
    /// <param name="form"></param>
    private static void SetFormDefinition(DataClassInfo info, FormInfo form)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(form, new());
            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = form.GetXmlDefinition();
        }
    }
}
