using CMS;
using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;

using XperienceCommunity.ElasticSearch;
using XperienceCommunity.ElasticSearch.Indexing;
using XperienceCommunity.ElasticSearch.Indexing.Models;
using XperienceCommunity.ElasticSearch.Indexing.SearchTasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: RegisterModule(typeof(ElasticSearchSearchModule))]

namespace XperienceCommunity.ElasticSearch;

/// <summary>
/// Initializes page event handlers, and ensures the thread queue workers for processing ElasticSearch tasks.
/// </summary>
internal class ElasticSearchSearchModule : Module
{
    private IElasticSearchTaskLogger elasticSearchTaskLogger = null!;

    /// <inheritdoc/>
    public ElasticSearchSearchModule() : base(nameof(ElasticSearchSearchModule))
    {
    }

    /// <inheritdoc/>
    protected override void OnInit(ModuleInitParameters parameters)
    {
        try
        {
            base.OnInit(parameters);

            var services = parameters.Services;
            var options = services.GetRequiredService<IOptions<ElasticSearchOptions>>();

            if (!options.Value?.SearchServiceEnabled ?? false)
            {
                return;
            }

            elasticSearchTaskLogger = services.GetRequiredService<IElasticSearchTaskLogger>();

            WebPageEvents.Publish.Execute += HandleEvent;
            WebPageEvents.Unpublish.Execute += HandleEvent;
            WebPageEvents.Delete.Execute += HandleEvent;

            ContentItemEvents.Publish.Execute += HandleContentItemEvent;
            ContentItemEvents.Unpublish.Execute += HandleContentItemEvent;
            ContentItemEvents.Delete.Execute += HandleContentItemEvent;

            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => ElasticSearchQueueWorker.Current.EnsureRunningThread();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }


    /// <summary>
    /// Called when a page is published. Logs an ElasticSearch task to be processed later.
    /// </summary>
    private void HandleEvent(object? sender, CMSEventArgs e)
    {
        if (e is not WebPageEventArgsBase publishedEvent)
        {
            return;
        }

        var indexedItemModel = new IndexEventWebPageItemModel(
            publishedEvent.ID,
            publishedEvent.Guid,
            publishedEvent.ContentLanguageName,
            publishedEvent.ContentTypeName,
            publishedEvent.Name,
            publishedEvent.IsSecured,
            publishedEvent.ContentTypeID,
            publishedEvent.ContentLanguageID,
            publishedEvent.WebsiteChannelName,
            publishedEvent.TreePath,
            publishedEvent.Order,
            publishedEvent.ParentID);

        elasticSearchTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }

    private void HandleContentItemEvent(object? sender, CMSEventArgs e)
    {
        if (e is not ContentItemEventArgsBase publishedEvent)
        {
            return;
        }

        var indexedContentItemModel = new IndexEventReusableItemModel(
            publishedEvent.ID,
            publishedEvent.Guid,
            publishedEvent.ContentLanguageName,
            publishedEvent.ContentTypeName,
            publishedEvent.Name,
            publishedEvent.IsSecured,
            publishedEvent.ContentTypeID,
            publishedEvent.ContentLanguageID
        );

        elasticSearchTaskLogger?.HandleReusableItemEvent(indexedContentItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }
}
