using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;

namespace PenguinTools.CLI;

internal static class AssetCommands
{
    internal static Command BuildAssetCommand()
    {
        var command = new Command("assets", "Asset dictionary utilities.");
        command.Subcommands.Add(BuildCollectCommand());
        return command;
    }

    private static Command BuildCollectCommand()
    {
        var gameRootArgument = new Argument<string>("game-root")
        {
            Description = "Directory to scan recursively for Music.xml and Stage.xml (typically the game or data install root)."
        };

        var command = new Command(
            "collect",
            $"Scan game XML and merge entries into {AssetManager.PlusAssetsFileName} under the per-user data directory (see {ApplicationPaths.UserDataEnvironmentVariable}).");
        command.Arguments.Add(gameRootArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var gameRoot = CliPaths.ResolvePath(parseResult.GetRequiredValue(gameRootArgument));
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("assets collect", outputFormat, async (runtime, ct) =>
            {
                if (!Directory.Exists(gameRoot))
                {
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(
                            CliDiagnostics.SnapshotFromMessage($"Directory not found: {gameRoot}")),
                        $"Directory not found: {gameRoot}");
                }

                await runtime.Assets.CollectAssetsAsync(gameRoot, ct);
                var writtenPath = runtime.Assets.PlusAssetsPath;
                return new CliCommandOutcome(
                    OperationResult.Success(),
                    $"Collected assets and wrote {writtenPath}.",
                    new CliCommandData(InputPath: gameRoot, OutputPath: writtenPath));
            }, cancellationToken);
        });

        return command;
    }
}
