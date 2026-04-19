using System.CommandLine;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class CommandLineOptions
{
    internal static AudioCommandOptions CreateAudioCommandOptions()
    {
        return new AudioCommandOptions(
            new Option<string?>("--dummy-acb")
            {
                Description = "Override the dummy ACB template path."
            },
            new Option<string?>("--working-audio")
            {
                Description = "Override the intermediate WAV path used during audio conversion."
            },
            new Option<ulong?>("--hca-key")
            {
                Description = "Override the HCA encryption key."
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
                Description = "Override the stage background image path."
            },
            new Option<string?>("--effect-1")
            {
                Description = "Optional first stage effect image path."
            },
            new Option<string?>("--effect-2")
            {
                Description = "Optional second stage effect image path."
            },
            new Option<string?>("--effect-3")
            {
                Description = "Optional third stage effect image path."
            },
            new Option<string?>("--effect-4")
            {
                Description = "Optional fourth stage effect image path."
            },
            new Option<int?>("--stage-id")
            {
                Description = "Override the custom stage ID."
            },
            new Option<int?>("--notes-field-line-id")
            {
                Description = "Override the notes field line entry ID."
            },
            new Option<string?>("--notes-field-line-name")
            {
                Description = "Override the notes field line entry name."
            },
            new Option<string?>("--notes-field-line-data")
            {
                Description = "Override the notes field line entry data value."
            },
            new Option<string?>("--stage-template")
            {
                Description = "Override the stage template AFB path."
            },
            new Option<string?>("--notes-field-template")
            {
                Description = "Override the notes field template AFB path."
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
