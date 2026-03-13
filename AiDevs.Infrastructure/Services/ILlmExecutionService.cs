using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface ILlmExecutionService
{
    IAsyncEnumerable<LlmExecutionUpdate> ExecuteCustomLlmAsync(
        CustomLlmRequest request,
        CancellationToken cancellationToken = default);
}

public class CustomLlmRequest
{
    public OpenRouterModel Model { get; set; } = OpenRouterModel.Gpt4o;
    public List<Type>? ToolTypes { get; set; }
    public string? SystemMessage { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int Iterations { get; set; } = 1;
}

public class LlmExecutionUpdate
{
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Name { get; set; }
    public string? Arguments { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public int? Iteration { get; set; }
    public int? Total { get; set; }
}
