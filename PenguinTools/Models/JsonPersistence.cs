using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PenguinTools.Models;

public class ModelJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    private static readonly DefaultJsonTypeInfoResolver DefaultResolver = new();

    public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = DefaultResolver.GetTypeInfo(type, options);

        if (typeof(ObservableValidator).IsAssignableFrom(type))
        {
            var hasError = info.Properties.FirstOrDefault(p =>
                string.Equals(p.Name, nameof(ObservableValidator.HasErrors), StringComparison.OrdinalIgnoreCase));
            if (hasError != null) info.Properties.Remove(hasError);
        }

        return info;
    }
}

public static class JsonPersistence
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new ModelJsonTypeInfoResolver()
    };

    public static async Task LoadIntoAsync(object target, string directory, string jsonFileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(directory, jsonFileName);
        if (!File.Exists(path)) return;

        await using var stream = File.OpenRead(path);
        try
        {
            var type = target.GetType();
            var obj = await JsonSerializer.DeserializeAsync(stream, type, Options, cancellationToken);
            if (obj is null) return;

            var properties = type.GetProperties()
                .Where(p => p is { CanRead: true, CanWrite: true })
                .Where(p => p.GetMethod?.IsStatic == false);
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(obj);
                    property.SetValue(target, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load JSON from '{path}'.", ex);
        }
    }

    public static async Task SaveFromAsync(object source, string directory, string jsonFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentNullException(nameof(directory));
        var path = Path.Combine(directory, jsonFileName);
        await using var stream = File.Create(path);
        var type = source.GetType();
        await JsonSerializer.SerializeAsync(stream, source, type, Options, cancellationToken);
    }
}
