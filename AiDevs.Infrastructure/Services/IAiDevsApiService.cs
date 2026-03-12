using System.Text.Json.Serialization;
using AiDevs.Core.Models;

namespace AiDevs.Infrastructure.Services;

/// <summary>
/// Service for interacting with AiDevs Hub API
/// </summary>
public interface IAiDevsApiService
{
    /// <summary>
    /// Get locations where a person was seen
    /// </summary>
    Task<List<Coordinate>> GetLocationAsync(string name, string surname, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get access level for a person
    /// </summary>
    Task<int> GetAccessLevelAsync(string name, string surname, int birthYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit answer to verify endpoint
    /// </summary>
    Task<SolutionResult> VerifyAsync(string task, object answer, CancellationToken cancellationToken = default);

    Task<AiDevsApiService.StatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check package status and location
    /// </summary>
    Task<string> CheckPackageAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Redirect a package to a new destination
    /// </summary>
    Task<string> RedirectPackageAsync(string packageId, string destination, string code, CancellationToken cancellationToken = default);
}

public class Coordinate
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
