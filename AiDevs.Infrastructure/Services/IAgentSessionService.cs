using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IAgentSessionService
{
    Task<string> ExecuteAgentSessionAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Claude35Sonnet,
        double temperature = 0,
        int maxIterations = 20,
        CancellationToken cancellationToken = default);
}
