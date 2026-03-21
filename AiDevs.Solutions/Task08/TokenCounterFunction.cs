using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;

namespace AiDevs.Solutions.Task08;

[FunctionDefinition("token_counter", "Count the number of tokens in a text")]
public class TokenCounterFunction : IFunctionHandler
{
    public Type ParametersType => typeof(TokenCounterParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not TokenCounterParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var tokenCount = p.Text.Length / 3;
        tokenCount = (int)(1.1 * tokenCount);

        return JsonSerializer.Serialize(new
        {
            tokenCount
        });
    } 
}

public class TokenCounterParameters
{
    [JsonPropertyName("text")]
    [Parameter("The text to count tokens for", required: true)]
    public string Text { get; set; } = string.Empty;
}