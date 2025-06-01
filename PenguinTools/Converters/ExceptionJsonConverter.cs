using System.Text.Json;
using System.Text.Json.Serialization;

namespace PenguinTools.Converters;

public class ExceptionJsonConverter<TExceptionType> : JsonConverter<TExceptionType> where TExceptionType : Exception
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
    {
        var properties = value.GetType().GetProperties().Select(uu => new { uu.Name, Value = uu.GetValue(value) }).Where(uu => uu.Name != nameof(Exception.TargetSite));
        if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull) properties = properties.Where(uu => uu.Value != null);
        var props = properties.ToList();
        if (props.Count == 0) return;

        writer.WriteStartObject();
        foreach (var prop in props)
        {
            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, prop.Value, options);
        }
        writer.WriteEndObject();
    }
}