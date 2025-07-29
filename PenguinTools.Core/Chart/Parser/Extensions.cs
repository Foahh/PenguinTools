using PenguinTools.Core.Resources;
using System.Text;

namespace PenguinTools.Core.Chart.Parser;

internal static class Extensions
{
    public static void ReadBlock(this BinaryReader br, string expected, Action<BinaryReader> action)
    {
        var actual = br.ReadUtf8String(4);
        if (actual != expected)
        {
            var msg = string.Format(Strings.Error_Invalid_Header, actual, expected);
            throw new DiagnosticException(msg);
        }

        var size = br.ReadInt32();
        var bytes = br.ReadBytes(size);
        if (bytes.Length < size)
        {
            var msg = string.Format(Strings.Error_Size_Incompatible, size, expected, bytes.Length);
            throw new DiagnosticException(msg);
        }

        using var ms = new MemoryStream(bytes);
        using var nr = new BinaryReader(ms);
        while (nr.BaseStream.Position < nr.BaseStream.Length) action(nr);
    }

    public static string ReadUtf8String(this BinaryReader br, int length)
    {
        if (length > 128) return Encoding.UTF8.GetString(br.ReadBytes(length));
        // use stackalloc for small strings
        Span<byte> buffer = stackalloc byte[length];
        var read = br.Read(buffer);
        if (read == length) return Encoding.UTF8.GetString(buffer);
        var msg = string.Format(Strings.Error_Size_Incompatible, length, "UTF8", read);
        throw new DiagnosticException(msg);
    }

    public static object ReadData(this BinaryReader br)
    {
        var type = br.ReadInt16();
        var attr = br.ReadInt16();

        return type switch
        {
            4 => br.ReadUtf8String(attr),
            3 => br.ReadDouble(),
            2 => br.ReadInt32(),
            1 or 0 => (int)attr,
            _ => throw new DiagnosticException(string.Format(Strings.MgCrit_Unrecognized_data_type, type, br.BaseStream.Position))
        };
    }

    public static object ReadBigData(this BinaryReader br)
    {
        var type = br.ReadInt32();
        var attr = br.ReadInt32();
        if (type == 4) return br.ReadUtf8String(attr);
        var msg = string.Format(Strings.MgCrit_Unrecognized_data_type, type, br.BaseStream.Position);
        throw new DiagnosticException(msg);
    }

    public static decimal Round(this object obj, int decimals = 6)
    {
        return obj switch
        {
            double d => Math.Round((decimal)d, decimals),
            int i => i,
            short s => s,
            byte b => b,
            _ => Math.Round((decimal)obj, decimals)
        };
    }
}