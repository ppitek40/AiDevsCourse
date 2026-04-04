using System.Text.Json;
using AiDevs.Infrastructure.Models;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AiDevs.Infrastructure.Services;

public class McpClientService : IMcpClientService
{
    private readonly StdioClientTransportOptions _transportOptions;
    private McpClient? _client;

    private McpClientService(StdioClientTransportOptions transportOptions)
    {
        _transportOptions = transportOptions;
    }

    public static IMcpClientService CreatePlaywright() =>
        new McpClientService(new StdioClientTransportOptions
        {
            Name = "Playwright",
            Command = "npx",
            Arguments = ["-y", "@playwright/mcp@latest"]
        });

    public static IMcpClientService Create(string name, string command, IEnumerable<string>? arguments = null) =>
        new McpClientService(new StdioClientTransportOptions
        {
            Name = name,
            Command = command,
            Arguments = arguments?.ToArray() ?? []
        });

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var transport = new StdioClientTransport(_transportOptions);
        _client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    public async Task<List<OpenRouterTool>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var mcpTools = await _client!.ListToolsAsync(cancellationToken: cancellationToken);

        return mcpTools.Select(t => new OpenRouterTool
        {
            Type = "function",
            Function = new OpenRouterFunction
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.JsonSchema
            }
        }).ToList();
    }

    public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        Dictionary<string, object?> arguments;
        try
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson)
                        ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            arguments = new Dictionary<string, object?>();
        }

        var result = await _client!.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

        var textContent = result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text)
            .ToList();

        if (textContent.Count == 0)
            return JsonSerializer.Serialize(result.Content);

        return textContent.Count == 1 ? textContent[0] : JsonSerializer.Serialize(textContent);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
            await _client.DisposeAsync();
    }

    private void EnsureConnected()
    {
        if (_client == null)
            throw new InvalidOperationException("MCP client is not connected. Call ConnectAsync first.");
    }
}
