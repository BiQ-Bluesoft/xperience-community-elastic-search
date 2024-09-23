namespace Kentico.Xperience.AzureSearch.Aliasing;

/// <summary>
/// Manages aliases.
/// </summary>
public interface IElasticSearchIndexAliasService
{
    /// <summary>
    /// Edits the ElasticSearch index alias.
    /// </summary>
    /// <param name="indexName">Name of the index.</param>
    /// <param name="oldAliasName">The alias to edit.</param>
    /// <param name="newAliasName">New name of the alias.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="oldAliasName"/> is null.</exception>
    Task EditAlias(string indexName, string oldAliasName, string newAliasName, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the ElasticSearch index alias by removing existing index alias data.
    /// </summary>
    /// <param name="aliasName">The index to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="OperationCanceledException" />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliasName"/> is null.</exception>
    Task DeleteAlias(string aliasName, CancellationToken cancellationToken);
}
