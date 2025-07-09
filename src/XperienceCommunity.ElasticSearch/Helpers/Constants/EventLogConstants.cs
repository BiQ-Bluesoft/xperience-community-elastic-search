namespace XperienceCommunity.ElasticSearch.Helpers.Constants;

public static class EventLogConstants
{
    public const string ElasticInfoEventCode = "ELASTIC_INFO";
    public const string ElasticRebuildEventCode = "ELASTIC_REBUILD";
    public const string ElasticDeleteEventCode = "ELASTIC_DELETE";
    public const string ElasticCreateEventCode = "ELASTIC_CREATE";

    public const string ElasticItemsDeleteEventCode = "ELASTIC_DELETE_ITEM";
    public const string ElasticItemsAddEventCode = "ELASTIC_ADD_ITEM";

    public const string ElasticAliasCreateEventCode = "ELASTIC_ALIAS_CREATE";
    public const string ElasticAliasDeleteEventCode = "ELASTIC_ALIAS_DELETE";
}
