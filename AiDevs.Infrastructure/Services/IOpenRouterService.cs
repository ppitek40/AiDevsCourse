using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

/// <summary>
/// Service for interacting with OpenRouter API
/// </summary>
public interface IOpenRouterService
{
    /// <summary>
    /// Complete a prompt using the specified model
    /// </summary>
    Task<string> CompleteAsync(
        string prompt,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with message history using the specified model
    /// </summary>
    Task<string> ChatAsync(
        List<OpenRouterMessage> messages,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with message history and function calling support
    /// </summary>
    Task<OpenRouterResponse> ChatWithToolsAsync(
        List<OpenRouterMessage> messages,
        List<OpenRouterTool>? tools = null,
        object? toolChoice = null,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
