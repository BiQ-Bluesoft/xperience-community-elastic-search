using Kentico.Xperience.ElasticSearch.Admin.Models;
using Kentico.Xperience.ElasticSearch.Admin.Services;
using Kentico.Xperience.ElasticSearch.Indexing.Models;
using Kentico.Xperience.ElasticSearch.Indexing.Strategies;

namespace Kentico.Xperience.ElasticSearch.Indexing;

/// <summary>
/// Represents a global singleton store of ElasticSearch indexes
/// </summary>
public sealed class ElasticSearchIndexStore
{
    private static readonly Lazy<ElasticSearchIndexStore> mInstance = new();
    private readonly List<ElasticSearchIndex> registeredIndexes = [];

    /// <summary>
    /// Gets singleton instance of the <see cref="ElasticSearchIndexStore"/>
    /// </summary>
    public static ElasticSearchIndexStore Instance => mInstance.Value;

    /// <summary>
    /// Gets all registered indexes.
    /// </summary>
    public IEnumerable<ElasticSearchIndex> GetAllIndices() => registeredIndexes;

    /// <summary>
    /// Gets a registered <see cref="ElasticSearchIndex"/> with the specified <paramref name="indexName"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public ElasticSearchIndex? GetIndex(string indexName) => string.IsNullOrEmpty(indexName) ? null
        : registeredIndexes.SingleOrDefault(i => i.IndexName.Equals(indexName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a registered <see cref="ElasticSearchIndex"/> with the specified <paramref name="identifier"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="identifier">The identifier of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public ElasticSearchIndex? GetIndex(int identifier) => registeredIndexes.Find(i => i.Identifier == identifier);

    /// <summary>
    /// Gets a registered <see cref="ElasticSearchIndex"/> with the specified <paramref name="indexName"/>. If no index is found, a <see cref="InvalidOperationException" /> is thrown.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public ElasticSearchIndex GetRequiredIndex(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentException("Value must not be null or empty");
        }

        return registeredIndexes.SingleOrDefault(i => i.IndexName.Equals(indexName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The index '{indexName}' is not registered.");
    }

    /// <summary>
    /// Adds an index to the store.
    /// </summary>
    /// <param name="index">The index to add.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    internal void AddIndex(ElasticSearchIndex index)
    {
        if (index == null)
        {
            throw new ArgumentNullException(nameof(index));
        }

        if (registeredIndexes.Exists(i => i.IndexName.Equals(index.IndexName, StringComparison.OrdinalIgnoreCase) || index.Identifier == i.Identifier))
        {
            throw new InvalidOperationException($"Attempted to register ElasticSearch index with identifier [{index.Identifier}] and name [{index.IndexName}] but it is already registered.");
        }

        registeredIndexes.Add(index);
    }

    /// <summary>
    /// Resets all indicies
    /// </summary>
    /// <param name="models"></param>
    internal void SetIndicies(IEnumerable<ElasticSearchConfigurationModel> models)
    {
        registeredIndexes.Clear();

        foreach (var index in models)
        {
            Instance.AddIndex(new ElasticSearchIndex(index, StrategyStorage.Strategies));
        }
    }

    /// <summary>
    /// Sets the current indicies to those provided by <paramref name="configurationService"/>
    /// </summary>
    /// <param name="configurationService"></param>
    internal static void SetIndicies(IElasticSearchConfigurationStorageService configurationService)
    {
        var indices = configurationService.GetAllIndexData();

        Instance.SetIndicies(indices);
    }
}
