using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

[FunctionDefinition("get_access_level", "Get the access level for a person based on their name and birth year")]
public class GetAccessLevelFunction : IFunctionHandler<GetAccessLevelParameters>
{
    private readonly IAiDevsApiService _aiDevsApiService;

    public GetAccessLevelFunction(IAiDevsApiService aiDevsApiService)
    {
        _aiDevsApiService = aiDevsApiService;
    }

    public async Task<string> ExecuteAsync(GetAccessLevelParameters parameters, CancellationToken cancellationToken = default)
    {
        var accessLevel = await _aiDevsApiService.GetAccessLevelAsync(
            parameters.Name,
            parameters.Surname,
            parameters.BirthYear,
            cancellationToken);

        return JsonSerializer.Serialize(new { accessLevel });
    }
}

public class GetAccessLevelParameters
{
    [JsonPropertyName("name")]
    [Parameter("Person's first name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    [Parameter("Person's last name")]
    public string Surname { get; set; } = string.Empty;

    [JsonPropertyName("birthYear")]
    [Parameter("Person's birth year (e.g., 1987)")]
    public int BirthYear { get; set; }
}
