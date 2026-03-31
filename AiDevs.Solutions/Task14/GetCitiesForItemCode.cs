using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;

namespace AiDevs.Solutions.Task14;

[FunctionDefinition("get_cities_for_item_code", "Returns the list of cities where a given item is located, based on its code")]
public class GetCitiesForItemCode(IItemCityDataService itemCityDataService) : IFunctionHandler
{
    public Type ParametersType => typeof(Task14ToolOneParameters);

    public Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not Task14ToolOneParameters p)
            return Task.FromResult(JsonSerializer.Serialize(new { error = "Invalid parameters type" }));

        if (string.IsNullOrWhiteSpace(p.itemCode))
            return Task.FromResult(JsonSerializer.Serialize(new { error = "Input cannot be empty" }));

        try
        {
            var cities = itemCityDataService.GetCitiesForItem(p.itemCode);
            return Task.FromResult(JsonSerializer.Serialize(new { cities }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new { error = $"Failed to get cities: {ex.Message}" }));
        }
    }
}

public class Task14ToolOneParameters
{
    [JsonPropertyName("item_code")]
    [Parameter("The exact code of the item to look up (e.g. '8PZM53')")]
    public string itemCode { get; set; } = string.Empty;
}
