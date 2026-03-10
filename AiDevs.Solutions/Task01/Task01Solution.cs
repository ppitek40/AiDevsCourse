using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task01;

/// <summary>
/// Task 01: Filter people and tag them based on job descriptions
/// </summary>
public class Task01Solution(IOpenRouterService openRouterService, IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 1;

    public async Task<SolutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read and parse CSV file
            var csvPath = Path.Combine(AppContext.BaseDirectory, "../../../../AiDevs.Solutions/Task01/people.csv");
            var people = await CsvParser.ParseCsvAsync(csvPath, cancellationToken);

            // Filter people by criteria
            var filteredPeople = people
                .Where(p => p.MeetsTransportCriteria())
                .ToList();

            Console.WriteLine($"Found {filteredPeople.Count} people meeting basic criteria");

            // Tag people using LLM
            await TagPeopleAsync(filteredPeople, cancellationToken);

            // Filter only those with transport tag
            var transportPeople = filteredPeople
                .Where(p => p.Tags.Contains("transport"))
                .ToList();

            // Save transportPeople to result.json
            var resultPath = Path.Combine(AppContext.BaseDirectory, "../../../../AiDevs.Solutions/Task01/result.json");
            await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(transportPeople), cancellationToken);
            Console.WriteLine($"Transport people saved to {resultPath}");

            Console.WriteLine($"Found {transportPeople.Count} people with transport tag");

            // Prepare payload
            var answer = transportPeople.Select(p => new
            {
                name = p.Name,
                surname = p.Surname,
                gender = p.Gender,
                born = p.BirthYear,
                city = p.BirthPlace,
                tags = p.Tags.ToArray()
            }).ToArray();

            var result = await aiDevsApiService.VerifyAsync("people", answer, cancellationToken);

            return SolutionResult.Ok(result, new Dictionary<string, object>
            {
                { "totalPeople", people.Count },
                { "filteredByBasicCriteria", filteredPeople.Count },
                { "transportPeople", transportPeople.Count }
            });
        }
        catch (Exception ex)
        {
            return SolutionResult.Fail($"Task 01 failed: {ex.Message}");
        }
    }


    private async Task TagPeopleAsync(List<Person> people, CancellationToken cancellationToken)
    {
        // Process in batches to avoid overwhelming the API
        const int batchSize = 10;
        for (int i = 0; i < people.Count; i += batchSize)
        {
            var batch = people.Skip(i).Take(batchSize).ToList();
            await TagBatchAsync(batch, cancellationToken);
            Console.WriteLine($"Tagged {Math.Min(i + batchSize, people.Count)}/{people.Count} people");
        }
    }

    private async Task TagBatchAsync(List<Person> batch, CancellationToken cancellationToken)
    {
        var jobDescriptions = string.Join("\n\n", batch.Select((p, idx) =>
            $"{idx}. {p.Name} {p.Surname}: {p.Job}"));

        var prompt = $@"Przeanalizuj poniższe opisy pracy i przypisz odpowiednie tagi dla każdej osoby.

Dostępne tagi:
- IT (programowanie, rozwój oprogramowania, bazy danych, sieci komputerowe)
- transport (logistyka, transport, dostawy, kierowcy, mechanika pojazdów)
- edukacja (nauczyciele, wykładowcy, szkolenia)
- medycyna (lekarze, pielęgniarki, farmaceuci, badania medyczne)
- praca z ludźmi (obsługa klienta, doradztwo, pomoc społeczna)
- praca z pojazdami (mechanika, naprawy, serwis pojazdów)
- praca fizyczna (budownictwo, rzemiosło, montaż)

Jedna osoba może mieć wiele tagów. Zwróć TYLKO JSON array w formacie:
[
  {{""index"": 0, ""tags"": [""tag1"", ""tag2""]}},
  {{""index"": 1, ""tags"": [""tag3""]}}
]

Opisy:
{jobDescriptions}";

        var response = await openRouterService.CompleteAsync(
            prompt,
            model: OpenRouterModel.Gpt4o,
            temperature: 0.3,
            cancellationToken: cancellationToken
        );

        // Parse response and assign tags
        var cleanResponse = response.Trim();
        if (cleanResponse.StartsWith("```json"))
        {
            cleanResponse = cleanResponse.Substring(7);
        }
        if (cleanResponse.StartsWith("```"))
        {
            cleanResponse = cleanResponse.Substring(3);
        }
        if (cleanResponse.EndsWith("```"))
        {
            cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
        }
        cleanResponse = cleanResponse.Trim();

        var tagResults = JsonSerializer.Deserialize<List<TagResult>>(cleanResponse);
        if (tagResults != null)
        {
            foreach (var result in tagResults)
            {
                if (result.Index >= 0 && result.Index < batch.Count)
                {
                    batch[result.Index].Tags = result.Tags;
                }
            }
        }
    }

    private class TagResult
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }  

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }
}