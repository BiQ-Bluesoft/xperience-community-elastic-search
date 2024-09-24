using Nest;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    internal class ElasticSearchIndexAliasService(ElasticClient indexClient) : IElasticSearchIndexAliasService
    {
        /// <inheritdoc />
        public async Task EditAlias(string oldAliasName, string newAliasName, IEnumerable<string> newAliasIndices,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(oldAliasName))
            {
                throw new ArgumentNullException(nameof(oldAliasName));
            }

            await DeleteAlias(oldAliasName, cancellationToken);

            var createAliasResponse = await indexClient.Indices.BulkAliasAsync(alias =>
            {
                foreach (var index in newAliasIndices)
                {
                    alias.Add(ad => ad.Index(index).Alias(newAliasName));
                }
                return alias;
            }, cancellationToken);
            if (!createAliasResponse.IsValid)
            {
                //TODO
            }
        }

        /// <inheritdoc />
        public async Task DeleteAlias(string aliasName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw new ArgumentNullException(nameof(aliasName));
            }

            var associatedIndices = await indexClient.GetIndicesPointingToAliasAsync(aliasName);
            if (associatedIndices != null && associatedIndices.Count != 0)
            {
                var deleteAliasResponse = await indexClient.Indices.BulkAliasAsync(alias =>
                {
                    foreach (var index in associatedIndices)
                    {
                        alias.Remove(r => r.Index(index).Alias(aliasName));
                    }

                    return alias;
                }, cancellationToken);
                if (!deleteAliasResponse.IsValid)
                {
                    //TODO
                }
            }
        }
    }
}
