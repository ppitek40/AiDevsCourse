using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IAgentSessionService
{
    IAsyncEnumerable<StreamUpdate> ExecuteAgentSessionStreamAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Claude35Sonnet,
        double temperature = 0,
        int maxIterations = 20,
        CancellationToken cancellationToken = default);
}
