using System.Text.Json.Serialization;

namespace AiDevs.Core.Models;

/// <summary>
/// Standard result returned by all task solutions
/// </summary>
public class SolutionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("output")]
    public string? Output { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    [JsonPropertyName("metadata")]
    private Dictionary<string, object>? Metadata { get; set; }

    public static SolutionResult Ok(string output, Dictionary<string, object>? metadata = null)
    {
        return new SolutionResult
        {
            Success = true,
            Output = output,
            Metadata = metadata
        };
    }

    public static SolutionResult Fail(string error)
    {
        return new SolutionResult
        {
            Success = false,
            Error = error
        };
    }

    public SolutionResult AddMetadata(Dictionary<string, object> metadata)
    {
        Metadata ??= new Dictionary<string, object>();
        foreach (var kvp in metadata)
            Metadata[kvp.Key] = kvp.Value;
        return this;
    }
}

/// <summary>
/// Represents a streaming update from a task solution
/// </summary>
public class StreamUpdate
{
    [JsonPropertyName("type")]
    public StreamUpdateType Type { get; set; }
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    [JsonPropertyName("toolName")]
    public string? ToolName { get; set; }
    [JsonPropertyName("toolInput")]
    public string? ToolInput { get; set; }
    [JsonPropertyName("toolOutput")]
    public string? ToolOutput { get; set; }
    [JsonPropertyName("isComplete")]
    public bool IsComplete { get; set; }
    [JsonPropertyName("finalResult")]
    public SolutionResult? FinalResult { get; set; }

   
    public static StreamUpdate Complete(SolutionResult result)
    {
        return new StreamUpdate
        {
            Type = StreamUpdateType.Complete,
            IsComplete = true,
            FinalResult = result
        };
    }

    public static StreamUpdate Status(string status)
    {
        return new StreamUpdate
        {
            Type = StreamUpdateType.Status,
            Content = status
        };
    }

    public static StreamUpdate ToolCall(string toolName, string toolInput)
    {
        return new StreamUpdate
        {
            Type = StreamUpdateType.ToolCall,
            ToolName = toolName,
            ToolInput = toolInput
        };
    }

    public static StreamUpdate ToolResult(string toolName, string toolOutput)
    {
        return new StreamUpdate
        {
            Type = StreamUpdateType.ToolResult,
            ToolName = toolName,
            ToolOutput = toolOutput
        };
    }

    public static StreamUpdate LLMToken(string token)
    {
        return new StreamUpdate
        {
            Type = StreamUpdateType.LLMToken,
            Content = token
        };
    }
}

public enum StreamUpdateType
{
    LLMToken,
    ToolCall,
    ToolResult,
    Status,
    Complete
}
