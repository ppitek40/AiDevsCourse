using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiDevs.Infrastructure.Models;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Infrastructure.Services;

public class OpenRouterService : IOpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("OpenRouter API key not configured");
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        List<OpenRouterMessage> messages,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterRequest
        {
            Model = model.ToModelId(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = messages,
            Stream = true
        };

        await foreach (var chunk in StreamRequestAsync(request, cancellationToken))
        {
            var content = chunk.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    public async IAsyncEnumerable<OpenRouterStreamChunk> StreamChatWithToolsAsync(
        List<OpenRouterMessage> messages,
        List<OpenRouterTool>? tools = null,
        object? toolChoice = null,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterRequest
        {
            Model = model.ToModelId(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = messages,
            Tools = tools,
            ToolChoice = toolChoice,
            Stream = true
        };

        await foreach (var chunk in StreamRequestAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<OpenRouterStreamChunk> StreamRequestAsync(
        OpenRouterRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = content
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                var chunk = JsonSerializer.Deserialize<OpenRouterStreamChunk>(data);
                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }
}
