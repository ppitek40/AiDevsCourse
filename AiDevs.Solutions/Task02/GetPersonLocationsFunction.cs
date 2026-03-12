using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

[FunctionDefinition("get_person_locations", "Get the list of coordinates where a person was seen")]
public class GetPersonLocationsFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(GetPersonLocationsParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not GetPersonLocationsParameters p)
            return "Invalid parameters type";

        var locations = await aiDevsApiService.GetLocationAsync(
            p.Name,
            p.Surname,
            cancellationToken);

        return JsonSerializer.Serialize(locations);
    }
}

public class GetPersonLocationsParameters
{
    [JsonPropertyName("name")]
    [Parameter("Person's first name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    [Parameter("Person's last name")]
    public string Surname { get; set; } = string.Empty;
}
