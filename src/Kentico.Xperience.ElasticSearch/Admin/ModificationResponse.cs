namespace Kentico.Xperience.ElasticSearch.Admin;

public class ModificationResponse(ModificationResult result, List<string>? errorMessage = null)
{
    public ModificationResult ModificationResult { get; set; } = result;
    public List<string>? ErrorMessages { get; set; } = errorMessage;
}
