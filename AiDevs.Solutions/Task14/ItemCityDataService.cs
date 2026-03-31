namespace AiDevs.Solutions.Task14;

public interface IItemCityDataService
{
    IReadOnlyList<string> GetCitiesForItem(string itemCode);
    IReadOnlyList<SearchResult> SearchItems(string word);
}

public class ItemCityDataService : IItemCityDataService
{
    private static readonly string DataDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "AiDevs.Solutions", "Task14");

    private readonly Lazy<Dictionary<string, string>> _itemNameToCode = new(BuildItemNameToCode);
    private readonly Lazy<Dictionary<string, List<string>>> _itemCodeToCities = new(BuildItemCodeToCities);

    public IReadOnlyList<SearchResult> SearchItems(string word)
    {
        return _itemNameToCode.Value.Where(kvp => kvp.Value.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new SearchResult { ItemName = kvp.Value, ItemCode = kvp.Key }).ToList();
    }

    public IReadOnlyList<string> GetCitiesForItem(string itemCode)
    {
        return _itemCodeToCities.Value[itemCode];
    }

    private static Dictionary<string, List<string>> BuildItemCodeToCities()
    {
        var cities = ReadCsv(Path.Combine(DataDir, "cities.csv"))
            .ToDictionary(r => r["code"], r => r["name"]);

        var connections = ReadCsv(Path.Combine(DataDir, "connections.csv"));

        var itemCodeToCities = new Dictionary<string, List<string>>();
        foreach (var conn in connections)
        {
            if (!cities.TryGetValue(conn["cityCode"], out var cityName)) continue;

            if (!itemCodeToCities.TryGetValue(conn["itemCode"], out var list))
            {
                list = [];
                itemCodeToCities[conn["itemCode"]] = list;
            }

            list.Add(cityName);
        }

        return itemCodeToCities;
    }

    private static Dictionary<string, string> BuildItemNameToCode()
    {
        var items = ReadCsv(Path.Combine(DataDir, "items.csv"))
            .ToDictionary(r => r["code"], r => r["name"]);
        return items;
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return [];

        var headers = lines[0].Split(',');
        var rows = new List<Dictionary<string, string>>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(',');
            var row = new Dictionary<string, string>();
            for (var i = 0; i < headers.Length && i < values.Length; i++)
                row[headers[i].Trim()] = values[i].Trim();
            rows.Add(row);
        }

        return rows;
    }
}

public class SearchResult
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
}
