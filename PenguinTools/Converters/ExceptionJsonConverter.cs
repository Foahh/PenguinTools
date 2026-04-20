using System.Text.Json;
using System.Text.Json.Serialization;
using PenguinTools.Core;

namespace PenguinTools.Converters;

public class ExceptionJsonConverter : JsonConverter<Exception>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Type", value.GetType().FullName);
        writer.WriteString("Message", value.Message);
        writer.WriteString("StackTrace", value.StackTrace);
        writer.WriteString("Source", value.Source);

        if (value is DiagnosticException dEx)
        {
            var diagnostic = dEx.ToDiagnostic();

            writer.WritePropertyName(nameof(DiagnosticException.Target));
            JsonSerializer.Serialize(writer, dEx.Target, options);

            writer.WriteString("Path", diagnostic.Path);

            writer.WriteString("Tick", diagnostic.Time?.ToString());

            writer.WriteString("Line", diagnostic.Line?.ToString());

            writer.WritePropertyName(nameof(DiagnosticException.TimeCalculator));
            JsonSerializer.Serialize(writer, (object?)dEx.TimeCalculator, options);
        }

        if (value.InnerException != null)
        {
            writer.WritePropertyName("InnerException");
            JsonSerializer.Serialize(writer, value.InnerException, options);
        }

        writer.WriteEndObject();
    }
}
