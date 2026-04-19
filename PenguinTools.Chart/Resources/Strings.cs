using System.Globalization;
using System.Resources;

namespace PenguinTools.Chart.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??=
        new ResourceManager("PenguinTools.Chart.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string Error_Invalid_Header =>
        ResourceManager.GetString(nameof(Error_Invalid_Header), Culture) ?? string.Empty;

    public static string Error_Invalid_audio =>
        ResourceManager.GetString(nameof(Error_Invalid_audio), Culture) ?? string.Empty;

    public static string Error_Invalid_bg_image =>
        ResourceManager.GetString(nameof(Error_Invalid_bg_image), Culture) ?? string.Empty;

    public static string Error_Invalid_jk_image =>
        ResourceManager.GetString(nameof(Error_Invalid_jk_image), Culture) ?? string.Empty;

    public static string Error_Size_Incompatible =>
        ResourceManager.GetString(nameof(Error_Size_Incompatible), Culture) ?? string.Empty;

    public static string MgCrit_Air_parent_null =>
        ResourceManager.GetString(nameof(MgCrit_Air_parent_null), Culture) ?? string.Empty;

    public static string MgCrit_Air_slide_parent_null =>
        ResourceManager.GetString(nameof(MgCrit_Air_slide_parent_null), Culture) ?? string.Empty;

    public static string MgCrit_Hold_has_no_tail =>
        ResourceManager.GetString(nameof(MgCrit_Hold_has_no_tail), Culture) ?? string.Empty;

    public static string MgCrit_Invalid_AirSlide_parent =>
        ResourceManager.GetString(nameof(MgCrit_Invalid_AirSlide_parent), Culture) ?? string.Empty;

    public static string MgCrit_Invalid_Air_parent =>
        ResourceManager.GetString(nameof(MgCrit_Invalid_Air_parent), Culture) ?? string.Empty;

    public static string MgCrit_Pairing_notes_incompatible =>
        ResourceManager.GetString(nameof(MgCrit_Pairing_notes_incompatible), Culture) ?? string.Empty;

    public static string MgCrit_SoflanArea_has_no_tail =>
        ResourceManager.GetString(nameof(MgCrit_SoflanArea_has_no_tail), Culture) ?? string.Empty;

    public static string MgCrit_Unrecognized_data_type =>
        ResourceManager.GetString(nameof(MgCrit_Unrecognized_data_type), Culture) ?? string.Empty;

    public static string MgCrit_Unrecognized_event =>
        ResourceManager.GetString(nameof(MgCrit_Unrecognized_event), Culture) ?? string.Empty;

    public static string Mg_Concurrent_ex_effects =>
        ResourceManager.GetString(nameof(Mg_Concurrent_ex_effects), Culture) ?? string.Empty;

    public static string Mg_Head_BPM_not_found =>
        ResourceManager.GetString(nameof(Mg_Head_BPM_not_found), Culture) ?? string.Empty;

    public static string Mg_Head_Time_Signature_event_not_found =>
        ResourceManager.GetString(nameof(Mg_Head_Time_Signature_event_not_found), Culture) ?? string.Empty;

    public static string Mg_Invalid_joint_type_note =>
        ResourceManager.GetString(nameof(Mg_Invalid_joint_type_note), Culture) ?? string.Empty;

    public static string Mg_Length_smaller_than_unit =>
        ResourceManager.GetString(nameof(Mg_Length_smaller_than_unit), Culture) ?? string.Empty;

    public static string Mg_Main_timeline_not_found =>
        ResourceManager.GetString(nameof(Mg_Main_timeline_not_found), Culture) ?? string.Empty;

    public static string Mg_Meta_Argument_count_min_one =>
        ResourceManager.GetString(nameof(Mg_Meta_Argument_count_min_one), Culture) ?? string.Empty;

    public static string Mg_Meta_First_argument_must_int =>
        ResourceManager.GetString(nameof(Mg_Meta_First_argument_must_int), Culture) ?? string.Empty;

    public static string Mg_Meta_Invalid_date =>
        ResourceManager.GetString(nameof(Mg_Meta_Invalid_date), Culture) ?? string.Empty;

    public static string Mg_Meta_Unknown_tag =>
        ResourceManager.GetString(nameof(Mg_Meta_Unknown_tag), Culture) ?? string.Empty;

    public static string Mg_No_sortname_provided =>
        ResourceManager.GetString(nameof(Mg_No_sortname_provided), Culture) ?? string.Empty;

    public static string Mg_Note_overlapped_in_different_TIL =>
        ResourceManager.GetString(nameof(Mg_Note_overlapped_in_different_TIL), Culture) ?? string.Empty;

    public static string Mg_Overlapping_air_parent_slide =>
        ResourceManager.GetString(nameof(Mg_Overlapping_air_parent_slide), Culture) ?? string.Empty;

    public static string Mg_String_id_not_found =>
        ResourceManager.GetString(nameof(Mg_String_id_not_found), Culture) ?? string.Empty;

    public static string Mg_Unrecognized_meta =>
        ResourceManager.GetString(nameof(Mg_Unrecognized_meta), Culture) ?? string.Empty;

    public static string Mg_Unrecognized_note =>
        ResourceManager.GetString(nameof(Mg_Unrecognized_note), Culture) ?? string.Empty;
}