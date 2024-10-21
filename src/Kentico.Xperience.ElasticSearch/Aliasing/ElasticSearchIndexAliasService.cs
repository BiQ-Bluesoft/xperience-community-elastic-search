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
            ValidateNonEmptyAliasName(oldAliasName);

            await DeleteAliasAsync(oldAliasName, cancellationToken);

            await CreateAliasAsync(newAliasName, newAliasIndices, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAliasAsync(string aliasName, CancellationToken cancellationToken)
        {
            ValidateNonEmptyAliasName(aliasName);

            var getAliasResponse = await indexClient.Indices
                .GetAliasAsync(alias => alias.Name(aliasName), cancellationToken);

            if (getAliasResponse.IsValidResponse && getAliasResponse.Aliases.Any())
            {
                var aliasActions = getAliasResponse.Aliases.Select(index =>
                    IndexUpdateAliasesAction.Remove(new RemoveAction
                    {
                        Alias = aliasName,
                        Index = index.Key
                    })).ToList();

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

        /// <inheritdoc />
        public async Task AddAliasAsync(string aliasName, string indexName, CancellationToken cancellationToken)
        {
            ValidateNonEmptyAliasName(aliasName);

            ValidationNonEmptyIndexName(indexName);

            var putAliasReponse = await indexClient.Indices
                .PutAliasAsync(new PutAliasRequest(indexName, aliasName), cancellationToken);
            if (!putAliasReponse.IsValidResponse)
            {
                eventLogService.LogError(
                        nameof(AddAliasAsync),
                        "ELASTIC_SEARCH",
                        $"Unable to add alias with name: {aliasName} for index with name: {indexName}. Operation failed with error: {putAliasReponse.DebugInformation}");
            }
        }

        /// <inheritdoc />
        public async Task CreateAliasAsync(string aliasName, IEnumerable<string> aliasIndices, CancellationToken cancellationToken)
        {
            ValidateNonEmptyAliasName(aliasName);

            var aliasActions = aliasIndices.Select(index =>
                IndexUpdateAliasesAction.Add(new AddAction
                {
                    Index = index,
                    Alias = aliasName
                })).ToList();

            var createAliasResponse = await indexClient.Indices.UpdateAliasesAsync(
                new UpdateAliasesRequest
                {
                    Actions = aliasActions,
                },
                cancellationToken);

            if (!createAliasResponse.IsValidResponse)
            {
                eventLogService.LogError(
                    nameof(CreateAliasAsync),
                    "ELASTIC_SEARCH",
                    $"Unable to create alias with name: {aliasName}\n" +
                    $"- indices: {aliasIndices}." +
                    $"Operation failed with error: {createAliasResponse.DebugInformation}");
            }
        }

        #region Private methods

        private static void ValidateNonEmptyAliasName(string aliasName)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw new ArgumentNullException(nameof(aliasName));
            }
        }
        private static void ValidationNonEmptyIndexName(string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
        }

        #endregion
    }
}
