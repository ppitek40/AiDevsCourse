using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

[FunctionDefinition("get_access_level", "Get the access level for a person based on their name and birth year")]
public class GetAccessLevelFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(GetAccessLevelParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not GetAccessLevelParameters p)
            return "Invalid parameters type";

        var accessLevel = await aiDevsApiService.GetAccessLevelAsync(
            p.Name,
            p.Surname,
            p.BirthYear,
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
