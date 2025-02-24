using CMS.Core;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

using Kentico.Xperience.ElasticSearch.Helpers.Constants;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    internal class ElasticSearchIndexAliasService(
        ElasticsearchClient elasticSearchClient,
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

            var getAliasResponse = await elasticSearchClient.Indices
                .GetAliasAsync(alias => alias.Name(aliasName), cancellationToken);

            if (getAliasResponse.IsValidResponse && getAliasResponse.Aliases.Any())
            {
                var aliasActions = getAliasResponse.Aliases.Select(index =>
                    IndexUpdateAliasesAction.Remove(new RemoveAction
                    {
                        Alias = aliasName,
                        Index = index.Key
                    })).ToList();

                var deleteAliasResponse = await elasticSearchClient.Indices.UpdateAliasesAsync(new UpdateAliasesRequest
                {
                    Actions = aliasActions,
                }, cancellationToken);

                if (!deleteAliasResponse.IsValidResponse)
                {
                    eventLogService.LogError(
                        nameof(ElasticSearchIndexAliasService),
                        EventLogConstants.ElasticAliasDeleteEventCode,
                        $"Unable to delete alias with name: {aliasName}. Operation failed with error: {deleteAliasResponse.DebugInformation}");
                }
            }
        }

        /// <inheritdoc />
        public async Task AddAliasAsync(string aliasName, string indexName, CancellationToken cancellationToken)
        {
            ValidateNonEmptyAliasName(aliasName);
            ValidateNonEmptyIndexName(indexName);

            var getResponse = await elasticSearchClient.Indices.GetAsync(indexName, cancellationToken);
            if (!getResponse.IsValidResponse || getResponse.Indices.Count == 0)
            {
                eventLogService.LogError(
                    nameof(ElasticSearchIndexAliasService),
                    EventLogConstants.ElasticAliasCreateEventCode,
                    $"Index with name or alias {indexName} not found.");
                return;
            }

            var putAliasReponse = await elasticSearchClient.Indices
                .PutAliasAsync(
                    new PutAliasRequest(getResponse.Indices.First().Key.ToString(), aliasName), cancellationToken);
            if (!putAliasReponse.IsValidResponse)
            {
                eventLogService.LogError(
                    nameof(ElasticSearchIndexAliasService),
                    EventLogConstants.ElasticAliasCreateEventCode,
                    $"Unable to add alias with name: {aliasName} for index with name: {indexName}. Operation failed with error: {putAliasReponse.DebugInformation}");
            }
        }

        /// <inheritdoc />
        public async Task CreateAliasAsync(string aliasName, IEnumerable<string> aliasIndices, CancellationToken cancellationToken)
        {
            ValidateNonEmptyAliasName(aliasName);

            var addActions = new List<IndexUpdateAliasesAction>();
            foreach (var aliasIndexName in aliasIndices)
            {
                var getResponse = await elasticSearchClient.Indices.GetAsync(aliasIndexName, cancellationToken);
                if (!getResponse.IsValidResponse || getResponse.Indices.Count == 0)
                {
                    eventLogService.LogError(
                        nameof(ElasticSearchIndexAliasService),
                        EventLogConstants.ElasticAliasCreateEventCode,
                        $"Index with name or alias {aliasIndexName} not found.");
                    continue;
                }

                addActions.Add(IndexUpdateAliasesAction.Add(new AddAction
                {
                    Index = getResponse.Indices.First().Key.ToString(),
                    Alias = aliasName,
                }));
            }

            var createAliasResponse = await elasticSearchClient.Indices.UpdateAliasesAsync(
                new UpdateAliasesRequest
                {
                    Actions = addActions,
                },
                cancellationToken);

            if (!createAliasResponse.IsValidResponse)
            {
                eventLogService.LogError(
                    nameof(ElasticSearchIndexAliasService),
                    EventLogConstants.ElasticAliasCreateEventCode,
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
        private static void ValidateNonEmptyIndexName(string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
        }

        #endregion
    }
}
