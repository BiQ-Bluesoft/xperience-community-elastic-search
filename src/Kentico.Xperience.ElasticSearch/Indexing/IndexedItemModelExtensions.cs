using CMS.ContentEngine.Internal;
using CMS.Core;

using Kentico.Xperience.ElasticSearch.Indexing.Models;

namespace Kentico.Xperience.ElasticSearch.Indexing;

/// <summary>
/// ElasticSearch extension methods for the <see cref="IIndexEventItemModel"/> class.
/// </summary>
internal static class IndexedItemModelExtensions
{
    /// <summary>
    /// Returns true if the node is included in the ElasticSearch index based on the index's defined paths
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="item">The node to check for indexing.</param>
    /// <param name="log"></param>
    /// <param name="indexName">The ElasticSearch index code name.</param>
    /// <param name="eventName"></param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsIndexedByIndex(this IndexEventWebPageItemModel item, IEventLogService log, string indexName, string eventName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        ArgumentNullException.ThrowIfNull(item);

        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName);

        if (elasticSearchIndex is null)
        {
            log.LogError(nameof(IndexedItemModelExtensions), nameof(IsIndexedByIndex), $"Error loading registered ElasticSearch index '{indexName}' for event [{eventName}].");

            return false;
        }

        if (!string.Equals(item.WebsiteChannelName, elasticSearchIndex.WebSiteChannelName))
        {
            return false;
        }

        if (!elasticSearchIndex.LanguageNames.Exists(x => x == item.LanguageName))
        {
            return false;
        }

        return elasticSearchIndex.IncludedPaths.Any(path =>
        {
            var matchesContentType = path.ContentTypes.Exists(x => string.Equals(x.ContentTypeName, item.ContentTypeName));

            if (!matchesContentType)
            {
                return false;
            }

            // Supports wildcard matching
            if (path.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase))
            {
                var pathToMatch = path.AliasPath[..^2];
                var pathsOnPath = TreePathUtils.GetTreePathsOnPath(item.WebPageItemTreePath, true, false).ToHashSet();

                return pathsOnPath.Any(p => p.StartsWith(pathToMatch, StringComparison.OrdinalIgnoreCase));
            }

            return item.WebPageItemTreePath.Equals(path.AliasPath, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Returns true if the node is included in the ElasticSearch index's allowed
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="item">The node to check for indexing.</param>
    /// <param name="log"></param>
    /// <param name="indexName">The ElasticSearch index code name.</param>
    /// <param name="eventName"></param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsIndexedByIndex(this IndexEventReusableItemModel item, IEventLogService log, string indexName, string eventName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        ArgumentNullException.ThrowIfNull(item);

        var elasticSearchIndex = ElasticSearchIndexStore.Instance.GetIndex(indexName);

        if (elasticSearchIndex is null)
        {
            log.LogError(nameof(IndexedItemModelExtensions), nameof(IsIndexedByIndex), $"Error loading registered ElasticSearch index '{indexName}' for event [{eventName}].");

            return false;
        }

        return elasticSearchIndex.LanguageNames.Exists(x => x == item.LanguageName);
    }
}
