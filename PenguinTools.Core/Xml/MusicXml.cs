﻿using PenguinTools.Common.Asset;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using System.Xml.Serialization;

namespace PenguinTools.Common.Xml;

[XmlRoot("MusicData")]
public sealed class MusicXml : XmlElement<MusicXml>
{
    private static readonly Dictionary<Difficulty, Entry> DiffMap = new()
    {
        [Difficulty.Basic] = new Entry(0, "Basic", "BASIC"),
        [Difficulty.Advanced] = new Entry(1, "Advanced", "ADVANCED"),
        [Difficulty.Expert] = new Entry(2, "Expert", "EXPERT"),
        [Difficulty.Master] = new Entry(3, "Master", "MASTER"),
        [Difficulty.Ultima] = new Entry(4, "Ultima", "ULTIMA"),
        [Difficulty.WorldsEnd] = new Entry(5, "WorldsEnd", "WORLD'S END")
    };

    internal MusicXml()
    {
    }

    public MusicXml(Dictionary<Difficulty, Meta> metaMap, Difficulty mainDiff)
    {
        var mainMeta = metaMap[mainDiff] ?? throw new DiagnosticException("Main meta is null");
        var id = mainMeta.Id ?? throw new DiagnosticException(Strings.Error_song_id_is_not_set);

        DataName = $"music{id:0000}";
        ExType = mainMeta.Difficulty == Difficulty.WorldsEnd ? 2 : 0;
        Name = new Entry(id, mainMeta.Title);
        SortName = mainMeta.SortName;
        ArtistName = new Entry(id, mainMeta.Artist);
        GenreNames = new List<Entry> { mainMeta.Genre };
        JaketFile = $"CHU_UI_Jacket_{id:0000}.dds";
        EnableUltima = mainMeta.Difficulty == Difficulty.Ultima;
        ReleaseDate = mainMeta.ReleaseDate.ToString("yyyyMMdd");
        CueFileName = new Entry(id, $"music{id:0000}");
        WorldsEndTagName = mainMeta.WeTag;
        StarDifType = (int)mainMeta.WeDifficulty;
        StageName = mainMeta.Stage;

        foreach (var diff in Enum.GetValues<Difficulty>())
        {
            metaMap.TryGetValue(diff, out var meta);
            var (whole, frac) = meta != null ? SplitLevel(meta.Level) : (0, 0);

            var fumen = new MusicFumenData
            {
                Type = DiffMap[diff],
                Enable = meta != null,
                File = $"{id:0000}_{(int)diff:00}.c2s",
                Level = whole,
                LevelDecimal = frac
            };

            Fumens.Add(fumen);
        }
    }

    protected override string FileName => "Music.xml";

    public MusicFumenData this[Difficulty diff] => Fumens.First(f => f.Type == DiffMap[diff]);

    [XmlElement("releaseTagName")]
    public Entry ReleaseTagName { get; set; } = ReleaseTag.Default.Name;

    [XmlElement("netOpenName")]
    public Entry NetOpenName { get; set; } = XmlConstants.NetOpenName;

    [XmlElement("disableFlag")]
    public bool DisableFlag { get; set; }

    [XmlElement("exType")]
    public int ExType { get; set; }

    [XmlElement("name")]
    public Entry Name { get; set; } = Entry.Default;

    [XmlElement("sortName")]
    public string SortName { get; set; } = string.Empty;

    [XmlElement("artistName")]
    public Entry ArtistName { get; set; } = Entry.Default;

    [XmlElement("genreNames")]
    public EntryCollection GenreNames { get; set; } = new List<Entry> { Entry.Default };

    [XmlElement("worksName")]
    public Entry WorksName { get; set; } = Entry.Default;

    [XmlElement("labelName")]
    public Entry LabelName { get; set; } = Entry.Default;

    [XmlElement("jaketFile")]
    public PathElement JaketFile { get; set; } = new();

    [XmlElement("firstLock")]
    public bool FirstLock { get; set; }

    [XmlElement("enableUltima")]
    public bool EnableUltima { get; set; }

    [XmlElement("isGiftMusic")]
    public bool IsGiftMusic { get; set; }

    [XmlElement("releaseDate")]
    public string ReleaseDate { get; set; } = string.Empty;

    [XmlElement("priority")]
    public int Priority { get; set; }

    [XmlElement("cueFileName")]
    public Entry CueFileName { get; set; } = Entry.Default;

    [XmlElement("worldsEndTagName")]
    public Entry WorldsEndTagName { get; set; } = Entry.Default;

    [XmlElement("starDifType")]
    public int StarDifType { get; set; }

    [XmlElement("stageName")]
    public Entry StageName { get; set; } = Entry.Default;

    [XmlArray("fumens")]
    [XmlArrayItem("MusicFumenData")]
    public List<MusicFumenData> Fumens { get; set; } = [];

    private static (int whole, int frac) SplitLevel(decimal level)
    {
        if (level <= 0) return (0, 0);
        var w = (int)decimal.Truncate(level);
        var f = (int)((level - w) * 100);
        if (f < 100) return (w, f);
        w += 1;
        f -= 100;
        return (w, f);
    }
}

public class MusicFumenData
{
    [XmlElement("type")]
    public Entry Type { get; set; } = Entry.Default;

    [XmlElement("enable")]
    public bool Enable { get; set; }

    [XmlElement("file")]
    public PathElement File { get; set; } = string.Empty;

    [XmlElement("level")]
    public int Level { get; set; }

    [XmlElement("levelDecimal")]
    public int LevelDecimal { get; set; }

    [XmlElement("notesDesigner")]
    public string NotesDesigner { get; set; } = string.Empty;

    [XmlElement("defaultBpm")]
    public decimal DefaultBpm { get; set; }
}