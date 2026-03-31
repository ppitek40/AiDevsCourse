using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task10;

[FunctionDefinition("send_drone_instructions", "Send instructions to the drone")]
public class SendDroneInstructionsFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SendDroneInstructionsParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SendDroneInstructionsParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var result = await apiService.VerifyAsync("drone", p, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}

public class SendDroneInstructionsParameters
{
    [JsonPropertyName("instructions")]
    [Parameter("Instructions for the drone as a list of strings.", required: true)]
    public List<string> Instructions { get; set; } = [];
}