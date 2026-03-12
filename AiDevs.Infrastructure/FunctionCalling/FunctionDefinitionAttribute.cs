namespace AiDevs.Infrastructure.FunctionCalling;

[AttributeUsage(AttributeTargets.Class)]
public class FunctionDefinitionAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}
