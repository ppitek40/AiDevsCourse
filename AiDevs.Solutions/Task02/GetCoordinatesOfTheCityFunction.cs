using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;

namespace AiDevs.Solutions.Task02;

[FunctionDefinition("get_exact_location_of_the_city", "Get the exact location from a city name")]
public class GetCoordinatesOfTheCityFunction(IHttpClientFactory httpClientFactory) : IFunctionHandler
{
    public Type ParametersType => typeof(GetCoordinatesOfTheCityParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not GetCoordinatesOfTheCityParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var httpClient = httpClientFactory.CreateClient();
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(p.City)}&count=1&language=en&format=json";
        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = "Failed to fetch coordinates" });

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GetCoordinatesOfTheCityResponse>(json);

        if (result == null || result.Results == null)
            return JsonSerializer.Serialize(new { error = "No coordinates found" });
        
        var coordinates = result.Results.FirstOrDefault();
        if (coordinates == null)
            return JsonSerializer.Serialize(new { error = "No coordinates found" });

        return JsonSerializer.Serialize(new { latitude = coordinates.Latitude.ToString("F3"), longitude = coordinates.Longitude.ToString("F3") });
    }    
}

public record GetCoordinatesOfTheCityParameters
{
    [JsonPropertyName("city")]
    [Parameter("City name (e.g., 'Warsaw')")]
    public string City { get; init; }
}

public record GetCoordinatesOfTheCityResponse
{
    [JsonPropertyName("results")]
    public List<GetCoordinatesOfTheCityResult> Results { get; init; }
}

public record GetCoordinatesOfTheCityResult
{
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; init; }
    [JsonPropertyName("longitude")]
    public decimal Longitude { get; init; }
}
