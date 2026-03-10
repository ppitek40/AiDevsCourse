namespace AiDevs.Infrastructure.FunctionCalling;

[AttributeUsage(AttributeTargets.Class)]
public class FunctionDefinitionAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

    public FunctionDefinitionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
