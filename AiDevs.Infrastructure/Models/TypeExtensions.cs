using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AiDevs.Infrastructure.Models;

public static class TypeExtensions
{
    public static ResponseFormat ToResponseFormat(this Type type)
    {
        var properties = new Dictionary<string, ResponseProperty>();
        var required = new List<string>();

        foreach (var property in type.GetProperties())
        {
            var propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            properties[propertyName] = new ResponseProperty
            {
                Type = GetJsonType(property.PropertyType),
                Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name
            };

            if (!IsNullable(property.PropertyType))
            {
                required.Add(propertyName);
            }
        }

        return new ResponseFormat
        {
            Type = "json_schema",
            JsonSchema = new JsonSchema
            {
                Name = type.Name,
                Strict = true,
                Schema = new Schema
                {
                    Type = "object",
                    Properties = properties,
                    Required = required.Count > 0 ? required : null,
                    AdditionalProperties = false
                }
            }
        };
    }
    private static string GetJsonType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(short) || underlyingType == typeof(byte))
            return "integer";
        if (underlyingType == typeof(float) || underlyingType == typeof(double) ||
            underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType.IsArray || (underlyingType.IsGenericType &&
            typeof(IEnumerable<>).IsAssignableFrom(underlyingType.GetGenericTypeDefinition())))
            return "array";

        return "object";
    }

    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType)
            return true;

        return Nullable.GetUnderlyingType(type) != null;
    }
}