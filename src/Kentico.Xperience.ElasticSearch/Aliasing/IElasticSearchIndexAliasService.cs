namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    public interface IElasticSearchIndexAliasService
    {
        /// <summary>
        /// Edits the ElasticSearch index alias in Elastic.
        /// </summary>
        /// <param name="oldAliasName">The alias to edit.</param>
        /// <param name="newAliasName">The name of the new alias.</param>
        /// <param name="newAliasIndices">Indices included in the new alias.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="oldAliasName"/> is null.</exception>
        Task EditAliasAsync(string oldAliasName, string newAliasName, IEnumerable<string> newAliasIndices, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the ElasticSearch index alias by removing existing index alias data from Elastic.
        /// </summary>
        /// <param name="aliasName">The index to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliasName"/> is null.</exception>
        Task DeleteAliasAsync(string aliasName, CancellationToken cancellationToken);
    }
}
