using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using AiDevs.Tools;

namespace AiDevs.Solutions.Task04;

/// <summary>
/// Solution for Task 04 - Declaration form completion for Gdańsk-Żarnowiec package
/// </summary>
public class Task04Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 4;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 04: Declaration form completion...");

        var systemPrompt =
            @"Jesteś specjalistą ds. wypełniania deklaracji transportowych w Systemie Przesyłek Konduktorskich (SPK).

Twoje zadanie:
1. Pobierz CAŁĄ dokumentację SPK, zaczynając od 'index.md' - będzie zawierał odnośniki do innych plików
2. Przeczytaj WSZYSTKIE pliki dokumentacji (zarówno tekstowe jak i graficzne) - odpowiedzi mogą być w różnych załącznikach
3. Zwróć szczególną uwagę na:
   - Wzór deklaracji (musi być zachowany DOKŁADNIE taki sam format)
   - Kategorie przesyłek i przypisane im opłaty
   - Skróty używane w dokumentacji
   - Grafiki - mogą zawierać kluczowe informacje
4. Wypełnij deklarację dla przesyłki zgodnie z podanymi danymi, zachowując DOKŁADNIE format ze wzoru
5. Zwróć TYLKO gotową deklarację, bez żadnych dodatkowych komentarzy

KRYTYCZNE WYMAGANIA:
- Wartości wpisuj zamiast nawiasów kwadratowych []. Nie może być żadnych nawiasów kwadratowych w deklaracji
- Przesyłka musi być DARMOWA lub opłacana przez System (budżet zerowy!)
- Trasa: Gdańsk → Żarnowiec
- NIE dodawaj żadnych uwag specjalnych (są zawsze weryfikowane ręcznie)
- Upewnij się, że przesyłka nie jest za ciężka i nie potrzebuje dodatkowego wagonu. NIE DODAWAJ ICH ZA DUŻO!
- Format deklaracji musi być IDENTYCZNY jak we wzorze (hub weryfikuje format!)
- Wymagana trasa może być zamknięta, jest to OK

Zwróć TYLKO gotową deklarację, bez żadnych dodatkowych komentarzy.
";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new()
            {
                Role = "user",
                Content =
                    "Fetch and analyze all documentation, then complete the declaration form for the Gdańsk-Żarnowiec package. Nadawca to '450202122', Waga to 2.8 tony. Zawartość to 'kasety z paliwem do reaktora'. Nie dodawaj żadnych uwag specjalnych"
            }
        };

        yield return StreamUpdate.Status("Starting agent session with documentation access...");

        string? declaration = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(FetchDocumentFunction)],
            OpenRouterModel.Gemini25Flash,
            0.4,
            30,
            cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                declaration = update.FinalResult.Output;
        }

        if (declaration != null)
        {
            yield return StreamUpdate.Status("Submitting declaration to verification...");
            var declarationObj = new DeclarationResponse { Declaration = ResponseStripper.Strip(declaration) };

            // Submit to verify endpoint
            var verifyResponse = await aiDevsApiService.VerifyAsync("sendit", declarationObj, cancellationToken);
            yield return StreamUpdate.Complete(verifyResponse);
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Failed to generate declaration"));
    }

    private class DeclarationResponse
    {
        [JsonPropertyName("declaration")]
        public required string Declaration { get; init; }
    }
}