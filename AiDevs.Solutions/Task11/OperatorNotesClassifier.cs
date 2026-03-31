using System.Collections.Concurrent;
using System.Text;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task11;

public interface IOperatorNotesClassifier
{
    /// <summary>
    /// Determines if operator notes indicate a problem or anomaly
    /// Returns true if notes indicate something bad is happening
    /// </summary>
    Task<bool> IndicatesProblemAsync(string operatorNotes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Classifies operator notes using LLM with caching for repeated notes
/// </summary>
public class OperatorNotesClassifier(IOpenRouterService openRouterService) : IOperatorNotesClassifier
{
    private readonly ConcurrentDictionary<string, bool> _cache = new();

    /// <summary>
    /// Determines if operator notes indicate a problem or anomaly
    /// Returns true if notes indicate something bad is happening
    /// </summary>
    public async Task<bool> IndicatesProblemAsync(string operatorNotes, CancellationToken cancellationToken = default)
    {
        var trimmedNotes = operatorNotes.Split(',')[0].Replace(" ", "").Trim().ToLower();
        // Check cache first
        if (_cache.TryGetValue(trimmedNotes, out var cachedResult))
        {
            return cachedResult;
        }

        // Classify using LLM
        var systemPrompt = @"You are analyzing operator notes from industrial sensor readings.
Your task is to determine if the notes indicate a problem, anomaly, or concern.

Respond with ONLY one word:
- 'PROBLEM' if the notes mention: anomaly, issue, concern, unusual, uncomfortable, escalate, troubleshooting, not right, risky, unsafe, etc.
- 'NORMAL' if the notes indicate: normal operation, routine, stable, calm, predictable, fits reference, clean, reliable, etc.

Analyze the sentiment and keywords carefully.";

        var messages = new List<IOpenRouterMessage>
        {
            new OpenRouterMessage { Role = "system", Content = systemPrompt },
            new OpenRouterMessage { Role = "user", Content = $"Operator notes: {operatorNotes}" }
        };

        var response = new StringBuilder();
        await foreach (var token in openRouterService.StreamChatAsync(
                           messages,
                           model: OpenRouterModel.Gpt41Nano,
                           temperature: 0,
                           cancellationToken: cancellationToken))
        {
            response.Append(token);
        }

        var result = response.ToString().Trim().ToUpperInvariant();
        var indicatesProblem = result.Contains("PROBLEM");

        // Cache the result
        _cache[trimmedNotes] = indicatesProblem;

        return indicatesProblem;
    }
}
