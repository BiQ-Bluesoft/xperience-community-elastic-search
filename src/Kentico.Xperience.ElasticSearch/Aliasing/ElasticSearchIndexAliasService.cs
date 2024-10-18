using CMS.Core;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    internal class ElasticSearchIndexAliasService(
         ElasticsearchClient indexClient,
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

            await CreateAliasAsync(oldAliasName, newAliasName, newAliasIndices, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAliasAsync(string aliasName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw new ArgumentNullException(nameof(aliasName));
            }

            var getAliasResponse = await indexClient.Indices
                .GetAliasAsync(alias => alias.Name(aliasName), cancellationToken);

            var aliasActions = new List<IndexUpdateAliasesAction>();
            if (getAliasResponse.IsValidResponse && getAliasResponse.Aliases.Any())
            {
                foreach (var index in getAliasResponse.Aliases)
                {
                    aliasActions.Add(IndexUpdateAliasesAction.Remove(new RemoveAction
                    {
                        Alias = aliasName,
                        Index = index.Key,
                    }));
                }

                var deleteAliasResponse = await indexClient.Indices.UpdateAliasesAsync(new UpdateAliasesRequest
                {
                    Actions = aliasActions,
                }, cancellationToken);
                if (!deleteAliasResponse.IsValidResponse)
                {
                    // Additional work - Discuss whether exception should be thrown or logging the error is enough.
                    eventLogService.LogError(
                        nameof(DeleteAliasAsync),
                        "ELASTIC_SEARCH",
                        $"Unable to delete alias with name: {aliasName}. Operation failed with error: {deleteAliasResponse.DebugInformation}");
                }
            }
        }

        private async Task CreateAliasAsync(string oldAliasName, string newAliasName, IEnumerable<string> newAliasIndices, CancellationToken cancellationToken)
        {
            var aliasActions = new List<IndexUpdateAliasesAction>();
            foreach (var index in newAliasIndices)
            {
                aliasActions.Add(IndexUpdateAliasesAction.Add(new AddAction
                {
                    Index = index,
                    Alias = newAliasName,
                }));
            }

            var createAliasResponse = await indexClient.Indices.UpdateAliasesAsync(
                new UpdateAliasesRequest
                {
                    Actions = aliasActions,
                },
                cancellationToken);

            if (!createAliasResponse.IsValidResponse)
            {
                // Additional work - Discuss whether exception should be thrown or logging the error is enough.
                eventLogService.LogError(
                    nameof(EditAliasAsync),
                    "ELASTIC_SEARCH",
                    $"Unable to edit alias with name: {oldAliasName}\n" +
                    $"- new alias name: {newAliasName}\n" +
                    $"- new indices: {newAliasIndices}." +
                    $"Operation failed with error: {createAliasResponse.DebugInformation}");
            }
        }
    }
}
