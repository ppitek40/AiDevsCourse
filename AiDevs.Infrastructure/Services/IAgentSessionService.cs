using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IAgentSessionService
{
    Task<string> ExecuteAgentSessionAsync(
        List<OpenRouterMessage> initialMessages,
        Type[] handlerTypes,
        string model = "anthropic/claude-3.5-sonnet",
        double temperature = 0,
        int maxIterations = 20,
        CancellationToken cancellationToken = default);
}
