namespace AiDevs.Tools;

public static class ResponseStripper
{
    public static string Strip(string response)
    {
        if (response.StartsWith("```json"))
            response = response.Substring(7);
        if (response.StartsWith("```"))
            response = response.Substring(3);
        if (response.EndsWith("```"))
            response = response.Substring(0, response.Length - 3);
        return response.Trim();
    }
}