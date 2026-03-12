namespace AiDevs.Infrastructure.FunctionCalling;

[AttributeUsage(AttributeTargets.Property)]
public class ParameterAttribute(string description, bool required = true) : Attribute
{
    public string Description { get; } = description;
    public bool Required { get; } = required;
}
