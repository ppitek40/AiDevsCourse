using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task13;

[FunctionDefinition("send_command", "Send a command to the robot")]
public class SendCommandFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SendCommandParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SendCommandParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Command))
            return JsonSerializer.Serialize(new { error = "Command cannot be empty" });

        try
        {
            var result = await apiService.VerifyAsync("reactor", new {command = p.Command}, cancellationToken);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Failed to send command: {ex.Message}",
                success = false
            });
        }
    }
}

public class SendCommandParameters
{
    [JsonPropertyName("command")]
    [Parameter("The command to send to the robot (e.g 'start', 'left', 'right', 'wait')")]
    public string Command { get; set; } = string.Empty;
}
