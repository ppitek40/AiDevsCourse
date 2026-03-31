using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Solutions.Task07;

[FunctionDefinition("get_electricity_diagram", "Get the current and target electricity diagrams from the API")]
public class GetElectricityDiagramFunction(IOpenRouterService openRouterService, IConfiguration configuration) : IFunctionHandler
{
    public Type ParametersType => typeof(GetElectricityDiagramParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not GetElectricityDiagramParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var reset = p.Reset ? "?reset=1" : "";

        var str = new StringBuilder();
        str.AppendLine("Electricity Diagrams");
        str.AppendLine("Current:");
        str.AppendLine(await HandleImage($"https://hub.ag3nts.org/data/{configuration["AiDevs:ApiKey"]}/electricity.png{reset}"));
        str.AppendLine("Target:");
        str.AppendLine(await HandleImage("https://hub.ag3nts.org/i/solved_electricity.png"));

        return JsonSerializer.Serialize(new
        {
            statusCode = 200,
            response = str.ToString()
        });
    }
private async Task<string> HandleImage(string filename)
    {
        var messages = new List<IOpenRouterMessage>
        {
            new OpenRouterMessage { Role = "system", Content = $"Analyze the image and return all the data from this image. Return ONLY the extracted data, do not add any additional comments or explanations. For each information write the position and the exact shape or information in that position." },
            new MultiModalOpenRouterMessage() 
            {
                Role = "user",
                Content = [new MultiModalContent{
                    Type = "text", 
                    Text = "Analyze the image and return all the data from this image. Return ONLY the extracted data, do not add any additional comments or explanations."
                }, new MultiModalContent{
                    Type = "image_url", 
                    ImageUrl = new ImageUrl{Url = $"{filename}"}
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


public class GetElectricityDiagramParameters
{
    [JsonPropertyName("reset")]
    [Parameter("Reset the puzzle to its initial state", required: true)]
    public bool Reset { get; set; }
}
