using System.Text;
using System.Text.Json;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public interface IToolsService
{
    List<OpenRouterTool> GetTools(List<Type> types);

    void BuildTools(
        List<OpenRouterToolCall> toolCalls,
        Dictionary<int, ToolCallBuilder> currentToolCall);

    Task<string> ExecuteToolAsync(IFunctionHandler handler, string argumentsJson, CancellationToken cancellationToken);
}

public class ToolsService(IServiceProvider serviceProvider) : IToolsService
{
    public List<OpenRouterTool> GetTools(List<Type> types)
    {
        var handlers = types
            .Select(serviceProvider.GetService)
            .Where(h => h != null)
            .Where(h => h is IFunctionHandler)
            .Select(h => (IFunctionHandler)h!)
            .ToList();

        return handlers.Select(h => h.BuildToolFromHandler())
            .Where(t => t != null).Select(t => t!).ToList();
    }

    public void BuildTools(
        List<OpenRouterToolCall> toolCalls,
        Dictionary<int, ToolCallBuilder> currentToolCall)
    {
        foreach (var toolCall in toolCalls)
        {
            if (!currentToolCall.TryGetValue(0, out var value))
            {
                value = new ToolCallBuilder
                {
                    Id = toolCall.Id ?? "",
                    Name = toolCall.Function?.Name ?? "",
                    Arguments = new StringBuilder()
                };
                currentToolCall[0] = value;
            }

            if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                value.Arguments.Append(toolCall.Function.Arguments);

            if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                value.Name = toolCall.Function.Name;

            if (!string.IsNullOrEmpty(toolCall.Id))
                value.Id = toolCall.Id;
        }
    }

    public Task<string> ExecuteToolAsync(IFunctionHandler handler, string argumentsJson, CancellationToken cancellationToken)
    {
        var parameters = JsonSerializer.Deserialize(argumentsJson, handler.ParametersType);
        if (parameters == null)
            return Task.FromResult($"Failed to deserialize arguments for function: {handler.GetType().Name}");

        return handler.ExecuteAsync(parameters, cancellationToken);
    }
}