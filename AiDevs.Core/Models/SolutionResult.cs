namespace AiDevs.Core.Models;

/// <summary>
/// Standard result returned by all task solutions
/// </summary>
public class SolutionResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static SolutionResult Ok(string output, Dictionary<string, object>? metadata = null)
    {
        return new SolutionResult
        {
            Success = true,
            Output = output,
            Metadata = metadata
        };
    }

    public static SolutionResult Fail(string error)
    {
        return new SolutionResult
        {
            Success = false,
            Error = error
        };
    }
}
