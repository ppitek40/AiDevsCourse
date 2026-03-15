using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;

namespace AiDevs.Infrastructure.Models;

public class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-4";

    [JsonPropertyName("messages")]
    public List<IOpenRouterMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("tools")]
    public List<OpenRouterTool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    public bool ParallelToolCalls { get; set; } = true;
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; } 
}

public class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";
    [JsonPropertyName("json_schema")]
    public JsonSchema JsonSchema { get; set; }
}

public class JsonSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("strict")]
    public bool Strict { get; set; } = true;

    [JsonPropertyName("schema")]
    public Schema Schema { get; set; }
}

public class Schema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    [JsonPropertyName("properties")]
    public Dictionary<string, ResponseProperty> Properties { get; set; } = new();
    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }
    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties { get; set; } = false;
}

public class ResponseProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

[JsonConverter(typeof(JsonPolymorphicConverter))]
public interface IOpenRouterMessage
{
    string Role { get; set; }
    List<OpenRouterToolCall>? ToolCalls { get; set; }
    string? ToolCallId { get; set; }
}

public class JsonPolymorphicConverter : JsonConverter<IOpenRouterMessage>
{
    public override IOpenRouterMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IOpenRouterMessage value, JsonSerializerOptions options)
    {
        if (value is MultiModalOpenRouterMessage modal)
            JsonSerializer.Serialize(writer, modal, options);
        else if (value is OpenRouterMessage msg)
            JsonSerializer.Serialize(writer, msg, options);
        else
            throw new NotSupportedException($"Unsupported message type: {value.GetType().Name}");
    }
}

public class MultiModalOpenRouterMessage : IOpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("content")]
    public MultiModalContent[] Content { get; set; }
    [JsonPropertyName("tool_calls")]
    public List<OpenRouterToolCall>? ToolCalls { get; set; }
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

public class MultiModalContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "image_url";

    [JsonPropertyName("image_url")]
    public ImageUrl? ImageUrl { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; } 
}

public class ImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class OpenRouterMessage : IOpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenRouterToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

public class OpenRouterResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenRouterChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

public class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class OpenRouterTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenRouterFunction Function { get; set; } = new();
    
    [JsonIgnore]
    public IFunctionHandler Handler { get; set; }
}

public class OpenRouterFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }
}

public class OpenRouterToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenRouterFunctionCall Function { get; set; } = new();

    [JsonPropertyName("index")]
    public int? Index { get; set; }
}

public class OpenRouterFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class OpenRouterStreamChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenRouterStreamChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

public class OpenRouterStreamChoice
{
    [JsonPropertyName("delta")]
    public OpenRouterDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class OpenRouterDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenRouterToolCall>? ToolCalls { get; set; }
}
