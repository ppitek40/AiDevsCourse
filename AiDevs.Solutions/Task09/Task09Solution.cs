using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task09;

/// <summary>
/// Solution for Task 09 - Search the email inbox using the zmail API to find specific data
/// </summary>
public class Task09Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 9;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 09 — email search...");

        const string systemPrompt = @"You are an email investigation agent with access to the mailbox.

## Your Goal
Find specific data requested by the user by searching through emails in the inbox.

## Workflow
1. Start with calling the mailbox with 'action'='help' to get the complete API documentation
2. Start by searching for the topics that are relevant to the task
3. Extract messages from matching topics
4. Extract the requested information from the email content
5. If the first search yields no results, try alternative keywords or broader queries
6. Once you have found the answer, return it clearly and concisely

## Rules
- Start with broad search queries and then try to narrow down
- Do not try to guess or make assumptions without evidence
- the mailbox is active, so if you don't find anything, try again later
- Read full email content before drawing conclusions
- If a search returns multiple results, check each relevant one
- Be thorough — try multiple search strategies if needed
- Return the final answer as plain text or JSON depending on the task requirements";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Search the email inbox for three informations: 1. Hasło do systemu pracowniczego, 2. Data kiedy dział bezpieczeństwa planuje atak na elektrownię, 3. Kod potwierdzenie z ticketa wsyłanego przez dział bezpieczeństwa w formacie SEC- +32 znaki. Zwróć szczególną uwagę na maile z domeny \"proton.me\". Gotowe odpowiedzi wyślij przy użyciu funkcji mailbox_verify." }
        };

        yield return StreamUpdate.Status("Starting agent session with email search tools...");

        string? agentAnswer = null;

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(SearchEmailFunction), typeof(MailboxVerifyFunction)], 
            model: OpenRouterModel.Gemini3FlashPreview,
            temperature: 0,
            maxIterations: 20,
            cancellationToken: cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                agentAnswer = update.FinalResult.Output;
        }

        if (agentAnswer == null)
        {
            yield return StreamUpdate.Complete(SolutionResult.Fail("Agent did not produce a result"));
            yield break;
        }

        yield return StreamUpdate.Status($"Agent found answer: {agentAnswer}");

        yield return StreamUpdate.Complete(SolutionResult.Ok(agentAnswer));
    }
}
