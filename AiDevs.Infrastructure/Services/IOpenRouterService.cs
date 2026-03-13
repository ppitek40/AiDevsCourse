using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

/// <summary>
/// Service for interacting with OpenRouter API
/// </summary>
public interface IOpenRouterService
{
    /// <summary>
    /// Stream chat responses
    /// </summary>
    IAsyncEnumerable<string> StreamChatAsync(
        List<IOpenRouterMessage> messages,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream chat responses with function calling support
    /// </summary>
    IAsyncEnumerable<OpenRouterStreamChunk> StreamChatWithToolsAsync(
        List<IOpenRouterMessage> messages,
        List<OpenRouterTool>? tools = null,
        object? toolChoice = null,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
