using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task18;

[FunctionDefinition("send_command", "Send a command to the API endpoint")]
public class SendCommandTask18Function(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SendCommandTask18Parameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SendCommandTask18Parameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Command))
            return JsonSerializer.Serialize(new { error = "Command cannot be empty" });

        try
        {
            var payload = JsonSerializer.Deserialize<object>(p.Command);
            if (payload == null)
                return JsonSerializer.Serialize(new { error = "Invalid command format — must be valid JSON" });

            var result = await apiService.VerifyAsync("domatowo", payload, cancellationToken);
            return JsonSerializer.Serialize(result);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { error = "Command must be a valid JSON string" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to send command: {ex.Message}" });
        }
    }
}

public class SendCommandTask18Parameters
{
    [JsonPropertyName("command")]
    [Parameter("The JSON command to send to the API (e.g. {\"action\": \"help\"})", required: true)]
    public string Command { get; set; } = string.Empty;
}
