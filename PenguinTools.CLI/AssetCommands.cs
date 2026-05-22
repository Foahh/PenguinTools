using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.i18n;
using PenguinTools.Infrastructure;

namespace PenguinTools.CLI;

internal static class AssetCommands
{
    internal static Command BuildAssetCommand()
    {
        var command = new Command("assets", Strings.Cli_Cmd_assets);
        command.Subcommands.Add(BuildCollectCommand());
        return command;
    }

    private static Command BuildCollectCommand()
    {
        var gameRootArgument = new Argument<string>("game-root")
        {
            Description = Strings.Cli_Arg_game_root
        };

        var command = new Command(
            "collect",
            string.Format(Strings.Cli_Cmd_assets_collect, AssetManager.PlusAssetsFileName,
                ApplicationPaths.UserDataEnvironmentVariable));
        command.Arguments.Add(gameRootArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var gameRoot = CliPaths.ResolvePath(parseResult.GetRequiredValue(gameRootArgument));
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("assets collect", outputOptions, async (runtime, ct) =>
            {
                if (!Directory.Exists(gameRoot))
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(
                            CliDiagnostics.SnapshotFromMessage(
                                string.Format(Strings.Cli_Msg_directory_not_found, gameRoot))),
                        string.Format(Strings.Cli_Msg_directory_not_found, gameRoot));

                await runtime.Assets.CollectAssetsAsync(gameRoot, ct);
                var writtenPath = runtime.Assets.PlusAssetsPath;
                return new CliCommandOutcome(
                    OperationResult.Success(),
                    string.Format(Strings.Cli_Msg_assets_collected, writtenPath),
                    new CliCommandData(gameRoot, writtenPath));
            }, cancellationToken);
        });

        return command;
    }
}
