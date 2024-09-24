using Kentico.Xperience.ElasticSearch.Admin.Models;

namespace Kentico.Xperience.ElasticSearch.Aliasing
{
    public sealed class ElasticSearchIndexAlias
    {
        /// <summary>
        /// An arbitrary ID used to identify the ElasticSearch index in the admin UI.
        /// </summary>
        public int Identifier { get; set; }

        /// <summary>
        /// The Name of the ElasticSearch index alias.
        /// </summary>
        public string AliasName { get; }

        /// <summary>
        /// The code name of the ElasticSearch index which is aliased.
        /// </summary>
        public IEnumerable<string> IndexNames { get; }

        internal ElasticSearchIndexAlias(ElasticSearchAliasConfigurationModel aliasConfiguration)
        {
            Identifier = aliasConfiguration.Id;
            IndexNames = aliasConfiguration.IndexNames;
            AliasName = aliasConfiguration.AliasName;
        }
    }
}
