using System.Text.Json.Serialization;

namespace PenguinTools.Common.Asset;

public enum AssetType
{
    [JsonStringEnumMemberName("genreNames")]
    GenreNames,
    [JsonStringEnumMemberName("notesFieldLine")]
    FieldLines,
    [JsonStringEnumMemberName("stageName")]
    StageNames,
    [JsonStringEnumMemberName("worldsEndTagName")]
    WeTagNames
}