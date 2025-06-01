using PenguinTools.Common.Graphic;
using PenguinTools.Common.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace PenguinTools.Common;

public static class ResourceManager
{
    private static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name ?? nameof(ResourceManager);
    private static readonly Lock Lock = new();
    public static string TempWorkPath => Path.Combine(Path.GetTempPath(), Name);

    public static void Initialize()
    {
        lock (Lock)
        {
            Directory.CreateDirectory(TempWorkPath);
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!path.Contains(TempWorkPath, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", $"{TempWorkPath};{path}");
            }
        }
        Register("mua_lib.dll", MuaInterop.LibraryStream);
    }

    public static string GetTempPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
        var tempPath = Path.Combine(TempWorkPath, fileName);
        return tempPath;
    }

    #region Management

    public static void Release()
    {
        lock (Lock)
        {
            foreach (var filePath in Directory.GetFiles(TempWorkPath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            try
            {
                Directory.Delete(TempWorkPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private static void Register(string fileName, Stream resource)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
        if (resource == null || resource.Length == 0) throw new ArgumentNullException(nameof(resource));

        lock (Lock)
        {
            var finalPath = Path.Combine(TempWorkPath, fileName);
            using var fileStream = File.Create(finalPath);
            resource.CopyTo(fileStream);
        }
    }

    #endregion

    #region Resources

    internal static Stream GetStream(string resourceName)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly '{Name}'.");
        return stream;
    }

    internal static byte[] GetByte(string resourceName)
    {
        var stream = GetStream(resourceName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    #endregion"
}