using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IAgentSessionService
{
    IAsyncEnumerable<StreamUpdate> ExecuteAgentSessionStreamAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Gemini25Flash,
        double temperature = 0,
        int maxIterations = 20,
        Type? responseFormatType = null,
        IMcpClientService? mcpClient = null,
        CancellationToken cancellationToken = default);
}
