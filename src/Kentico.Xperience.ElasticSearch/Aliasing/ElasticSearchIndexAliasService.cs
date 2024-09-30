using CMS.Core;

using Nest;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    internal class ElasticSearchIndexAliasService(
        ElasticClient indexClient,
        IEventLogService eventLogService) : IElasticSearchIndexAliasService
    {
        /// <inheritdoc />
        public async Task EditAliasAsync(string oldAliasName, string newAliasName, IEnumerable<string> newAliasIndices,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(oldAliasName))
            {
                throw new ArgumentNullException(nameof(oldAliasName));
            }

            await DeleteAliasAsync(oldAliasName, cancellationToken);

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
                // TODO Discuss whether exception should be thrown or logging the error is enough.
                eventLogService.LogError(
                    nameof(EditAliasAsync),
                    "ELASTIC_SEARCH",
                    $"Unable to edit alias with name: {oldAliasName}\n" +
                    $"- new alias name: {newAliasName}\n" +
                    $"- new indices: {newAliasIndices}." +
                    $"Operation failed with error: {createAliasResponse.OriginalException}");
            }
        }

        /// <inheritdoc />
        public async Task DeleteAliasAsync(string aliasName, CancellationToken cancellationToken)
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
                    // TODO Discuss whether exception should be thrown or logging the error is enough.
                    eventLogService.LogError(
                        nameof(DeleteAliasAsync),
                        "ELASTIC_SEARCH",
                        $"Unable to delete alias with name: {aliasName}. Operation failed with error: {deleteAliasResponse.OriginalException}");
                }
            }
        }
    }
}
