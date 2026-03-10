using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public class AgentSessionService : IAgentSessionService
{
    private readonly IOpenRouterService _openRouterService;
    private readonly IServiceProvider _serviceProvider;

    public AgentSessionService(IOpenRouterService openRouterService, IServiceProvider serviceProvider)
    {
        _openRouterService = openRouterService;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> ExecuteAgentSessionAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Claude35Sonnet,
        double temperature = 0,
        int maxIterations = 20,
        CancellationToken cancellationToken = default)
    {
        var tools = BuildToolsFromHandlers(handlerTypes);
        var handlers = InstantiateHandlers(handlerTypes);

        var messages = new List<OpenRouterMessage>(initialMessages);
        var iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;

            var response = await _openRouterService.ChatWithToolsAsync(
                messages,
                tools,
                toolChoice: "auto",
                model: model,
                temperature: temperature,
                cancellationToken: cancellationToken
            );

            var assistantMessage = response.Choices?.FirstOrDefault()?.Message;
            if (assistantMessage == null)
                break;

            messages.Add(assistantMessage);

            // Check if done
            if (assistantMessage.ToolCalls == null || assistantMessage.ToolCalls.Count == 0)
            {
                return assistantMessage.Content ?? "";
            }

            // Execute tool calls
            foreach (var toolCall in assistantMessage.ToolCalls)
            {
                var functionName = toolCall.Function.Name;
                var handler = handlers.FirstOrDefault(h => GetFunctionName(h.GetType()) == functionName);

                string result;
                if (handler != null)
                {
                    result = await ExecuteHandler(handler, toolCall.Function.Arguments, cancellationToken);
                }
                else
                {
                    result = "Unknown function";
                }

                messages.Add(new OpenRouterMessage
                {
                    Role = "tool",
                    Content = result,
                    ToolCallId = toolCall.Id
                });
            }
        }

        throw new InvalidOperationException($"Agent session exceeded maximum iterations ({maxIterations})");
    }

    private List<object> InstantiateHandlers(List<Type> handlerTypes)
    {
        return handlerTypes
            .Select(_serviceProvider.GetService)
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
    }

    private List<OpenRouterTool> BuildToolsFromHandlers(List<Type> handlerTypes)
    {
        var tools = new List<OpenRouterTool>();

        foreach (var handlerType in handlerTypes)
        {
            var functionAttr = handlerType.GetCustomAttribute<FunctionDefinitionAttribute>();
            if (functionAttr == null)
                continue;

            var parametersType = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionHandler<>))
                ?.GetGenericArguments()
                .FirstOrDefault();

            if (parametersType == null)
                continue;

            var properties = new Dictionary<string, object>();
            var required = new List<string>();

            foreach (var prop in parametersType.GetProperties())
            {
                var paramAttr = prop.GetCustomAttribute<ParameterAttribute>();
                if (paramAttr == null)
                    continue;

                var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name.ToLower();

                properties[jsonName] = new
                {
                    type = GetJsonType(prop.PropertyType),
                    description = paramAttr.Description
                };

                if (paramAttr.Required)
                {
                    required.Add(jsonName);
                }
            }

            tools.Add(new OpenRouterTool
            {
                Type = "function",
                Function = new OpenRouterFunction
                {
                    Name = functionAttr.Name,
                    Description = functionAttr.Description,
                    Parameters = new
                    {
                        type = "object",
                        properties,
                        required = required.ToArray()
                    }
                }
            });
        }

        return tools;
    }

    private async Task<string> ExecuteHandler(object handler, string argumentsJson, CancellationToken cancellationToken)
    {
        var handlerType = handler.GetType();
        var interfaceType = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionHandler<>));

        if (interfaceType == null)
            throw new InvalidOperationException($"Handler {handlerType.Name} does not implement IFunctionHandler<>");

        var parametersType = interfaceType.GetGenericArguments()[0];
        var parameters = JsonSerializer.Deserialize(argumentsJson, parametersType);

        var executeMethod = interfaceType.GetMethod("ExecuteAsync");
        if (executeMethod == null)
            throw new InvalidOperationException($"ExecuteAsync method not found on {interfaceType.Name}");

        var task = (Task<string>)executeMethod.Invoke(handler, new[] { parameters, cancellationToken })!;
        return await task;
    }

    private string GetFunctionName(Type handlerType)
    {
        var attr = handlerType.GetCustomAttribute<FunctionDefinitionAttribute>();
        return attr?.Name ?? handlerType.Name;
    }

    private static string GetJsonType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long))
            return "integer";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";

        return "string";
    }
}
