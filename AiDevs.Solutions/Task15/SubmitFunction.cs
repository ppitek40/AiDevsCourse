using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task15;

[FunctionDefinition("submit", "Submit the final list of movement instructions to verify the solution.")]
public class SubmitFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(SubmitParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not SubmitParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var result = await aiDevsApiService.VerifyAsync("savethem", p.Instructions, cancellationToken);

        return JsonSerializer.Serialize(result);
    }
}

public class SubmitParameters
{
    [JsonPropertyName("instructions")]
    [Parameter("The ordered list of movement instructions to submit as the solution (e.g., 'horse', 'left', 'right', 'up', 'down', 'rocket')")]
    public string[] Instructions { get; set; } = [];
}
