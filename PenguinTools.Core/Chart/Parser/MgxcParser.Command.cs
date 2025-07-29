using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Chart.Parser;

public partial class MgxcParser
{
    private void MetaEntryHandler(string name, string[] args, Action<Entry> setter, AssetType type)
    {
        if (args.Length is < 1 or > 2)
        {
            var msg = string.Format(Strings.Mg_Meta_override_argument_count_mismatch, name);
            Diagnostic.Report(Severity.Warning, msg, target: args);
            return;
        }

        if (args.Length >= 2)
        {
            var newId = int.TryParse(args[0], out var parsedId) ? parsedId : throw new DiagnosticException(Strings.Mg_First_argument_must_int);
            var data = args.Length >= 3 ? args[2] : null;
            var newEntry = new Entry(newId, args[1], data ?? string.Empty);
            setter(newEntry);
            Assets.DefineEntry(type, newEntry);
            return;
        }

        var value = args[0];
        var entry = int.TryParse(value, out var id) ? Assets[type].FirstOrDefault(e => e.Id == id) : null;
        entry ??= Assets[type].FirstOrDefault(e => e.Str.Equals(value, StringComparison.Ordinal));

        if (entry == null)
        {
            var msg = string.Format(Strings.Mg_String_id_not_found, value, type.ToString());
            Diagnostic.Report(Severity.Information, msg, target: args);
        }
        else
        {
            setter(entry);
        }
    }

    private void MetaGenreHandler(string[] args)
    {
        MetaEntryHandler("genre", args, entry => Mgxc.Meta.Genre = entry, AssetType.GenreNames);
    }

    private void MetaStageHandler(string[] args)
    {
        MetaEntryHandler("stage", args, Setter, AssetType.StageNames);

        void Setter(Entry entry)
        {
            Mgxc.Meta.Stage = entry;
            Mgxc.Meta.IsCustomStage = false;
        }
    }

    private void MetaFieldLineHandler(string[] args)
    {
        MetaEntryHandler("fline", args, entry => Mgxc.Meta.NotesFieldLine = entry, AssetType.FieldLines);
    }

    private void MetaWeTagHandler(string[] args)
    {
        MetaEntryHandler("wetag", args, entry => Mgxc.Meta.WeTag = entry, AssetType.WeTagNames);
    }

    private void MainHandler(string[] args)
    {
        Mgxc.Meta.IsMain = args.Length < 1 || ParseBool(args[0]);
    }

    private void MetaHandler(string[] args)
    {
        var (name, value) = (args[0], args[1..]);

        switch (name)
        {
            case "stage":
                MetaStageHandler(value);
                break;
            case "main":
                MainHandler(value);
                break;
            case "genre":
                MetaGenreHandler(value);
                break;
            case "fline":
                MetaFieldLineHandler(value);
                break;
            case "wetag":
                MetaWeTagHandler(value);
                break;
            default:
                Diagnostic.Report(Severity.Warning, string.Format(Strings.Mg_Unknown_tag, name), target: args);
                break;
        }
    }

    private void ProcessCommand()
    {
        var config = new Dictionary<string, Action<string[]>>
        {
            {
                "meta", MetaHandler
            }
        };

        var lines = Mgxc.Meta.Comment.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith('#')) continue;

            var parts = trimmedLine[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var tagName = parts[0];
            var tagArgs = parts.Skip(1).ToArray();

            if (config.TryGetValue(tagName, out var handler))
            {
                try
                {
                    handler(tagArgs);
                }
                catch (Exception ex)
                {
                    Diagnostic.Report(ex);
                }
            }
            else
            {
                Diagnostic.Report(
                    Severity.Warning,
                    string.Format(Strings.Mg_Unknown_tag, tagName),
                    target: parts
                );
            }
        }
    }

    private static bool ParseBool(string str)
    {
        var value = str.ToLowerInvariant();
        if (value is "true" or "1" or "yes") return true;
        if (value is "false" or "0" or "no") return false;
        var test = string.IsNullOrWhiteSpace(str);
        return test;
    }
}