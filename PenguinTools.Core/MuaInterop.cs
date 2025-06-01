using PenguinTools.Common.Resources;
using System.Runtime.InteropServices;

namespace PenguinTools.Common;

public static partial class MuaInterop
{
    private const string DllName = "mua_lib.dll";
    internal static Stream LibraryStream => ResourceManager.GetStream("mua_lib.dll");
    private const int MaxMessageLength = 1024;

    public static void ConvertJk(string inPath, string outPath)
    {
        unsafe
        {
            var buffer = stackalloc char[MaxMessageLength];
            var hr = convert_jk(inPath, outPath, buffer);
            if (hr == 0) return;
            var msg = new string(buffer);
            throw new DiagnosticException(string.IsNullOrWhiteSpace(msg) ? Strings.Error_interop_Jacket : msg);
        }
    }

    public static void ConvertStage(string bgInPath, string?[]? fxInPaths, string stOutPath, string nfOutPath)
    {
        unsafe
        {
            var buffer = stackalloc char[MaxMessageLength];
            var hr = convert_stage(bgInPath, fxInPaths, fxInPaths?.Length ?? 0, stOutPath, nfOutPath, buffer);
            if (hr == 0) return;
            var msg = new string(buffer);
            throw new DiagnosticException(string.IsNullOrWhiteSpace(msg) ? Strings.Error_interop_bg : msg);
        }
    }

    public static bool IsValidImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.Exists(path)) return false;
        unsafe
        {
            var buffer = stackalloc char[MaxMessageLength];
            var hr = validate_image(path, buffer);
            return hr == 0;
        }
    }

    public static void ExtractAfb(string inPath, string outFolder)
    {
        unsafe
        {
            var buffer = stackalloc char[MaxMessageLength];
            var hr = extract_afb(inPath, outFolder, buffer);
            if (hr == 0) return;
            var msg = new string(buffer);
            throw new DiagnosticException(string.IsNullOrWhiteSpace(msg) ? Strings.Error_iinterop_afb : msg);
        }
    }

    [LibraryImport(DllName, EntryPoint = "validate_image", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe partial int validate_image(string inPath, char* errorBuffer, int errorBufferSize = MaxMessageLength);

    [LibraryImport(DllName, EntryPoint = "extract_afb", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe partial int extract_afb(string inPath, string outFolder, char* errorBuffer, int errorBufferSize = MaxMessageLength);

    [LibraryImport(DllName, EntryPoint = "convert_stage", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe partial int convert_stage(string bgInPath, [In] string?[]? fxInPaths, int fxInPathsCount, string stOutPath, string nfOutPath, char* errorBuffer, int errorBufferSize = MaxMessageLength);

    [LibraryImport(DllName, EntryPoint = "convert_jk", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe partial int convert_jk(string inPath, string outPath, char* errorBuffer, int errorBufferSize = MaxMessageLength);
};