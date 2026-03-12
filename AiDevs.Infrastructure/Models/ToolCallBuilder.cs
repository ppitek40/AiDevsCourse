using System.Text;

namespace AiDevs.Infrastructure.Models;

public class ToolCallBuilder
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public StringBuilder Arguments { get; set; } = new();
}