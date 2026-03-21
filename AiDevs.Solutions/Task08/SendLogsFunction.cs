using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task08;

[FunctionDefinition("send_logs", "Submit the logs to the Centrala review")]
public class SendLogsFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SendLogsParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SendLogsParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var result = await apiService.VerifyAsync("failure", new { logs = p.Logs }, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            response = result.Output,
            error = result.Error,
        });
    }
}

public class SendLogsParameters
{
    [JsonPropertyName("logs")]
    [Parameter("The logs to send", required: true)]
    public string Logs { get; set; } = string.Empty;
}