using System.Globalization;
using System.Text;

namespace AiDevs.Solutions.Task01;

public static class CsvParser
{
    public static async Task<List<Person>> ParseCsvAsync(string filePath, CancellationToken cancellationToken)
    {
        var people = new List<Person>();
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

        // Skip header
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var parts = ParseCsvLine(line);

            if (parts.Length >= 7)
            {
                var person = new Person
                {
                    Name = parts[0],
                    Surname = parts[1],
                    Gender = parts[2],
                    BirthDate = DateTime.ParseExact(parts[3], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    BirthPlace = parts[4],
                    BirthCountry = parts[5],
                    Job = parts[6]
                };
                people.Add(person);
            }
        }

        return people;
    }

    private static string[] ParseCsvLine(string line)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        parts.Add(current.ToString());

        return parts.ToArray();
    }
}