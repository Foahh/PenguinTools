using System.CommandLine;
using System.Globalization;
using System.Text;
using PenguinTools.Core;
using PenguinTools.Core.Diagnostic;
using PenguinTools.i18n;
using PenguinTools.Infrastructure;

namespace PenguinTools.CLI;

internal static class InfoCommands
{
    internal static Command BuildInfoCommand()
    {
        var command = new Command("info", Strings.Cli_Cmd_info);
        command.SetAction((parseResult, _) =>
        {
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            try
            {
                var paths = ApplicationPaths.Create();
                var storeOptions =
#if PENGUINTOOLS_EXTERNAL_ASSETS
                    ResourceStoreOptions.External();
#else
                    ResourceStoreOptions.Resolve();
#endif
                var info = ExecutionInfoProvider.Create(paths, storeOptions);
                var outcome = new CliCommandOutcome(
                    OperationResult.Success(),
                    FormatText(info),
                    new CliCommandData(ExecInfo: info));
                CliOutput.Write("info", outputOptions, outcome);
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                var outcome = new CliCommandOutcome(
                    OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromException(ex)),
                    ex.Message);
                CliOutput.Write("info", outputOptions, outcome);
                return Task.FromResult(1);
            }
        });

        return command;
    }

    private static string FormatText(ExecutionInfo info)
    {
        var builder = new StringBuilder();
        AppendLine(builder, Strings.Cli_Info_application, info.ApplicationName);
        AppendLine(builder, Strings.Cli_Info_version, info.Version);
        AppendLine(builder, Strings.Cli_Info_build_date,
            info.BuildDateUtc?.ToString("u", CultureInfo.InvariantCulture) ?? Strings.Cli_Info_unknown);
        AppendLine(builder, Strings.Cli_Info_base_directory, info.BaseDirectory);
        AppendLine(builder, Strings.Cli_Info_temp_path, info.TempWorkPath);
        AppendLine(builder, Strings.Cli_Info_user_data_path, info.UserDataPath);
        AppendLine(builder, Strings.Cli_Info_shared_asset_cache_path, info.SharedAssetCachePath);
        AppendLine(builder, Strings.Cli_Info_infrastructure_assets_path, info.InfrastructureAssetsPath);
        AppendLine(builder, Strings.Cli_Info_assets_mode, info.AssetsMode);
        AppendLine(builder, Strings.Cli_Info_plus_assets_path, info.PlusAssetsPath);
        return builder.ToString().TrimEnd();
    }

    private static void AppendLine(StringBuilder builder, string label, string value)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(value);
    }
}
