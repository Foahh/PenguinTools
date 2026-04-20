using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

/// <summary>
///     Resolves temp and user-data directories.
///     Override with <c>PENGUIN_TOOLS_TEMP</c> and <c>PENGUIN_TOOLS_USER_DATA</c>.
/// </summary>
public sealed class ApplicationPaths : IApplicationPaths
{
    public const string TempEnvironmentVariable = "PENGUIN_TOOLS_TEMP";
    public const string UserDataEnvironmentVariable = "PENGUIN_TOOLS_USER_DATA";

    private const string DefaultTempSubfolder = "PenguinTools.Temp";
    private const string AppFolderName = "PenguinTools";

    private ApplicationPaths(string tempWorkPath, string userDataPath, string sharedAssetCachePath)
    {
        TempWorkPath = tempWorkPath;
        UserDataPath = userDataPath;
        SharedAssetCachePath = sharedAssetCachePath;
    }

    public string TempWorkPath { get; }
    public string UserDataPath { get; }
    public string SharedAssetCachePath { get; }

    public static ApplicationPaths Create()
    {
        var tempWorkPath = ResolveTempWorkPath();
        var userDataPath = ResolveUserDataPath();
        var sharedAssetCachePath = ResolveSharedAssetCachePath();
        Directory.CreateDirectory(tempWorkPath);
        Directory.CreateDirectory(userDataPath);
        Directory.CreateDirectory(sharedAssetCachePath);
        return new ApplicationPaths(tempWorkPath, userDataPath, sharedAssetCachePath);
    }

    private static string ResolveTempWorkPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable(TempEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv)) return Path.GetFullPath(fromEnv.Trim());

        return Path.Combine(Path.GetTempPath(), DefaultTempSubfolder);
    }

    private static string ResolveUserDataPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable(UserDataEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv)) return Path.GetFullPath(fromEnv.Trim());

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, AppFolderName);
    }

    private static string ResolveSharedAssetCachePath()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, AppFolderName, "assets", version);
    }
}