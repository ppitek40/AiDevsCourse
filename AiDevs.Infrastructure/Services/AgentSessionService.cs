using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Models;
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

    public async IAsyncEnumerable<StreamUpdate> ExecuteAgentSessionStreamAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Claude35Sonnet,
        double temperature = 0,
        int maxIterations = 20,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tools = BuildToolsFromHandlers(handlerTypes);
        var handlers = InstantiateHandlers(handlerTypes);

        var messages = new List<OpenRouterMessage>(initialMessages);
        var iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;

            yield return new StreamUpdate
            {
                Type = StreamUpdateType.Status,
                Content = $"Iteration {iteration}/{maxIterations}"
            };

            var messageContent = new StringBuilder();
            var toolCalls = new List<OpenRouterToolCall>();
            var currentToolCall = new Dictionary<int, ToolCallBuilder>();

            await foreach (var chunk in _openRouterService.StreamChatWithToolsAsync(
                messages,
                tools,
                toolChoice: "auto",
                model: model,
                temperature: temperature,
                cancellationToken: cancellationToken))
            {
                var delta = chunk.Choices?.FirstOrDefault()?.Delta;
                if (delta == null) continue;

                // Stream text content
                if (!string.IsNullOrEmpty(delta.Content))
                {
                    messageContent.Append(delta.Content);
                    yield return new StreamUpdate
                    {
                        Type = StreamUpdateType.LLMToken,
                        Content = delta.Content
                    };
                }

                // Accumulate tool calls
                if (delta.ToolCalls != null)
                {
                    foreach (var toolCall in delta.ToolCalls)
                    {
                        if (!currentToolCall.ContainsKey(0))
                        {
                            currentToolCall[0] = new ToolCallBuilder
                            {
                                Id = toolCall.Id ?? "",
                                Name = toolCall.Function?.Name ?? "",
                                Arguments = new StringBuilder()
                            };
                        }

                        if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                        {
                            currentToolCall[0].Arguments.Append(toolCall.Function.Arguments);
                        }

                        if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                        {
                            currentToolCall[0].Name = toolCall.Function.Name;
                        }

                        if (!string.IsNullOrEmpty(toolCall.Id))
                        {
                            currentToolCall[0].Id = toolCall.Id;
                        }
                    }
                }

                // Check for finish
                if (chunk.Choices?.FirstOrDefault()?.FinishReason != null)
                {
                    foreach (var builder in currentToolCall.Values)
                    {
                        toolCalls.Add(new OpenRouterToolCall
                        {
                            Id = builder.Id,
                            Type = "function",
                            Function = new OpenRouterFunctionCall
                            {
                                Name = builder.Name,
                                Arguments = builder.Arguments.ToString()
                            }
                        });
                    }
                }
            }

            // Add assistant message
            var assistantMessage = new OpenRouterMessage
            {
                Role = "assistant",
                Content = messageContent.Length > 0 ? messageContent.ToString() : null,
                ToolCalls = toolCalls.Count > 0 ? toolCalls : null
            };
            messages.Add(assistantMessage);

            // Check if done
            if (toolCalls.Count == 0)
            {
                yield return new StreamUpdate
                {
                    Type = StreamUpdateType.Complete,
                    IsComplete = true,
                    FinalResult = SolutionResult.Ok(messageContent.ToString())
                };
                yield break;
            }

            // Execute tool calls
            foreach (var toolCall in toolCalls)
            {
                var functionName = toolCall.Function.Name;
                var handler = handlers.FirstOrDefault(h => GetFunctionName(h.GetType()) == functionName);

                yield return new StreamUpdate
                {
                    Type = StreamUpdateType.ToolCall,
                    ToolName = functionName,
                    ToolInput = toolCall.Function.Arguments
                };

                string result;
                if (handler != null)
                {
                    result = await ExecuteHandler(handler, toolCall.Function.Arguments, cancellationToken);
                }
                else
                {
                    result = "Unknown function";
                }

                yield return new StreamUpdate
                {
                    Type = StreamUpdateType.ToolResult,
                    ToolName = functionName,
                    ToolOutput = result
                };

                messages.Add(new OpenRouterMessage
                {
                    Role = "tool",
                    Content = result,
                    ToolCallId = toolCall.Id
                });
            }
        }

        yield return new StreamUpdate
        {
            Type = StreamUpdateType.Complete,
            IsComplete = true,
            FinalResult = SolutionResult.Fail($"Agent session exceeded maximum iterations ({maxIterations})")
        };
    }

    private class ToolCallBuilder
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public StringBuilder Arguments { get; set; } = new();
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
