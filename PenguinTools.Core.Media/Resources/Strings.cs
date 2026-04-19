using System.Globalization;
using System.Resources;

namespace PenguinTools.Core.Media.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;
    private static CultureInfo? resourceCulture;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??= new ResourceManager("PenguinTools.Core.Media.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture
    {
        get => resourceCulture;
        set => resourceCulture = value;
    }

    public static string Error_Audio_file_not_found => ResourceManager.GetString(nameof(Error_Audio_file_not_found), resourceCulture) ?? string.Empty;
    public static string Error_Audio_format_not_supported => ResourceManager.GetString(nameof(Error_Audio_format_not_supported), resourceCulture) ?? string.Empty;
    public static string Error_Background_file_not_found => ResourceManager.GetString(nameof(Error_Background_file_not_found), resourceCulture) ?? string.Empty;
    public static string Error_Command_failed => ResourceManager.GetString(nameof(Error_Command_failed), resourceCulture) ?? string.Empty;
    public static string Error_Effect_file_not_found => ResourceManager.GetString(nameof(Error_Effect_file_not_found), resourceCulture) ?? string.Empty;
    public static string Error_File_not_found => ResourceManager.GetString(nameof(Error_File_not_found), resourceCulture) ?? string.Empty;
    public static string Error_Invalid_bg_fx_image => ResourceManager.GetString(nameof(Error_Invalid_bg_fx_image), resourceCulture) ?? string.Empty;
    public static string Error_Invalid_bg_image => ResourceManager.GetString(nameof(Error_Invalid_bg_image), resourceCulture) ?? string.Empty;
    public static string Error_Jacket_file_not_found => ResourceManager.GetString(nameof(Error_Jacket_file_not_found), resourceCulture) ?? string.Empty;
    public static string Error_Preview_stop_greater_than_start => ResourceManager.GetString(nameof(Error_Preview_stop_greater_than_start), resourceCulture) ?? string.Empty;
    public static string Error_Song_id_is_not_set => ResourceManager.GetString(nameof(Error_Song_id_is_not_set), resourceCulture) ?? string.Empty;
    public static string Error_Stage_id_is_not_set => ResourceManager.GetString(nameof(Error_Stage_id_is_not_set), resourceCulture) ?? string.Empty;
    public static string Hint_Preview_value_clamped => ResourceManager.GetString(nameof(Hint_Preview_value_clamped), resourceCulture) ?? string.Empty;
    public static string Warn_Preview_later_than_120 => ResourceManager.GetString(nameof(Warn_Preview_later_than_120), resourceCulture) ?? string.Empty;
    public static string Warn_Stage_already_exists => ResourceManager.GetString(nameof(Warn_Stage_already_exists), resourceCulture) ?? string.Empty;
}
