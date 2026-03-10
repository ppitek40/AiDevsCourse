using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

[FunctionDefinition("get_person_locations", "Get the list of coordinates where a person was seen")]
public class GetPersonLocationsFunction : IFunctionHandler<GetPersonLocationsParameters>
{
    private readonly IAiDevsApiService _aiDevsApiService;

    public GetPersonLocationsFunction(IAiDevsApiService aiDevsApiService)
    {
        _aiDevsApiService = aiDevsApiService;
    }

    public async Task<string> ExecuteAsync(GetPersonLocationsParameters parameters, CancellationToken cancellationToken = default)
    {
        var locations = await _aiDevsApiService.GetLocationAsync(
            parameters.Name,
            parameters.Surname,
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
