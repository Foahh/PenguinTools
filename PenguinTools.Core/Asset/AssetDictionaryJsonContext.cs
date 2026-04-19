using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PenguinTools.Core.Asset;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<AssetType, SortedSet<Entry>>), TypeInfoPropertyName = nameof(Database))]
internal sealed partial class AssetDictionaryJsonContext : JsonSerializerContext
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly AssetDictionaryJsonContext DefaultContext = new(SerializerOptions);
}
