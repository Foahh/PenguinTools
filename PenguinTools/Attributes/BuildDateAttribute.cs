// ReSharper disable CheckNamespace

#pragma warning disable CA1050

using System.Globalization;
using System.Reflection;

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildDateAttribute(string value) : Attribute
{
    public DateTime DateTime { get; } = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);

    public static DateTime GetAssemblyBuildDate()
    {
        var assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Failed to retrieve entry assembly");
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime ?? default;
    }
}