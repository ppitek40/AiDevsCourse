using System.Reflection;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Infrastructure.FunctionCalling;

public static class FunctionExtensions
{
    public static OpenRouterTool? BuildToolFromHandler(this IFunctionHandler handler)  
    {
        var handlerType = handler.GetType();

        var functionAttr = handlerType.GetCustomAttribute<FunctionDefinitionAttribute>();
        if (functionAttr == null)
            return null;

        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in handler.ParametersType.GetProperties())
        {
            var paramAttr = prop.GetCustomAttribute<ParameterAttribute>();
            if (paramAttr == null)
                continue;

            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name.ToLower();

            properties[jsonName] = new
            {
                type = GetJsonType(prop.PropertyType),
                description = paramAttr.Description
            };

            if (paramAttr.Required)
                required.Add(jsonName);
        }

        return new OpenRouterTool
        {
            Type = "function",
            Function = new OpenRouterFunction
            {
                Name = functionAttr.Name,
                Description = functionAttr.Description,
                Parameters = new
                {
                    type = "object",
                    properties,
                    required = required.ToArray()
                }
            },
            Handler = handler
        };
    }


    private static string GetJsonType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long))
            return "integer";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";

        return "string";
    }
}