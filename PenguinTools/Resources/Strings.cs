using System.Globalization;
using System.Resources;

namespace PenguinTools.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager =>
        resourceMan ??= new ResourceManager("PenguinTools.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string Alert_Diagnostic_report =>
        ResourceManager.GetString(nameof(Alert_Diagnostic_report), Culture) ?? string.Empty;

    public static string Alert_Edit_notice =>
        ResourceManager.GetString(nameof(Alert_Edit_notice), Culture) ?? string.Empty;

    public static string Alert_File_changed =>
        ResourceManager.GetString(nameof(Alert_File_changed), Culture) ?? string.Empty;

    public static string Button_Clear => ResourceManager.GetString(nameof(Button_Clear), Culture) ?? string.Empty;
    public static string Button_Close => ResourceManager.GetString(nameof(Button_Close), Culture) ?? string.Empty;
    public static string Button_Convert => ResourceManager.GetString(nameof(Button_Convert), Culture) ?? string.Empty;
    public static string Button_Copy => ResourceManager.GetString(nameof(Button_Copy), Culture) ?? string.Empty;

    public static string Button_Copy_Exception =>
        ResourceManager.GetString(nameof(Button_Copy_Exception), Culture) ?? string.Empty;

    public static string Button_Extract_dds_from_afb =>
        ResourceManager.GetString(nameof(Button_Extract_dds_from_afb), Culture) ?? string.Empty;

    public static string Button_Generate => ResourceManager.GetString(nameof(Button_Generate), Culture) ?? string.Empty;
    public static string Button_Help => ResourceManager.GetString(nameof(Button_Help), Culture) ?? string.Empty;

    public static string Button_Open_temp_directory =>
        ResourceManager.GetString(nameof(Button_Open_temp_directory), Culture) ?? string.Empty;

    public static string Button_Recollect_A000 =>
        ResourceManager.GetString(nameof(Button_Recollect_A000), Culture) ?? string.Empty;

    public static string Button_Reload => ResourceManager.GetString(nameof(Button_Reload), Culture) ?? string.Empty;
    public static string Category_BGM => ResourceManager.GetString(nameof(Category_BGM), Culture) ?? string.Empty;
    public static string Category_Chart => ResourceManager.GetString(nameof(Category_Chart), Culture) ?? string.Empty;

    public static string Category_Display =>
        ResourceManager.GetString(nameof(Category_Display), Culture) ?? string.Empty;

    public static string Category_Misc => ResourceManager.GetString(nameof(Category_Misc), Culture) ?? string.Empty;

    public static string Category_Settings =>
        ResourceManager.GetString(nameof(Category_Settings), Culture) ?? string.Empty;

    public static string Category_Song => ResourceManager.GetString(nameof(Category_Song), Culture) ?? string.Empty;
    public static string Category_Sync => ResourceManager.GetString(nameof(Category_Sync), Culture) ?? string.Empty;

    public static string Description_BackgroundStage =>
        ResourceManager.GetString(nameof(Description_BackgroundStage), Culture) ?? string.Empty;

    public static string Description_BgmFile =>
        ResourceManager.GetString(nameof(Description_BgmFile), Culture) ?? string.Empty;

    public static string Description_BgmInitialBpm =>
        ResourceManager.GetString(nameof(Description_BgmInitialBpm), Culture) ?? string.Empty;

    public static string Description_BgmInitialTimeSignature =>
        ResourceManager.GetString(nameof(Description_BgmInitialTimeSignature), Culture) ?? string.Empty;

    public static string Description_DisplayBPM =>
        ResourceManager.GetString(nameof(Description_DisplayBPM), Culture) ?? string.Empty;

    public static string Description_Genre =>
        ResourceManager.GetString(nameof(Description_Genre), Culture) ?? string.Empty;

    public static string Description_InsertBlankMeasure =>
        ResourceManager.GetString(nameof(Description_InsertBlankMeasure), Culture) ?? string.Empty;

    public static string Description_MainDifficulty =>
        ResourceManager.GetString(nameof(Description_MainDifficulty), Culture) ?? string.Empty;

    public static string Description_MainTil =>
        ResourceManager.GetString(nameof(Description_MainTil), Culture) ?? string.Empty;

    public static string Description_ManualOffset =>
        ResourceManager.GetString(nameof(Description_ManualOffset), Culture) ?? string.Empty;

    public static string Description_RealOffset =>
        ResourceManager.GetString(nameof(Description_RealOffset), Culture) ?? string.Empty;

    public static string Description_SongId =>
        ResourceManager.GetString(nameof(Description_SongId), Culture) ?? string.Empty;

    public static string Description_SortName =>
        ResourceManager.GetString(nameof(Description_SortName), Culture) ?? string.Empty;

    public static string Description_UnlockEventID =>
        ResourceManager.GetString(nameof(Description_UnlockEventID), Culture) ?? string.Empty;

    public static string Description_UnlockEventIDOption =>
        ResourceManager.GetString(nameof(Description_UnlockEventIDOption), Culture) ?? string.Empty;

    public static string Description_ChartFileDiscovery =>
        ResourceManager.GetString(nameof(Description_ChartFileDiscovery), Culture) ?? string.Empty;

    public static string Display_Artist => ResourceManager.GetString(nameof(Display_Artist), Culture) ?? string.Empty;

    public static string Display_BackgroundFile =>
        ResourceManager.GetString(nameof(Display_BackgroundFile), Culture) ?? string.Empty;

    public static string Display_BackgroundStage =>
        ResourceManager.GetString(nameof(Display_BackgroundStage), Culture) ?? string.Empty;

    public static string Display_BatchSize =>
        ResourceManager.GetString(nameof(Display_BatchSize), Culture) ?? string.Empty;

    public static string Display_BgmFile => ResourceManager.GetString(nameof(Display_BgmFile), Culture) ?? string.Empty;

    public static string Display_BgmInitialBpm =>
        ResourceManager.GetString(nameof(Display_BgmInitialBpm), Culture) ?? string.Empty;

    public static string Display_BgmInitialTimeSignature =>
        ResourceManager.GetString(nameof(Display_BgmInitialTimeSignature), Culture) ?? string.Empty;

    public static string Display_BgmPreviewStart =>
        ResourceManager.GetString(nameof(Display_BgmPreviewStart), Culture) ?? string.Empty;

    public static string Display_BgmPreviewStop =>
        ResourceManager.GetString(nameof(Display_BgmPreviewStop), Culture) ?? string.Empty;

    public static string Display_ConvertAudio =>
        ResourceManager.GetString(nameof(Display_ConvertAudio), Culture) ?? string.Empty;

    public static string Display_ConvertBackground =>
        ResourceManager.GetString(nameof(Display_ConvertBackground), Culture) ?? string.Empty;

    public static string Display_ConvertChart =>
        ResourceManager.GetString(nameof(Display_ConvertChart), Culture) ?? string.Empty;

    public static string Display_ChartFileDiscovery =>
        ResourceManager.GetString(nameof(Display_ChartFileDiscovery), Culture) ?? string.Empty;

    public static string Display_ConvertJacket =>
        ResourceManager.GetString(nameof(Display_ConvertJacket), Culture) ?? string.Empty;

    public static string Display_Denominator =>
        ResourceManager.GetString(nameof(Display_Denominator), Culture) ?? string.Empty;

    public static string Display_Designer =>
        ResourceManager.GetString(nameof(Display_Designer), Culture) ?? string.Empty;

    public static string Display_Difficulty =>
        ResourceManager.GetString(nameof(Display_Difficulty), Culture) ?? string.Empty;

    public static string Display_DisplayBPM =>
        ResourceManager.GetString(nameof(Display_DisplayBPM), Culture) ?? string.Empty;

    public static string Display_FilePath =>
        ResourceManager.GetString(nameof(Display_FilePath), Culture) ?? string.Empty;

    public static string Display_GenerateEventXml =>
        ResourceManager.GetString(nameof(Display_GenerateEventXml), Culture) ?? string.Empty;

    public static string Display_Genre => ResourceManager.GetString(nameof(Display_Genre), Culture) ?? string.Empty;

    public static string Display_InsertBlankMeasure =>
        ResourceManager.GetString(nameof(Display_InsertBlankMeasure), Culture) ?? string.Empty;

    public static string Display_IsCustomStage =>
        ResourceManager.GetString(nameof(Display_IsCustomStage), Culture) ?? string.Empty;

    public static string Display_JacketFile =>
        ResourceManager.GetString(nameof(Display_JacketFile), Culture) ?? string.Empty;

    public static string Display_LastExport =>
        ResourceManager.GetString(nameof(Display_LastExport), Culture) ?? string.Empty;

    public static string Display_Level => ResourceManager.GetString(nameof(Display_Level), Culture) ?? string.Empty;

    public static string Display_MainDifficulty =>
        ResourceManager.GetString(nameof(Display_MainDifficulty), Culture) ?? string.Empty;

    public static string Display_MainTil => ResourceManager.GetString(nameof(Display_MainTil), Culture) ?? string.Empty;

    public static string Display_ManualOffset =>
        ResourceManager.GetString(nameof(Display_ManualOffset), Culture) ?? string.Empty;

    public static string Display_NotesFieldLine =>
        ResourceManager.GetString(nameof(Display_NotesFieldLine), Culture) ?? string.Empty;

    public static string Display_Numerator =>
        ResourceManager.GetString(nameof(Display_Numerator), Culture) ?? string.Empty;

    public static string Display_OptionName =>
        ResourceManager.GetString(nameof(Display_OptionName), Culture) ?? string.Empty;

    public static string Display_RealOffset =>
        ResourceManager.GetString(nameof(Display_RealOffset), Culture) ?? string.Empty;

    public static string Display_ReleaseDate =>
        ResourceManager.GetString(nameof(Display_ReleaseDate), Culture) ?? string.Empty;

    public static string Display_ReleaseTagXml =>
        ResourceManager.GetString(nameof(Display_ReleaseTagXml), Culture) ?? string.Empty;

    public static string Display_SongId => ResourceManager.GetString(nameof(Display_SongId), Culture) ?? string.Empty;

    public static string Display_SortName =>
        ResourceManager.GetString(nameof(Display_SortName), Culture) ?? string.Empty;

    public static string Display_StageId => ResourceManager.GetString(nameof(Display_StageId), Culture) ?? string.Empty;
    public static string Display_Title => ResourceManager.GetString(nameof(Display_Title), Culture) ?? string.Empty;

    public static string Display_UltimaEventId =>
        ResourceManager.GetString(nameof(Display_UltimaEventId), Culture) ?? string.Empty;

    public static string Display_UnlockEventID =>
        ResourceManager.GetString(nameof(Display_UnlockEventID), Culture) ?? string.Empty;

    public static string Display_WeEventId =>
        ResourceManager.GetString(nameof(Display_WeEventId), Culture) ?? string.Empty;

    public static string Display_WorldsEndDifficulty =>
        ResourceManager.GetString(nameof(Display_WorldsEndDifficulty), Culture) ?? string.Empty;

    public static string Display_WorldsEndTag =>
        ResourceManager.GetString(nameof(Display_WorldsEndTag), Culture) ?? string.Empty;

    public static string Error_Audio_file_not_found =>
        ResourceManager.GetString(nameof(Error_Audio_file_not_found), Culture) ?? string.Empty;

    public static string Error_Background_file_is_not_set =>
        ResourceManager.GetString(nameof(Error_Background_file_is_not_set), Culture) ?? string.Empty;

    public static string Error_File_ignored_due_to_id_missing =>
        ResourceManager.GetString(nameof(Error_File_ignored_due_to_id_missing), Culture) ?? string.Empty;

    public static string Error_Jacket_file_not_found =>
        ResourceManager.GetString(nameof(Error_Jacket_file_not_found), Culture) ?? string.Empty;

    public static string Error_No_charts_are_found_directory =>
        ResourceManager.GetString(nameof(Error_No_charts_are_found_directory), Culture) ?? string.Empty;

    public static string Error_Noop => ResourceManager.GetString(nameof(Error_Noop), Culture) ?? string.Empty;

    public static string Error_Song_id_is_not_set =>
        ResourceManager.GetString(nameof(Error_Song_id_is_not_set), Culture) ?? string.Empty;

    public static string Error_Stage_id_is_not_set =>
        ResourceManager.GetString(nameof(Error_Stage_id_is_not_set), Culture) ?? string.Empty;

    public static string Error_Unhandled => ResourceManager.GetString(nameof(Error_Unhandled), Culture) ?? string.Empty;
    public static string Filefilter_afb => ResourceManager.GetString(nameof(Filefilter_afb), Culture) ?? string.Empty;
    public static string Filefilter_c2s => ResourceManager.GetString(nameof(Filefilter_c2s), Culture) ?? string.Empty;
    public static string Filefilter_dds => ResourceManager.GetString(nameof(Filefilter_dds), Culture) ?? string.Empty;

    public static string Filefilter_image =>
        ResourceManager.GetString(nameof(Filefilter_image), Culture) ?? string.Empty;

    public static string Filefilter_mgxc => ResourceManager.GetString(nameof(Filefilter_mgxc), Culture) ?? string.Empty;

    public static string Filefilter_sound =>
        ResourceManager.GetString(nameof(Filefilter_sound), Culture) ?? string.Empty;

    public static string Label_Audio => ResourceManager.GetString(nameof(Label_Audio), Culture) ?? string.Empty;

    public static string Label_Background =>
        ResourceManager.GetString(nameof(Label_Background), Culture) ?? string.Empty;

    public static string Label_Chart => ResourceManager.GetString(nameof(Label_Chart), Culture) ?? string.Empty;
    public static string Label_Details => ResourceManager.GetString(nameof(Label_Details), Culture) ?? string.Empty;
    public static string Label_Effects => ResourceManager.GetString(nameof(Label_Effects), Culture) ?? string.Empty;
    public static string Label_Folder => ResourceManager.GetString(nameof(Label_Folder), Culture) ?? string.Empty;
    public static string Label_Image => ResourceManager.GetString(nameof(Label_Image), Culture) ?? string.Empty;
    public static string Label_Jacket_ID => ResourceManager.GetString(nameof(Label_Jacket_ID), Culture) ?? string.Empty;
    public static string Label_Message => ResourceManager.GetString(nameof(Label_Message), Culture) ?? string.Empty;

    public static string Label_Not_Available =>
        ResourceManager.GetString(nameof(Label_Not_Available), Culture) ?? string.Empty;

    public static string Label_NotesField_Line_Color =>
        ResourceManager.GetString(nameof(Label_NotesField_Line_Color), Culture) ?? string.Empty;

    public static string Label_Parameters =>
        ResourceManager.GetString(nameof(Label_Parameters), Culture) ?? string.Empty;

    public static string Label_Path => ResourceManager.GetString(nameof(Label_Path), Culture) ?? string.Empty;
    public static string Label_Preview => ResourceManager.GetString(nameof(Label_Preview), Culture) ?? string.Empty;

    public static string Label_Properties =>
        ResourceManager.GetString(nameof(Label_Properties), Culture) ?? string.Empty;

    public static string Label_Settings => ResourceManager.GetString(nameof(Label_Settings), Culture) ?? string.Empty;
    public static string Label_Stage_ID => ResourceManager.GetString(nameof(Label_Stage_ID), Culture) ?? string.Empty;
    public static string Label_Time => ResourceManager.GetString(nameof(Label_Time), Culture) ?? string.Empty;

    public static string Link_Documentation =>
        ResourceManager.GetString(nameof(Link_Documentation), Culture) ?? string.Empty;

    public static string Status_Checked => ResourceManager.GetString(nameof(Status_Checked), Culture) ?? string.Empty;

    public static string Status_Converted =>
        ResourceManager.GetString(nameof(Status_Converted), Culture) ?? string.Empty;

    public static string Status_Done => ResourceManager.GetString(nameof(Status_Done), Culture) ?? string.Empty;
    public static string Status_Error => ResourceManager.GetString(nameof(Status_Error), Culture) ?? string.Empty;
    public static string Status_Idle => ResourceManager.GetString(nameof(Status_Idle), Culture) ?? string.Empty;
    public static string Status_Reading => ResourceManager.GetString(nameof(Status_Reading), Culture) ?? string.Empty;

    public static string Status_Searching =>
        ResourceManager.GetString(nameof(Status_Searching), Culture) ?? string.Empty;

    public static string Status_Starting => ResourceManager.GetString(nameof(Status_Starting), Culture) ?? string.Empty;
    public static string Tab_Chart => ResourceManager.GetString(nameof(Tab_Chart), Culture) ?? string.Empty;
    public static string Tab_Jacket => ResourceManager.GetString(nameof(Tab_Jacket), Culture) ?? string.Empty;
    public static string Tab_Misc => ResourceManager.GetString(nameof(Tab_Misc), Culture) ?? string.Empty;
    public static string Tab_Audio => ResourceManager.GetString(nameof(Tab_Audio), Culture) ?? string.Empty;
    public static string Tab_Option => ResourceManager.GetString(nameof(Tab_Option), Culture) ?? string.Empty;
    public static string Tab_Song => ResourceManager.GetString(nameof(Tab_Song), Culture) ?? string.Empty;
    public static string Tab_Stage => ResourceManager.GetString(nameof(Tab_Stage), Culture) ?? string.Empty;

    public static string Title_Diagnostics =>
        ResourceManager.GetString(nameof(Title_Diagnostics), Culture) ?? string.Empty;

    public static string Title_Error => ResourceManager.GetString(nameof(Title_Error), Culture) ?? string.Empty;

    public static string Title_Select_the_game_folder =>
        ResourceManager.GetString(nameof(Title_Select_the_game_folder), Culture) ?? string.Empty;

    public static string Title_Select_the_input_file =>
        ResourceManager.GetString(nameof(Title_Select_the_input_file), Culture) ?? string.Empty;

    public static string Title_Select_the_output_folder =>
        ResourceManager.GetString(nameof(Title_Select_the_output_folder), Culture) ?? string.Empty;

    public static string Update_Already_Latest =>
        ResourceManager.GetString(nameof(Update_Already_Latest), Culture) ?? string.Empty;

    public static string Update_Checking => ResourceManager.GetString(nameof(Update_Checking), Culture) ?? string.Empty;
    public static string Update_Failed => ResourceManager.GetString(nameof(Update_Failed), Culture) ?? string.Empty;

    public static string Update_New_Version_Available =>
        ResourceManager.GetString(nameof(Update_New_Version_Available), Culture) ?? string.Empty;

    public static string Update_Tooltip => ResourceManager.GetString(nameof(Update_Tooltip), Culture) ?? string.Empty;

    public static string UserAssetSetup_Body =>
        ResourceManager.GetString(nameof(UserAssetSetup_Body), Culture) ?? string.Empty;

    public static string UserAssetSetup_Browse_install =>
        ResourceManager.GetString(nameof(UserAssetSetup_Browse_install), Culture) ?? string.Empty;

    public static string UserAssetSetup_Processing =>
        ResourceManager.GetString(nameof(UserAssetSetup_Processing), Culture) ?? string.Empty;

    public static string UserAssetSetup_Skip =>
        ResourceManager.GetString(nameof(UserAssetSetup_Skip), Culture) ?? string.Empty;

    public static string UserAssetSetup_Title =>
        ResourceManager.GetString(nameof(UserAssetSetup_Title), Culture) ?? string.Empty;

    public static string Warn_Duplicate_id_and_difficulty =>
        ResourceManager.GetString(nameof(Warn_Duplicate_id_and_difficulty), Culture) ?? string.Empty;

    public static string Warn_More_than_one_chart_marked_main =>
        ResourceManager.GetString(nameof(Warn_More_than_one_chart_marked_main), Culture) ?? string.Empty;

    public static string Warn_No_chart_marked_main =>
        ResourceManager.GetString(nameof(Warn_No_chart_marked_main), Culture) ?? string.Empty;

    public static string Warn_We_chart_must_be_unique_id =>
        ResourceManager.GetString(nameof(Warn_We_chart_must_be_unique_id), Culture) ?? string.Empty;

    public static string Window_Title => ResourceManager.GetString(nameof(Window_Title), Culture) ?? string.Empty;
}