using System.Text;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public class LlmExecutionService(
    IOpenRouterService openRouterService,
    IToolsService toolsService) : ILlmExecutionService
{
    public async IAsyncEnumerable<LlmExecutionUpdate> ExecuteCustomLlmAsync(
        CustomLlmRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<IOpenRouterMessage>();

        // Add system message if provided
        if (!string.IsNullOrWhiteSpace(request.SystemMessage))
        {
            messages.Add(new OpenRouterMessage
            {
                Role = "system",
                Content = request.SystemMessage
            });
        }

        // Add user message
        messages.Add(new OpenRouterMessage
        {
            Role = "user",
            Content = request.UserMessage
        });

        // Get tools if specified
        List<OpenRouterTool>? tools = null;
        if (request.ToolTypes?.Count > 0)
        {
            tools = toolsService.GetTools(request.ToolTypes);
        }

        var iterations = Math.Max(1, request.Iterations);

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            if (iteration > 0)
            {
                // Send iteration marker
                yield return new LlmExecutionUpdate
                {
                    Type = "iteration",
                    Iteration = iteration + 1,
                    Total = iterations
                };
            }

            var contentBuilder = new StringBuilder();
            var currentToolCalls = new Dictionary<int, ToolCallBuilder>();
            var finishReason = "";

            await foreach (var chunk in openRouterService.StreamChatWithToolsAsync(
                messages,
                tools,
                null,
                request.Model,
                request.Temperature,
                null,
                cancellationToken))
            {
                // Stream content
                var content = chunk.Choices?.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    contentBuilder.Append(content);
                    yield return new LlmExecutionUpdate
                    {
                        Type = "content",
                        Content = content
                    };
                }

                // Handle tool calls
                var toolCalls = chunk.Choices?.FirstOrDefault()?.Delta?.ToolCalls;
                if (toolCalls != null)
                {
                    toolsService.BuildTools(toolCalls, currentToolCalls);
                }

                // Check finish reason
                var chunkFinishReason = chunk.Choices?.FirstOrDefault()?.FinishReason;
                if (!string.IsNullOrEmpty(chunkFinishReason))
                {
                    finishReason = chunkFinishReason;
                }
            }

            // Add assistant message to conversation
            if (contentBuilder.Length > 0 || currentToolCalls.Count > 0)
            {
                var assistantMessage = new OpenRouterMessage
                {
                    Role = "assistant",
                    Content = contentBuilder.ToString()
                };

                if (currentToolCalls.Count > 0)
                {
                    assistantMessage.ToolCalls = currentToolCalls.Values.Select(tc => new OpenRouterToolCall
                    {
                        Id = tc.Id,
                        Type = "function",
                        Function = new OpenRouterFunctionCall
                        {
                            Name = tc.Name,
                            Arguments = tc.Arguments.ToString()
                        }
                    }).ToList();
                }

                messages.Add(assistantMessage);
            }

            // Execute tool calls if any
            if (currentToolCalls.Count > 0 && tools != null)
            {
                foreach (var toolCall in currentToolCalls.Values)
                {
                    var tool = tools.FirstOrDefault(t => t.Function.Name == toolCall.Name);
                    if (tool?.Handler == null)
                    {
                        continue;
                    }

                    yield return new LlmExecutionUpdate
                    {
                        Type = "tool_call",
                        Name = toolCall.Name,
                        Arguments = toolCall.Arguments.ToString()
                    };

                    var result = await toolsService.ExecuteToolAsync(
                        tool.Handler,
                        toolCall.Arguments.ToString(),
                        cancellationToken);

                    messages.Add(new OpenRouterMessage
                    {
                        Role = "tool",
                        Content = result,
                        ToolCallId = toolCall.Id
                    });

                    yield return new LlmExecutionUpdate
                    {
                        Type = "tool_result",
                        Name = toolCall.Name,
                        Result = result
                    };
                }

                // Continue conversation with tool results if this is not the last iteration
                if (iteration < iterations - 1)
                {
                    continue;
                }
            }

            // If no tool calls or last iteration, we're done
            if (currentToolCalls.Count == 0 || iteration == iterations - 1)
            {
                break;
            }
        }

        yield return new LlmExecutionUpdate
        {
            Type = "complete"
        };
    }
}
