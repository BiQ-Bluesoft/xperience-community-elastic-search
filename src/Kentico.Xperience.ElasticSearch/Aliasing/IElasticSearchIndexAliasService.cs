using Kentico.Xperience.ElasticSearch.Helpers;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    public interface IElasticSearchIndexAliasService
    {
        /// <summary>
        /// Creates the ElasticSearch index alias in Elastic.
        /// </summary>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="aliasIndices">Indices included in the alias.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliasName"/> is null.</exception>
        Task<ElasticSearchResponse> CreateAliasAsync(string aliasName, IEnumerable<string> aliasIndices, CancellationToken cancellationToken);

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
        Task<ElasticSearchResponse> EditAliasAsync(string oldAliasName, string newAliasName, IEnumerable<string> newAliasIndices, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the ElasticSearch index alias by removing existing index alias data from Elastic.
        /// </summary>
        /// <param name="aliasName">The index to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliasName"/> is null.</exception>
        Task<ElasticSearchResponse> DeleteAliasAsync(string aliasName, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the ElasticSearch index alias.
        /// </summary>
        /// <param name="aliasName">Name of the alias.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        Task AddAliasAsync(string aliasName, string indexName, CancellationToken cancellationToken);
    }
}
