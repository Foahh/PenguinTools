using PenguinTools.Core.Asset;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Chart.Parser;

public partial class MgxcParser
{
    private void ParseMeta(BinaryReader br)
    {
        var name = br.ReadUtf8String(4);
        var data = br.ReadField();

        if (name == "titl")
        {
            Mgxc.Meta.Title = (string)data;
        }
        else if (name == "sort")
        {
            Mgxc.Meta.SortName = (string)data;
        }
        else if (name == "arts")
        {
            Mgxc.Meta.Artist = (string)data;
        }
        else if (name == "genr")
        {
            var genre = (string)data;
            var entry = Assets.GenreNames.FirstOrDefault(e => e.Str.Equals(genre, StringComparison.Ordinal));
            if (entry != null) Mgxc.Meta.Genre = entry;
        }
        else if (name == "dsgn")
        {
            Mgxc.Meta.Designer = (string)data;
        }
        else if (name == "diff")
        {
            Mgxc.Meta.Difficulty = (int)data switch
            {
                0 => Difficulty.Basic,
                1 => Difficulty.Advanced,
                2 => Difficulty.Expert,
                3 => Difficulty.Master,
                4 => Difficulty.WorldsEnd,
                5 => Difficulty.Ultima,
                _ => Difficulty.Master
            };
            if (Mgxc.Meta.Difficulty == Difficulty.WorldsEnd) Mgxc.Meta.Stage = new Entry(0, "WORLD'S END0001_ノイズ");
        }
        else if (name == "plvl")
        {
            if (Mgxc.Meta.Difficulty != Difficulty.WorldsEnd) return;
            var trimmed = ((string)data).Trim('+');
            if (!int.TryParse(trimmed, out var num)) return;
            Mgxc.Meta.WeDifficulty = num switch
            {
                1 => StarDifficulty.S1,
                2 => StarDifficulty.S2,
                3 => StarDifficulty.S3,
                4 => StarDifficulty.S4,
                5 => StarDifficulty.S5,
                _ => StarDifficulty.Na
            };
        }
        else if (name == "weat")
        {
            var attr = Assets.WeTagNames.FirstOrDefault(x => x.Str == (string)data);
            if (attr != null) Mgxc.Meta.WeTag = attr;
        }
        else if (name == "cnst")
        {
            if (Mgxc.Meta.Difficulty == Difficulty.WorldsEnd) return;
            Mgxc.Meta.Level = data.Round(2);
        }
        else if (name == "sgid")
        {
            Mgxc.Meta.MgxcId = (string)data;
            if (int.TryParse(Mgxc.Meta.MgxcId, out var id)) Mgxc.Meta.Id = id;
        }
        else if (name == "wvfn")
        {
            Mgxc.Meta.BgmFilePath = (string)data;
            if (!string.IsNullOrWhiteSpace(Mgxc.Meta.BgmFilePath))
            {
                Tasks.Add(Manipulate.IsAudioValidAsync(Mgxc.Meta.FullBgmFilePath).ContinueWith(p =>
                {
                    if (p.IsCompletedSuccessfully) return;
                    Diagnostic.Report(Severity.Warning, Strings.Error_Invalid_audio, Mgxc.Meta.FullBgmFilePath);
                    Mgxc.Meta.BgmFilePath = string.Empty;
                }));
            }
        }
        else if (name == "wvof")
        {
            Mgxc.Meta.BgmManualOffset = data.Round();
        }
        else if (name == "wvp0")
        {
            Mgxc.Meta.BgmPreviewStart = data.Round();
        }
        else if (name == "wvp1")
        {
            Mgxc.Meta.BgmPreviewStop = data.Round();
        }
        else if (name == "jack")
        {
            Mgxc.Meta.JacketFilePath = (string)data;
            if (!string.IsNullOrWhiteSpace(Mgxc.Meta.JacketFilePath))
            {
                Tasks.Add(Manipulate.IsImageValidAsync(Mgxc.Meta.FullJacketFilePath).ContinueWith(p =>
                {
                    if (p.IsCompletedSuccessfully) return;
                    Diagnostic.Report(Severity.Warning, Strings.Error_Invalid_jk_image, Mgxc.Meta.FullJacketFilePath);
                    Mgxc.Meta.JacketFilePath = string.Empty;
                }));
            }
        }
        else if (name == "bgfn")
        {
            var path = (string)data;
            Mgxc.Meta.BgiFilePath = path;
            if (!string.IsNullOrWhiteSpace(path)) Mgxc.Meta.IsCustomStage = true;
        }
        else if (name == "bgsc")
        {
            // BGSCENE
        }
        else if (name == "bgsy")
        {
            // BGSYNC
        }
        else if (name == "flcl")
        {
            // FIELDCOL
        }
        else if (name == "flcx")
        {
            // FIELD COLOR
            var col = (int)data switch
            {
                0 => "White",
                1 => "Red",
                2 => "Orange",
                3 => "Yellow",
                4 => "Olive", // Lime
                5 => "Green",
                6 => "SkyBlue", // Teal
                7 => "Blue",
                8 => "Purple",
                _ => "Orange"
            };
            Mgxc.Meta.NotesFieldLine = Assets.FieldLines.FirstOrDefault(x => x.Str == col) ?? Mgxc.Meta.NotesFieldLine;
        }
        else if (name == "flbg")
        {
            // FIELDBG
        }
        else if (name == "flsc")
        {
            // FIELDSCENE
        }
        else if (name == "mtil")
        {
            Mgxc.Meta.MainTil = (int)data;
        }
        else if (name == "mbpm")
        {
            Mgxc.Meta.MainBpm = data.Round();
        }
        else if (name == "ttrl")
        {
            // TUTORIAL
        }
        else if (name == "sofs")
        {
            Mgxc.Meta.BgmEnableBarOffset = Convert.ToBoolean((int)data);
        }
        else if (name == "uclk")
        {
            // USECLICK
        }
        else if (name == "xlng")
        {
            // EXLONG
        }
        else if (name == "bgmw")
        {
            // BGMWAITEND
        }
        else if (name == "atls")
        {
            // AUTHOR LIST
        }
        else if (name == "atst")
        {
            // AUTHOR SITES
        }
        else if (name == "durl")
        {
            // DLURL
        }
        else if (name == "lcpy")
        {
            // COPYRIGHT
        }
        else if (name == "ltyp")
        {
            // LICENSE
        }
        else if (name == "lurl")
        {
            // LICENSE URL
        }
        else if (name == "xver")
        {
            // XVER
        }
        else if (name == "cmmt")
        {
            Mgxc.Meta.Comment = (string)data;
        }
        else if (name == "CTCK")
        {
            // last cursor position?
        }
        else if (name == "LXFN")
        {
            // .ugc location?
        }
        else if (name == "HSCL")
        {
            // idk
        }
        else if (name == "\0\0\0\0")
        {
            // why
        }
        else
        {
            var msg = string.Format(Strings.Mg_Unrecognized_meta, name, br.BaseStream.Position, data);
            Diagnostic.Report(Severity.Information, msg);
        }
    }
}