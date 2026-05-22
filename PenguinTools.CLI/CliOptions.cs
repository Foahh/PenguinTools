using System.CommandLine;
using PenguinTools.i18n;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class CommandLineOptions
{
    internal static Option<string?> CreateChartFileDiscoveryOption(string description)
    {
        return new Option<string?>("--chart-file-discovery")
        {
            Description = description
        };
    }

    internal static bool TryGetChartFileDiscovery(ParseResult parseResult, Option<string?> option,
        out IReadOnlyList<ChartFileFormat>? discovery, out string? error)
    {
        if (parseResult.GetValue(option) is { Length: > 0 } discoveryText)
        {
            if (!ChartFileDiscoveryFormats.TryParse(discoveryText, out var parsed, out error))
            {
                discovery = null;
                return false;
            }

            discovery = parsed;
            error = null;
            return true;
        }

        discovery = null;
        error = null;
        return true;
    }

    internal static AudioCommandOptions CreateAudioCommandOptions()
    {
        return new AudioCommandOptions(
            new Option<string?>("--dummy-acb")
            {
                Description = Strings.Cli_Opt_dummy_acb
            },
            new Option<string?>("--working-audio")
            {
                Description = Strings.Cli_Opt_working_audio
            },
            new Option<ulong?>("--hca-key")
            {
                Description = Strings.Cli_Opt_hca_key
            });
    }

    internal static void AddAudioCommandOptions(Command command, AudioCommandOptions options)
    {
        command.Options.Add(options.DummyAcbPath);
        command.Options.Add(options.WorkingAudioPath);
        command.Options.Add(options.HcaEncryptionKey);
    }

    internal static AudioRequestOverrides GetAudioRequestOverrides(ParseResult parseResult, AudioCommandOptions options)
    {
        return new AudioRequestOverrides(
            CliPaths.ResolveOptionalPath(parseResult.GetValue(options.DummyAcbPath)),
            CliPaths.ResolveOptionalPath(parseResult.GetValue(options.WorkingAudioPath)),
            parseResult.GetValue(options.HcaEncryptionKey));
    }

    internal static StageCommandOptions CreateStageCommandOptions()
    {
        return new StageCommandOptions(
            new Option<string?>("--background")
            {
                Description = Strings.Cli_Opt_stage_background
            },
            new Option<string?>("--effect-1")
            {
                Description = Strings.Cli_Opt_effect_1
            },
            new Option<string?>("--effect-2")
            {
                Description = Strings.Cli_Opt_effect_2
            },
            new Option<string?>("--effect-3")
            {
                Description = Strings.Cli_Opt_effect_3
            },
            new Option<string?>("--effect-4")
            {
                Description = Strings.Cli_Opt_effect_4
            },
            new Option<int?>("--stage-id")
            {
                Description = Strings.Cli_Opt_stage_id
            },
            new Option<int?>("--notes-field-line-id")
            {
                Description = Strings.Cli_Opt_notes_field_line_id
            },
            new Option<string?>("--notes-field-line-name")
            {
                Description = Strings.Cli_Opt_notes_field_line_name
            },
            new Option<string?>("--notes-field-line-data")
            {
                Description = Strings.Cli_Opt_notes_field_line_data
            },
            new Option<string?>("--stage-template")
            {
                Description = Strings.Cli_Opt_stage_template
            },
            new Option<string?>("--notes-field-template")
            {
                Description = Strings.Cli_Opt_notes_field_template
            });
    }

    internal static void AddStageCommandOptions(Command command, StageCommandOptions options)
    {
        command.Options.Add(options.BackgroundPath);
        command.Options.Add(options.Effect1Path);
        command.Options.Add(options.Effect2Path);
        command.Options.Add(options.Effect3Path);
        command.Options.Add(options.Effect4Path);
        command.Options.Add(options.StageId);
        command.Options.Add(options.NoteFieldLaneId);
        command.Options.Add(options.NoteFieldLaneName);
        command.Options.Add(options.NoteFieldLaneData);
        command.Options.Add(options.StageTemplatePath);
        command.Options.Add(options.NotesFieldTemplatePath);
    }

    internal static StageRequestOverrides GetStageRequestOverrides(ParseResult parseResult, StageCommandOptions options)
    {
        return new StageRequestOverrides(
            CliPaths.ResolveOptionalPath(parseResult.GetValue(options.BackgroundPath)),
            [
                CliPaths.ResolveOptionalPath(parseResult.GetValue(options.Effect1Path)),
                CliPaths.ResolveOptionalPath(parseResult.GetValue(options.Effect2Path)),
                CliPaths.ResolveOptionalPath(parseResult.GetValue(options.Effect3Path)),
                CliPaths.ResolveOptionalPath(parseResult.GetValue(options.Effect4Path))
            ],
            parseResult.GetValue(options.StageId),
            parseResult.GetValue(options.NoteFieldLaneId),
            parseResult.GetValue(options.NoteFieldLaneName),
            parseResult.GetValue(options.NoteFieldLaneData),
            CliPaths.ResolveOptionalPath(parseResult.GetValue(options.StageTemplatePath)),
            CliPaths.ResolveOptionalPath(parseResult.GetValue(options.NotesFieldTemplatePath)));
    }
}
