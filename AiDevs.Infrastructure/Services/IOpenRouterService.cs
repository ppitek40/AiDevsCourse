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
        string model = "openai/gpt-4",
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with message history using the specified model
    /// </summary>
    Task<string> ChatAsync(
        List<OpenRouterMessage> messages,
        string model = "openai/gpt-4",
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
