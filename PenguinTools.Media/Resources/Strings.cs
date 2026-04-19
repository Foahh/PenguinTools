using System.Globalization;
using System.Resources;

namespace PenguinTools.Media.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??=
        new ResourceManager("PenguinTools.Media.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string Error_Audio_file_not_found =>
        ResourceManager.GetString(nameof(Error_Audio_file_not_found), Culture) ?? string.Empty;

    public static string Error_Audio_format_not_supported =>
        ResourceManager.GetString(nameof(Error_Audio_format_not_supported), Culture) ?? string.Empty;

    public static string Error_Background_file_not_found =>
        ResourceManager.GetString(nameof(Error_Background_file_not_found), Culture) ?? string.Empty;

    public static string Error_Command_failed =>
        ResourceManager.GetString(nameof(Error_Command_failed), Culture) ?? string.Empty;

    public static string Error_Effect_file_not_found =>
        ResourceManager.GetString(nameof(Error_Effect_file_not_found), Culture) ?? string.Empty;

    public static string Error_File_not_found =>
        ResourceManager.GetString(nameof(Error_File_not_found), Culture) ?? string.Empty;

    public static string Error_Invalid_bg_fx_image =>
        ResourceManager.GetString(nameof(Error_Invalid_bg_fx_image), Culture) ?? string.Empty;

    public static string Error_Invalid_bg_image =>
        ResourceManager.GetString(nameof(Error_Invalid_bg_image), Culture) ?? string.Empty;

    public static string Error_Jacket_file_not_found =>
        ResourceManager.GetString(nameof(Error_Jacket_file_not_found), Culture) ?? string.Empty;

    public static string Error_Preview_stop_greater_than_start =>
        ResourceManager.GetString(nameof(Error_Preview_stop_greater_than_start), Culture) ?? string.Empty;

    public static string Error_Song_id_is_not_set =>
        ResourceManager.GetString(nameof(Error_Song_id_is_not_set), Culture) ?? string.Empty;

    public static string Error_Stage_id_is_not_set =>
        ResourceManager.GetString(nameof(Error_Stage_id_is_not_set), Culture) ?? string.Empty;

    public static string Hint_Preview_value_clamped =>
        ResourceManager.GetString(nameof(Hint_Preview_value_clamped), Culture) ?? string.Empty;

    public static string Warn_Preview_later_than_120 =>
        ResourceManager.GetString(nameof(Warn_Preview_later_than_120), Culture) ?? string.Empty;

    public static string Warn_Stage_already_exists =>
        ResourceManager.GetString(nameof(Warn_Stage_already_exists), Culture) ?? string.Empty;
}