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

    public async Task<string> CompleteAsync(
        string prompt,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterRequest
        {
            Model = model.ToModelId(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = new List<OpenRouterMessage>
            {
                new() { Role = "user", Content = prompt }
            }
        };

        return await SendRequestAsync(request, cancellationToken);
    }

    public async Task<string> ChatAsync(
        List<OpenRouterMessage> messages,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterRequest
        {
            Model = model.ToModelId(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = messages
        };

        return await SendRequestAsync(request, cancellationToken);
    }

    public async Task<OpenRouterResponse> ChatWithToolsAsync(
        List<OpenRouterMessage> messages,
        List<OpenRouterTool>? tools = null,
        object? toolChoice = null,
        OpenRouterModel model = OpenRouterModel.Gpt4o,
        double temperature = 0.7,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = new OpenRouterRequest
        {
            Model = model.ToModelId(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Messages = messages,
            Tools = tools,
            ToolChoice = toolChoice
        };

        return await SendRequestWithFullResponseAsync(request, cancellationToken);
    }

    private async Task<string> SendRequestAsync(OpenRouterRequest request, CancellationToken cancellationToken)
    {
        var openRouterResponse = await SendRequestWithFullResponseAsync(request, cancellationToken);
        return openRouterResponse.Choices?.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("No response from OpenRouter");
    }

    private async Task<OpenRouterResponse> SendRequestWithFullResponseAsync(
        OpenRouterRequest request,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = content
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson);

        return openRouterResponse
            ?? throw new InvalidOperationException("No response from OpenRouter");
    }
}
