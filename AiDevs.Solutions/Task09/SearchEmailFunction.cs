
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Solutions.Task09;

[FunctionDefinition("search_email", "API to search the email inbox using commands. Use {\"action\":\"help\",\"page\":1} for help.")]
public class SearchEmailFunction(HttpClient httpClient, IConfiguration configuration) : IFunctionHandler
{
    private readonly string _apiKey = configuration["AiDevs:ApiKey"]
        ?? throw new InvalidOperationException("AiDevs API key not configured");
    private const string ZmailBaseUrl = "https://hub.ag3nts.org/api/zmail";

    public Type ParametersType => typeof(SearchEmailParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SearchEmailParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Command))
            return JsonSerializer.Serialize(new { error = "Command is required" });

        var payload = JsonSerializer.Deserialize<ExpandoObject>(p.Command);
        payload.TryAdd("apikey", _apiKey);

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(ZmailBaseUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return responseBody;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

public class SearchEmailParameters
{
    [JsonPropertyName("command")]
    [Parameter("Command to execute (e.g. {\"apiKey\":\"empty\",\"action\":\"help\",\"page\":1})")]
    public string Command { get; set; } = "{}";
}
