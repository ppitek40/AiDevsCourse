using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task08;

[FunctionDefinition("log_reader", "Read logs from the API")]
public class LogReaderFunction(IAgentSessionService agentSessionService) : IFunctionHandler
{
    public Type ParametersType => typeof(LogReaderParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not LogReaderParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var systemPrompt = @"You are a log analyst.You have access to the `ReadLogChunk` tool.Task: Find all log
            entries related to criteria provided by the user.

        Instructions:
        1.Read chunks of the log file sequentially using ReadLogChunk 
        2.From each chunk, extract ONLY the relevant entries
        3.Return a compact JSON list of matches with:
        timestamp, level, message
        4.Stop when you've read all chunks
        5.Do NOT return irrelevant lines — be selective";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = p.Prompt }
        };

        var result = new List<string>();
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(LoadLogChunkFunction)],
            model: OpenRouterModel.Gpt5Nano,
            temperature: 0,
            maxIterations: 30,
            cancellationToken: cancellationToken))
        {
            if (update.IsComplete && update.FinalResult?.Success == true)
                result.Add(update.FinalResult.Output);
        }

        return JsonSerializer.Serialize(new
        {
            statusCode = 200,
            response = result
        });
    }
}

public class LogReaderParameters
{
    [JsonPropertyName("prompt")]
    [Parameter("The prompt to send to the API", required: true)]
    public string Prompt { get; set; } = string.Empty;
}
