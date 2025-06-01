using PenguinTools.Common.Asset;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;

namespace PenguinTools.Common.Chart.Parser;

public partial class MgxcParser
{
    private void ParseMeta(BinaryReader br)
    {
        var name = br.ReadUtf8String(4);
        var data = br.ReadData();

        if (name == "titl")
        {
            mgxc.Meta.Title = (string)data;
        }
        else if (name == "sort")
        {
            mgxc.Meta.SortName = (string)data;
        }
        else if (name == "arts")
        {
            mgxc.Meta.Artist = (string)data;
        }
        else if (name == "genr")
        {
            var genre = (string)data;
            var entry = asm.GenreNames.FirstOrDefault(e => e.Str.Equals(genre, StringComparison.Ordinal));
            if (entry != null) mgxc.Meta.Genre = entry;
        }
        else if (name == "dsgn")
        {
            mgxc.Meta.Designer = (string)data;
        }
        else if (name == "diff")
        {
            mgxc.Meta.Difficulty = (int)data switch
            {
                0 => Difficulty.Basic,
                1 => Difficulty.Advanced,
                2 => Difficulty.Expert,
                3 => Difficulty.Master,
                4 => Difficulty.WorldsEnd,
                5 => Difficulty.Ultima,
                _ => Difficulty.Master
            };
            if (mgxc.Meta.Difficulty == Difficulty.WorldsEnd) mgxc.Meta.Stage = new Entry(0, "WORLD'S END0001_ノイズ");
        }
        else if (name == "plvl")
        {
            if (mgxc.Meta.Difficulty != Difficulty.WorldsEnd) return;
            var trimmed = ((string)data).Trim('+');
            if (!int.TryParse(trimmed, out var num)) return;
            mgxc.Meta.WeDifficulty = num switch
            {
                1 => StarDifficulty.S1,
                2 => StarDifficulty.S2,
                3 => StarDifficulty.S3,
                4 => StarDifficulty.S4,
                5 => StarDifficulty.S5,
                _ => StarDifficulty.NA
            };
        }
        else if (name == "weat")
        {
            var attr = asm.WeTagNames.FirstOrDefault(x => x.Str == (string)data);
            if (attr != null) mgxc.Meta.WeTag = attr;
        }
        else if (name == "cnst")
        {
            if (mgxc.Meta.Difficulty == Difficulty.WorldsEnd) return;
            mgxc.Meta.Level = data.Round(2);
        }
        else if (name == "sgid")
        {
            mgxc.Meta.MgxcId = (string)data;
            if (int.TryParse(mgxc.Meta.MgxcId, out var id)) mgxc.Meta.Id = id;
        }
        else if (name == "wvfn")
        {
            try
            {
                mgxc.Meta.BgmFilePath = (string)data;
            }
            catch (Exception ex)
            {
                diag.Report(ex);
            }
        }
        else if (name == "wvof")
        {
            mgxc.Meta.BgmManualOffset = data.Round();
        }
        else if (name == "wvp0")
        {
            mgxc.Meta.BgmPreviewStart = data.Round();
        }
        else if (name == "wvp1")
        {
            mgxc.Meta.BgmPreviewStop = data.Round();
        }
        else if (name == "jack")
        {
            mgxc.Meta.JacketFilePath = (string)data;
            if (MuaInterop.IsValidImage(mgxc.Meta.FullJacketFilePath)) return;
            diag.Report(Severity.Error, Strings.Error_invalid_jk_image, mgxc.Meta.FullJacketFilePath);
            mgxc.Meta.JacketFilePath = string.Empty;
        }
        else if (name == "bgfn")
        {
            var path = (string)data;
            mgxc.Meta.BgiFilePath = path;
            if (!string.IsNullOrWhiteSpace(path)) mgxc.Meta.IsCustomStage = true;
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
            mgxc.Meta.NotesFieldLine = asm.FieldLines.FirstOrDefault(x => x.Str == col) ?? mgxc.Meta.NotesFieldLine;
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
            mgxc.Meta.MainTil = (int)data;
        }
        else if (name == "mbpm")
        {
            mgxc.Meta.MainBpm = data.Round();
        }
        else if (name == "ttrl")
        {
            // TUTORIAL
        }
        else if (name == "sofs")
        {
            mgxc.Meta.BgmEnableBarOffset = Convert.ToBoolean((int)data);
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
            mgxc.Meta.Comment = (string)data;
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
            var msg = string.Format(Strings.Diag_Unrecognized_meta, name, br.BaseStream.Position, data);
            diag.Report(Severity.Information, msg);
        }
    }
}