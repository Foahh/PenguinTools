using System.Reflection;

namespace PenguinTools.Core;

public static class Resourcer
{
    private const string Root = "PenguinTools.Temp";
    private static Assembly? _assembly;
    private static readonly Lock Lock = new();
    private static bool _isInitialized;
    public static string TempWorkPath => Path.Combine(Path.GetTempPath(), Root);

    public static void Initialize(Assembly assembly)
    {
        _assembly = assembly;
        Initialize();
    }

    private static void Initialize()
    {
        lock (Lock)
        {
            if (_isInitialized) { return; }

            Directory.CreateDirectory(TempWorkPath);
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!path.Contains(TempWorkPath, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", $"{TempWorkPath};{path}");
            }

            _isInitialized = true;
        }
    }

    public static string GetTempPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) { throw new ArgumentNullException(nameof(fileName)); }

        return Path.Combine(TempWorkPath, fileName);
    }

    #region Storage

    public static void Release()
    {
        lock (Lock)
        {
            foreach (var filePath in Directory.GetFiles(TempWorkPath))
            {
                try { File.Delete(filePath); }
                catch (Exception ex) { Console.WriteLine(ex); }
            }

            try { Directory.Delete(TempWorkPath, true); }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
    }

    public static void Save(string fileName, Stream resource)
    {
        Initialize();

        if (string.IsNullOrWhiteSpace(fileName)) { throw new ArgumentNullException(nameof(fileName)); }

        if (resource == null || resource.Length == 0) { throw new ArgumentNullException(nameof(resource)); }

        lock (Lock)
        {
            var finalPath = Path.Combine(TempWorkPath, fileName);
            using var fileStream = File.Create(finalPath);
            resource.CopyTo(fileStream);
        }
    }

    public static void Save(string fileName)
    {
        Initialize();

        Save(fileName, GetStream(fileName));
    }

    #endregion

    #region Resources

    public static async Task CopyAsync(string resourceName, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(resourceName)) { throw new ArgumentNullException(nameof(resourceName)); }

        if (string.IsNullOrWhiteSpace(destinationPath)) { throw new ArgumentNullException(nameof(destinationPath)); }

        await using var stream = GetStream(resourceName);
        await using var fileStream = File.Create(destinationPath);
        await stream.CopyToAsync(fileStream);
    }

    public static Stream GetStream(string resourceName)
    {
        if (_assembly == null) { throw new InvalidOperationException("RESOURCE_NOT_INITIALIZED"); }

        var stream = _assembly.GetManifestResourceStream(resourceName) ??
                     throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly '{Root}'.");
        return stream;
    }

    public static byte[] GetByte(string resourceName)
    {
        var stream = GetStream(resourceName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    #endregion"
}