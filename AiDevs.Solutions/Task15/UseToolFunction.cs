using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Solutions.Task15;

[FunctionDefinition("use_tool", "This function allows you to use a set of tools. The essential one is 'toolsearch', which searches for other tools.")]
public class UseToolFunction(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IFunctionHandler
{
    public Type ParametersType => typeof(UseToolParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not UseToolParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var client = httpClientFactory.CreateClient();

        var json = JsonSerializer.Serialize(new { apikey = configuration["AiDevs:ApiKey"], query = p.Query});


        var response = await client.PostAsync(
            configuration["AiDevs:BaseUrl"] + "/api/" + p.ToolName, 
            new StringContent(json, Encoding.UTF8, "application/json"), 
            cancellationToken);

        return await response.Content.ReadAsStringAsync();
    }
}

public class UseToolParameters
{
    [JsonPropertyName("tool_name")]
    [Parameter("The name of the tool to call (e.g., 'toolsearch')")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    [Parameter("The input to process")]
    public string Query { get; set; } = string.Empty;
}
