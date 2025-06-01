using System.Reflection;

namespace PenguinTools.Attributes;

public static class TypeExtensions
{
    public static T? GetPropertyValue<T>(this Type type, string key, T? fallbackValue = null) where T : class
    {
        var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Static);
        if (property == null) return fallbackValue;
        var value = property.GetValue(null, null) as T;
        return value ?? fallbackValue;
    }
}