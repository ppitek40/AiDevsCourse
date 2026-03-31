using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiDevs.Solutions.Task10;

[FunctionDefinition("read_drone_image",
    "Read and analyze an image from the drone camera and return the position of the dam")]
public class ReadDroneImageFunction(
    IOpenRouterService openRouterService,
    IConfiguration configuration,
    ILogger<ReadDroneImageFunction> logger) : IFunctionHandler
{
    public Type ParametersType => typeof(ReadDroneImageParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not ReadDroneImageParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var imageUrl = $"https://hub.ag3nts.org/data/{configuration["AiDevs:ApiKey"]}/drone.png";

        logger.LogInformation("Image URL: {ImageUrl}", imageUrl);
        var messages = new List<IOpenRouterMessage>
        {
            new OpenRouterMessage { Role = "system", Content = "" },
            new MultiModalOpenRouterMessage
            {
                Role = "user",
                Content =
                [
                    new MultiModalContent
                    {
                        Type = "text",
                        Text =
                            "Analyze the image and return the position of the dam in the image. Image is divided into grid. Left top corner is 1x1. Return ONLY the position of the dam in the format rowxcol, for example 2x3."
                    },
                    new MultiModalContent
                    {
                        Type = "image_url",
                        ImageUrl = new ImageUrl { Url = imageUrl }
                    }
                ]
            }
        };

        var result = new StringBuilder();
        await foreach (var token in openRouterService.StreamChatAsync(messages, OpenRouterModel.Gemini3FlashPreview,
            0.3, cancellationToken: cancellationToken))
        {
            result.Append(token);
        }

        return JsonSerializer.Serialize(new
        {
            statusCode = 200,
            response = result.ToString()
        });
    }
}

public class ReadDroneImageParameters
{
    [JsonPropertyName("target")]
    [Parameter("Target to find in the image", true)]
    public string Target { get; set; }
}