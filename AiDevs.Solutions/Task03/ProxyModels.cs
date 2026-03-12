using System.Text.Json.Serialization;

namespace AiDevs.Solutions.Task03;

public class ProxyRequest
{
    [JsonPropertyName("sessionID")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}

public class ProxyResponse
{
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}
