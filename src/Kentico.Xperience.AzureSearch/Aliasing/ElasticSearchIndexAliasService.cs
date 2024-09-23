using Nest;

namespace Kentico.Xperience.AzureSearch.Aliasing;

/// <summary>
/// Default implementation of <see cref="IElasticSearchIndexAliasService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ElasticSearchIndexAliasService"/> class.
/// </remarks>
internal class ElasticSearchIndexAliasService(ElasticClient indexClient) : IElasticSearchIndexAliasService
{
    /// <inheritdoc />
    public async Task EditAlias(string indexName, string oldAliasName, string newAliasName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(oldAliasName))
        {
            throw new ArgumentNullException(nameof(oldAliasName));
        }

        await DeleteAlias(oldAliasName, cancellationToken);
        await indexClient.Indices.PutAliasAsync(indexName, newAliasName, ct: cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAlias(string aliasName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(aliasName))
        {
            throw new ArgumentNullException(nameof(aliasName));
        }

        await indexClient.Indices.DeleteAliasAsync("*", aliasName, ct: cancellationToken);
    }
}
