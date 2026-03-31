using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task14;

[FunctionDefinition("search_items", "Searches for items based on a given word and returns a list of matching items with their codes and names, always search for more general terms first, then specific terms.")]
public class SearchItemsFunction(IItemCityDataService itemCityDataService) : IFunctionHandler
{
    public Type ParametersType => typeof(SearchItemsFunctionParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SearchItemsFunctionParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Query))
            return JsonSerializer.Serialize(new { error = "Query cannot be empty" });

        try
        {
            var result = itemCityDataService.SearchItems(p.Query);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Search items failed: {ex.Message}" });
        }
    }
}

public class SearchItemsFunctionParameters
{
    [JsonPropertyName("query")]
    [Parameter("Search for items containing this word. Best works with single words. Searching with multiple words can return wrong results.")]
    public string Query { get; set; } = string.Empty;
}
