namespace Kentico.Xperience.ElasticSearch.Helpers;

public class ElasticSearchResponse
{
    public ElasticSearchResult Result { get; set; }

    public string ErrorMessage { get; set; }

    public bool IsSuccess => Result == ElasticSearchResult.Success;

    private ElasticSearchResponse(ElasticSearchResult result, string errorMessage)
    {
        Result = result;
        ErrorMessage = errorMessage;
    }

    public static ElasticSearchResponse Success() => new(ElasticSearchResult.Success, string.Empty);
    public static ElasticSearchResponse Failure(string message = "") => new(ElasticSearchResult.Failure, message);
}
