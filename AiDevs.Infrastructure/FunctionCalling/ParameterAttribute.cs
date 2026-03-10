namespace AiDevs.Infrastructure.FunctionCalling;

[AttributeUsage(AttributeTargets.Property)]
public class ParameterAttribute : Attribute
{
    public string Description { get; }
    public bool Required { get; }

    public ParameterAttribute(string description, bool required = true)
    {
        Description = description;
        Required = required;
    }
}
