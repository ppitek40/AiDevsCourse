using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task16;

[FunctionDefinition("use_api", "Call an system API. use {\"action\": \"help\"} as a string to get the list of available actions ")]
public class UseApiFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(UseApiParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not UseApiParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });
        
        var json = JsonSerializer.Deserialize<object>(p.Command);
        if (json == null)
            return JsonSerializer.Serialize(new { error = "Invalid command format" });

        var result = await apiService.VerifyAsync("okoeditor", json, cancellationToken);

        return JsonSerializer.Serialize(result);
    }
}

public class UseApiParameters
{

    [JsonPropertyName("command")]
    [Parameter("The command to execute (e.g {'action': 'help'})", required: true)]
    public string Command { get; set; } = string.Empty;
}
