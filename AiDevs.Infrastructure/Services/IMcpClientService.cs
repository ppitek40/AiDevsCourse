using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IMcpClientService : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task<List<OpenRouterTool>> GetToolsAsync(CancellationToken cancellationToken = default);
    Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
