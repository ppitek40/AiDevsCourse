namespace AiDevs.Solutions.Task01;

public class Person
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public string BirthCountry { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();

    public int BirthYear => BirthDate.Year;

    public bool MeetsTransportCriteria()
    {
        // Men aged 20-40 in 2026 (born 1986-2006)
        // Born in Grudziądz
        return Gender == "M"
               && BirthYear >= 1986 && BirthYear <= 2006
               && BirthPlace.Equals("Grudziądz", StringComparison.OrdinalIgnoreCase);
    }
}
