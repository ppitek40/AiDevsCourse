using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task20;

[FunctionDefinition("send_command", "Send a command to the API endpoint")]
public class SendCommandTask20Function(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SendCommandTask20Parameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SendCommandTask20Parameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Command))
            return JsonSerializer.Serialize(new { error = "Command cannot be empty" });

        try
        {
            var result = await apiService.VerifyAsync("task20", p.Command, cancellationToken);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to send command: {ex.Message}" });
        }
    }
}

public class SendCommandTask20Parameters
{
    [JsonPropertyName("command")]
    [Parameter("The JSON command to send to the API", required: true)]
    public string Command { get; set; } = string.Empty;
}
