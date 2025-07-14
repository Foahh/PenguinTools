using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Chart.Parser;

public partial class MgxcParser
{
    protected void MetaEntryHandler(string name, string[] args, Action<Entry> setter, AssetManager asm, AssetType type)
    {
        if (args.Length is < 1 or > 2)
        {
            var msg = string.Format(Strings.Diag_meta_override_arument_count_mismatch, name);
            diag.Report(Severity.Warning, msg, target: args);
            return;
        }

        if (args.Length >= 2)
        {
            var newId = int.TryParse(args[0], out var parsedId) ? parsedId : throw new DiagnosticException(Strings.Diag_first_argument_must_int);
            var data = args.Length >= 3 ? args[2] : null;
            var newEntry = new Entry(newId, args[1], data ?? string.Empty);
            setter(newEntry);
            asm.DefineEntry(type, newEntry);
            return;
        }

        var value = args[0];
        var entry = int.TryParse(value, out var id) ? asm[type].FirstOrDefault(e => e.Id == id) : null;
        entry ??= asm[type].FirstOrDefault(e => e.Str.Equals(value, StringComparison.Ordinal));

        if (entry == null)
        {
            var msg = string.Format(Strings.Diag_string_id_not_found, value, type.ToString());
            diag.Report(Severity.Information, msg, target: args);
        }
        else
        {
            setter(entry);
        }
    }

    protected void MetaGenreHandler(string[] args)
    {
        MetaEntryHandler("genre", args, entry => mgxc.Meta.Genre = entry, asm, AssetType.GenreNames);
    }

    protected void MetaStageHandler(string[] args)
    {
        MetaEntryHandler("stage", args, Setter, asm, AssetType.StageNames);

        void Setter(Entry entry)
        {
            mgxc.Meta.Stage = entry;
            mgxc.Meta.IsCustomStage = false;
        }
    }

    protected void MainHandler(string[] args)
    {
        mgxc.Meta.IsMain = args.Length < 1 || ParseBool(args[0]);
    }

    protected void MetaHandler(string[] args)
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
            default:
                diag.Report(Severity.Warning, string.Format(Strings.Diag_unknown_tag, name), target: args);
                break;
        }
    }

    protected void ProcessMetaCommand()
    {
        var config = new Dictionary<string, Action<string[]>>
        {
            { "meta", MetaHandler }
        };

        var lines = mgxc.Meta.Comment.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

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
                    diag.Report(ex);
                }
            }
            else
            {
                diag.Report(
                    Severity.Warning,
                    string.Format(Strings.Diag_unknown_tag, tagName),
                    target: parts
                );
            }
        }
    }

    protected static bool ParseBool(string str)
    {
        var value = str.ToLowerInvariant();
        if (value is "true" or "1" or "yes") return true;
        if (value is "false" or "0" or "no") return false;
        var test = string.IsNullOrWhiteSpace(str);
        return test;
    }
}