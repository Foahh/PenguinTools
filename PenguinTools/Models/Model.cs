using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Core.Metadata;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using mg = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Models;

public class ModelJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    private static readonly DefaultJsonTypeInfoResolver DefaultResolver = new();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = DefaultResolver.GetTypeInfo(type, options);

        if (typeof(ObservableValidator).IsAssignableFrom(type))
        {
            var hasError = info.Properties.FirstOrDefault(p => string.Equals(p.Name, nameof(ObservableValidator.HasErrors), StringComparison.OrdinalIgnoreCase));
            if (hasError != null) info.Properties.Remove(hasError);
        }

        return info;
    }
}

public abstract class Model : ObservableValidator
{
    protected virtual string JsonName => throw new InvalidOperationException();

    private static JsonSerializerOptions JsonSerializerOptions => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new ModelJsonTypeInfoResolver()
    };

    // workaround for hiding ObservableValidator's property
    [Browsable(false)]
    [JsonIgnore]
    public new bool HasErrors { get; set; }

    public async Task LoadAsync(string directory, CancellationToken token)
    {
        var path = Path.Combine(directory, JsonName);
        if (!File.Exists(path)) return;

        await using var stream = File.OpenRead(path);
        try
        {
            var type = GetType();
            var obj = await JsonSerializer.DeserializeAsync(stream, type, JsonSerializerOptions, token);
            if (obj == null) return;

            var properties = type.GetProperties().Where(p => p is { CanRead: true, CanWrite: true }).Where(p => p.GetMethod?.IsStatic == false);
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(obj);
                    property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task SaveAsync(string directory, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentNullException(nameof(directory));
        var path = Path.Combine(directory, JsonName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, this, GetType(), JsonSerializerOptions, token);
    }

    protected void SetPropertyReadOnly(string propertyName, bool readOnly)
    {
        var descriptor = TypeDescriptor.GetProperties(GetType())[propertyName];
        if (descriptor == null) throw new ArgumentException($"Property '{propertyName}' not found on type '{GetType().Name}'.");
        if (descriptor.Attributes[typeof(ReadOnlyAttribute)] is not ReadOnlyAttribute attribute) throw new InvalidOperationException($"Property '{propertyName}' does not have a ReadOnlyAttribute.");
        var rf = GetBackingField(typeof(ReadOnlyAttribute).GetProperty(nameof(ReadOnlyAttribute.IsReadOnly))!);
        if (rf == null) throw new InvalidOperationException($"Property '{propertyName}' does not have a backing field.");
        rf.SetValue(attribute, readOnly);
    }

    private static FieldInfo? GetBackingField(PropertyInfo pi)
    {
        if (!pi.CanRead) return null;
        var getMethod = pi.GetGetMethod(true);
        if (getMethod == null || !getMethod.IsDefined(typeof(CompilerGeneratedAttribute), true)) return null;
        var flags = BindingFlags.NonPublic | (getMethod.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
        var bf = pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", flags);
        if (bf == null) return null;
        return bf.IsDefined(typeof(CompilerGeneratedAttribute), true) ? bf : null;
    }
}

public abstract class MetaModel : Model
{
    [Browsable(false)]
    public abstract mg.Chart Mgxc { get; }

    [Browsable(false)]
    public Meta Meta => Mgxc.Meta;
}