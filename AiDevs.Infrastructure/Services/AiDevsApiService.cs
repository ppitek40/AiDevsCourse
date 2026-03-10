using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Infrastructure.Services;

public class AiDevsApiService : IAiDevsApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://hub.ag3nts.org/api";

    public AiDevsApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AiDevs:ApiKey"]
            ?? throw new InvalidOperationException("AiDevs API key not configured");
    }

    public async Task<List<Coordinate>> GetLocationAsync(string name, string surname, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            name,
            surname
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/location", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var locationResponse = JsonSerializer.Deserialize<List<Coordinate>>(responseJson);

        return locationResponse ?? new List<Coordinate>();
    }

    public async Task<int> GetAccessLevelAsync(string name, string surname, int birthYear, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            name,
            surname,
            birthYear
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/accesslevel", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var accessResponse = JsonSerializer.Deserialize<AccessLevelResponse>(responseJson);

        return accessResponse?.AccessLevel ?? 0;
    }

    public async Task<string> VerifyAsync(string task, object answer, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            task,
            answer
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/../verify", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private class LocationResponse
    {
        [JsonPropertyName("locations")]
        public List<Coordinate>? Locations { get; set; }
    }

    private class AccessLevelResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; init; }
        [JsonPropertyName("surname")]
        public string Surname { get; init; }
        [JsonPropertyName("accessLevel")]
        public int AccessLevel { get; set; }
    }
}
