using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace PenguinTools.Controls;

public enum PickerMode
{
    File,
    Folder
}

public partial class FileFolderPicker : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(FileFolderPicker), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(string), typeof(FileFolderPicker), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(nameof(Filter), typeof(string), typeof(FileFolderPicker), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(FileFolderPicker), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FileFolderPicker), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(PickerMode), typeof(FileFolderPicker), new FrameworkPropertyMetadata(PickerMode.File, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public FileFolderPicker()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public string Filter
    {
        get => (string)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public PickerMode Mode
    {
        get => (PickerMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    [RelayCommand]
    private void BrowseFile()
    {
        string? path;
        bool? result;

        if (Mode == PickerMode.File)
        {
            var dlg = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
                CheckFileExists = true,
                AddExtension = true,
                ValidateNames = true
            };
            result = dlg.ShowDialog(App.MainWindow);
            path = dlg.FileName;
        }
        else if (Mode == PickerMode.Folder)
        {
            var dlg = new OpenFolderDialog
            {
                Title = Title,
                ValidateNames = true,
                Multiselect = false
            };
            result = dlg.ShowDialog(App.MainWindow);
            path = dlg.FolderName;
        }
        else throw new InvalidOperationException("Invalid PickerMode");

        if (result is not true || string.IsNullOrWhiteSpace(path)) return;
        Path = path;
    }
}