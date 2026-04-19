using System.Globalization;
using System.Resources;

namespace PenguinTools.Core.Chart.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;
    private static CultureInfo? resourceCulture;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??= new ResourceManager("PenguinTools.Core.Chart.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture
    {
        get => resourceCulture;
        set => resourceCulture = value;
    }

    public static string Error_Invalid_Header => ResourceManager.GetString(nameof(Error_Invalid_Header), resourceCulture) ?? string.Empty;
    public static string Error_Invalid_audio => ResourceManager.GetString(nameof(Error_Invalid_audio), resourceCulture) ?? string.Empty;
    public static string Error_Invalid_bg_image => ResourceManager.GetString(nameof(Error_Invalid_bg_image), resourceCulture) ?? string.Empty;
    public static string Error_Invalid_jk_image => ResourceManager.GetString(nameof(Error_Invalid_jk_image), resourceCulture) ?? string.Empty;
    public static string Error_Size_Incompatible => ResourceManager.GetString(nameof(Error_Size_Incompatible), resourceCulture) ?? string.Empty;
    public static string MgCrit_Air_parent_null => ResourceManager.GetString(nameof(MgCrit_Air_parent_null), resourceCulture) ?? string.Empty;
    public static string MgCrit_Air_slide_parent_null => ResourceManager.GetString(nameof(MgCrit_Air_slide_parent_null), resourceCulture) ?? string.Empty;
    public static string MgCrit_Hold_has_no_tail => ResourceManager.GetString(nameof(MgCrit_Hold_has_no_tail), resourceCulture) ?? string.Empty;
    public static string MgCrit_Invalid_AirSlide_parent => ResourceManager.GetString(nameof(MgCrit_Invalid_AirSlide_parent), resourceCulture) ?? string.Empty;
    public static string MgCrit_Invalid_Air_parent => ResourceManager.GetString(nameof(MgCrit_Invalid_Air_parent), resourceCulture) ?? string.Empty;
    public static string MgCrit_Pairing_notes_incompatible => ResourceManager.GetString(nameof(MgCrit_Pairing_notes_incompatible), resourceCulture) ?? string.Empty;
    public static string MgCrit_SoflanArea_has_no_tail => ResourceManager.GetString(nameof(MgCrit_SoflanArea_has_no_tail), resourceCulture) ?? string.Empty;
    public static string MgCrit_Unrecognized_data_type => ResourceManager.GetString(nameof(MgCrit_Unrecognized_data_type), resourceCulture) ?? string.Empty;
    public static string MgCrit_Unrecognized_event => ResourceManager.GetString(nameof(MgCrit_Unrecognized_event), resourceCulture) ?? string.Empty;
    public static string Mg_Concurrent_ex_effects => ResourceManager.GetString(nameof(Mg_Concurrent_ex_effects), resourceCulture) ?? string.Empty;
    public static string Mg_Head_BPM_not_found => ResourceManager.GetString(nameof(Mg_Head_BPM_not_found), resourceCulture) ?? string.Empty;
    public static string Mg_Head_Time_Signature_event_not_found => ResourceManager.GetString(nameof(Mg_Head_Time_Signature_event_not_found), resourceCulture) ?? string.Empty;
    public static string Mg_Invalid_joint_type_note => ResourceManager.GetString(nameof(Mg_Invalid_joint_type_note), resourceCulture) ?? string.Empty;
    public static string Mg_Length_smaller_than_unit => ResourceManager.GetString(nameof(Mg_Length_smaller_than_unit), resourceCulture) ?? string.Empty;
    public static string Mg_Main_timeline_not_found => ResourceManager.GetString(nameof(Mg_Main_timeline_not_found), resourceCulture) ?? string.Empty;
    public static string Mg_Meta_Argument_count_min_one => ResourceManager.GetString(nameof(Mg_Meta_Argument_count_min_one), resourceCulture) ?? string.Empty;
    public static string Mg_Meta_First_argument_must_int => ResourceManager.GetString(nameof(Mg_Meta_First_argument_must_int), resourceCulture) ?? string.Empty;
    public static string Mg_Meta_Invalid_date => ResourceManager.GetString(nameof(Mg_Meta_Invalid_date), resourceCulture) ?? string.Empty;
    public static string Mg_Meta_Unknown_tag => ResourceManager.GetString(nameof(Mg_Meta_Unknown_tag), resourceCulture) ?? string.Empty;
    public static string Mg_No_sortname_provided => ResourceManager.GetString(nameof(Mg_No_sortname_provided), resourceCulture) ?? string.Empty;
    public static string Mg_Note_overlapped_in_different_TIL => ResourceManager.GetString(nameof(Mg_Note_overlapped_in_different_TIL), resourceCulture) ?? string.Empty;
    public static string Mg_Overlapping_air_parent_slide => ResourceManager.GetString(nameof(Mg_Overlapping_air_parent_slide), resourceCulture) ?? string.Empty;
    public static string Mg_String_id_not_found => ResourceManager.GetString(nameof(Mg_String_id_not_found), resourceCulture) ?? string.Empty;
    public static string Mg_Unrecognized_meta => ResourceManager.GetString(nameof(Mg_Unrecognized_meta), resourceCulture) ?? string.Empty;
    public static string Mg_Unrecognized_note => ResourceManager.GetString(nameof(Mg_Unrecognized_note), resourceCulture) ?? string.Empty;
}
