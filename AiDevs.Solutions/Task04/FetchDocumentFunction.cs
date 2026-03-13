using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task04;

[FunctionDefinition("fetch_document",
    "Fetches a documentation file from the centrala server. Use this to download markdown file, image, or any other documentation file.")]
public class FetchDocumentFunction(
    IHttpClientFactory httpClientFactory,
    IOpenRouterService openRouterService) : IFunctionHandler
{
    private const string BaseUrl = "https://hub.ag3nts.org/dane/doc";

    public Type ParametersType => typeof(FetchDocumentParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not FetchDocumentParameters p)
            return JsonSerializer.Serialize(new FetchDocumentResult
                { Success = false, Error = "Invalid parameters type" });

        try
        {
            if (p.Filename.EndsWith(".png") || p.Filename.EndsWith(".jpg") || p.Filename.EndsWith(".jpeg"))
            {
                var imageContent = await HandleImage(p.Filename);
                return JsonSerializer.Serialize(new FetchDocumentResult
                {
                    Success = true,
                    Filename = p.Filename,
                    IsImage = true,
                    Base64Content = imageContent,
                    ContentType = "image/png"
                });
            }

            var url = $"{BaseUrl}/{p.Filename}";
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new FetchDocumentResult
                {
                    Success = false,
                    Error = $"Failed to fetch {p.Filename}: HTTP {response.StatusCode}"
                });
            }

            // It's a text file
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Serialize(new FetchDocumentResult
            {
                Success = true,
                Filename = p.Filename,
                IsImage = false,
                Content = content
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new FetchDocumentResult
            {
                Success = false,
                Error = $"Error fetching {p.Filename}: {ex.Message}"
            });
        }
    }

    private async Task<string> HandleImage(string filename)
    {
        var messages = new List<IOpenRouterMessage>
        {
            new OpenRouterMessage { Role = "system", Content = $"Analyze the image and return all the data from this image. Return ONLY the extracted data, do not add any additional comments or explanations." },
            new MultiModalOpenRouterMessage() 
            {
                Role = "user",
                Content = [new MultiModalContent{
                    Type = "text", 
                    Text = "Analyze the image and return all the data from this image. Return ONLY the extracted data, do not add any additional comments or explanations."
                }, new MultiModalContent{
                    Type = "image_url", 
                    ImageUrl = new ImageUrl{Url = $"{BaseUrl}/{filename}"}
                }]
            }
        };

        var result = new StringBuilder();
        await foreach (var token in openRouterService.StreamChatAsync(messages, OpenRouterModel.Gemini3FlashPreview, 0.3))
        {
            result.Append(token);
        }

        return result.ToString();
    }
}

public class FetchDocumentParameters
{
    [JsonPropertyName("filename")]
    [Parameter("The filename to fetch (e.g., 'index.md').")]
    public string Filename { get; set; } = string.Empty;
}

public class FetchDocumentResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("isImage")]
    public bool IsImage { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("base64Content")]
    public string? Base64Content { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}