using System.Runtime.CompilerServices;
using System.Text;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.Services;

public class AgentSessionService(IOpenRouterService openRouterService, IServiceProvider serviceProvider, IToolsService toolsService)
    : IAgentSessionService
{
    public async IAsyncEnumerable<StreamUpdate> ExecuteAgentSessionStreamAsync(
        List<OpenRouterMessage> initialMessages,
        List<Type> handlerTypes,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0,
        int maxIterations = 20,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tools = toolsService.GetTools(handlerTypes);

        var messages = new List<OpenRouterMessage>(initialMessages);

        for (var i =0; i < maxIterations; i++)
        {
            yield return StreamUpdate.Status($"Iteration {i + 1}/{maxIterations}");

            var messageContent = new StringBuilder();
            var toolCalls = new List<OpenRouterToolCall>();
            var currentToolCall = new Dictionary<int, ToolCallBuilder>();

            await foreach (var chunk in openRouterService.StreamChatWithToolsAsync(
                messages,
                tools,
                toolChoice: "auto",
                model: model,
                temperature: temperature,
                cancellationToken: cancellationToken))
            {
                await foreach (var p in HandleDataChunks(chunk, messageContent, currentToolCall, toolCalls).WithCancellation(cancellationToken))
                    yield return p;
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
                yield return StreamUpdate.Complete(SolutionResult.Ok(messageContent.ToString()));
                yield break;
            }

            // Execute tool calls
            foreach (var toolCall in toolCalls)
            {
                var functionName = toolCall.Function.Name;
                var handler = tools.FirstOrDefault(t => t.Function.Name == functionName)?.Handler;

                yield return StreamUpdate.ToolCall(functionName, toolCall.Function.Arguments);

                string result;
                if (handler != null)
                    result = await toolsService.ExecuteToolAsync(handler, toolCall.Function.Arguments, cancellationToken);
                else
                    result = "Unknown function";

                yield return StreamUpdate.ToolResult(functionName, result);

                messages.Add(new OpenRouterMessage
                {
                    Role = "tool",
                    Content = result,
                    ToolCallId = toolCall.Id
                });
            }
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail($"Agent session exceeded maximum iterations ({maxIterations})"));
    }

    private async IAsyncEnumerable<StreamUpdate> HandleDataChunks(
        OpenRouterStreamChunk chunk,
        StringBuilder messageContent,
        Dictionary<int, ToolCallBuilder> currentToolCall,
        List<OpenRouterToolCall> toolCalls)
    {
        var delta = chunk.Choices?.FirstOrDefault()?.Delta;
        if (delta == null) yield break;

        // Stream text content
        if (!string.IsNullOrEmpty(delta.Content))
        {
            messageContent.Append(delta.Content);
            yield return StreamUpdate.LLMToken(delta.Content);
        }

        // Accumulate tool calls
        if (delta.ToolCalls != null)
            toolsService.BuildTools(delta.ToolCalls, currentToolCall);

        // Check for finish
        if (chunk.Choices?.FirstOrDefault()?.FinishReason != null)
        {
            toolCalls.AddRange(currentToolCall.Values
                .Select(builder => new OpenRouterToolCall
                {
                    Id = builder.Id, 
                    Type = "function", 
                    Function = new OpenRouterFunctionCall { Name = builder.Name, Arguments = builder.Arguments.ToString() }
                }));
        }
    }
}