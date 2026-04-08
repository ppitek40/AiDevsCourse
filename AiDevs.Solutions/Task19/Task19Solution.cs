using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task19;

/// <summary>
/// Solution for Task 19 - Agent session analyzing Natan's trade notes to answer questions
/// </summary>
public class Task19Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 19;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 19 — loading Natan's notes...");

        var notesBasePath = Path.Combine(AppContext.BaseDirectory,
            "../../../../AiDevs.Solutions/Task19/natan_notes");

        var readme = await File.ReadAllTextAsync(
            Path.Combine(notesBasePath, "README.md"), cancellationToken);
        var ogloszenia = await File.ReadAllTextAsync(
            Path.Combine(notesBasePath, "ogłoszenia.txt"), cancellationToken);
        var rozmowy = await File.ReadAllTextAsync(
            Path.Combine(notesBasePath, "rozmowy.txt"), cancellationToken);
        var transakcje = await File.ReadAllTextAsync(
            Path.Combine(notesBasePath, "transakcje.txt"), cancellationToken);

        yield return StreamUpdate.Status("Notes loaded, starting agent session...");

        var systemPrompt = $$"""
            Jesteś agentem odpowiedzialnym za logiczne uporządkowanie notatek Natana Ramsa w wirtualnym systemie plików.
            Natan Rams to koordynator z Domatowa, który zarządza zaopatrzeniem i handlem między miastami.

            ## Twoje zadanie
            Na podstawie notatek Natana musisz zbudować następującą strukturę katalogów w wirtualnym systemie plików:

            ### /miasta/
            Każde miasto opisywane przez Natana musi mieć własny plik (np. `/miasta/Opalino`).
            Zawartość pliku to JSON z towarami potrzebnymi przez to miasto i ich ilościami (bez jednostek).
            Przykład: {"chleb": 45, "woda": 120, "mlotki": 6}

            ### /osoby/
            Każda osoba odpowiadająca za handel w danym mieście musi mieć własny plik (np. `/osoby/Iga_Kapecka`).
            Zawartość pliku to imię i nazwisko osoby oraz link w formacie markdown do katalogu miasta, którym zarządza.
            Przykład:
            Iga Kapecka
            [Opalino](/miasta/Opalino)

            ### /towary/
            Każdy towar wystawiony na sprzedaż musi mieć własny plik (np. `/towary/chleb`).
            Nazwa pliku to mianownik liczby pojedynczej towaru (np. "kilof", nie "kilofy").
            Zawartość pliku to link w formacie markdown do miasta, które ten towar oferuje.
            Przykład: [Domatowo](/miasta/Domatowo)

            ## Ważne zasady
            - W nazwach plików i katalogów NIE używamy polskich znaków (zamiast ą,ę,ó,ś,ź,ż,ć,ń,ł używamy a,e,o,s,z,z,c,n,l).
            - W treści plików JSON również NIE używamy polskich znaków.
            - Nazwy miast w plikach i linkach piszemy wielką literą (np. Opalino, Domatowo).
            - Nazwy towarów w linkach markdownowych i nazwach plików piszemy małą literą.

            ## Dostępne narzędzia (funkcje API)
            Masz do dyspozycji funkcję do zarządzania wirtualnym systemem plików. Użyj jej z {"action": "help"}, aby:
            dostać listę dostępnych komend.
            
            Możesz wywołać `reset`, aby wyczyścić cały filesystem i zacząć od nowa (jeśli coś pójdzie nie tak).
            Możesz wywołać `done`, gdy cała struktura jest gotowa — to wysyła dane do weryfikacji.

            ## Notatki Natana

            ### Opis plików (README.md)
            {{readme}}

            ### Ogłoszenia — zapotrzebowanie miast (ogłoszenia.txt)
            {{ogloszenia}}

            ### Rozmowy — dziennik Natana (rozmowy.txt)
            {{rozmowy}}

            ### Transakcje — które miasto sprzedało co innemu miastu (transakcje.txt)
            {{transakcje}}

            ## Jak postępować
            1. Przeanalizuj notatki i wyodrębnij: miasta z zapotrzebowaniem, osoby zarządzające miastami, towary wystawione na sprzedaż.
            2. Stwórz trzy katalogi: /miasta, /osoby, /towary.
            3. Wypełnij każdy katalog odpowiednimi plikami zgodnie z wymaganiami.
            4. Sprawdź strukturę przed wywołaniem `done`.
            5. Wywołaj `done`, gdy jesteś pewny poprawności struktury.
            6. Jeśli API zwróci flagę w formacie {FLG:...}, jest to końcowy wynik zadania.
            """;

        var userPrompt = """
            Przeanalizuj notatki Natana i zbuduj wymaganą strukturę katalogów w wirtualnym systemie plików.
            Gdy struktura będzie gotowa, wywołaj funkcję `done`, aby przesłać ją do weryfikacji.
            """;

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(SendCommandTask19Function)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 30,
            cancellationToken: cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                answer = update.FinalResult.Output;
        }

        if (answer == null)
        {
            yield return StreamUpdate.Complete(SolutionResult.Fail("Agent did not produce an answer"));
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Ok(answer));
    }
}
