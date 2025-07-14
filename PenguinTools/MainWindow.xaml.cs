using PenguinTools.Core.Resources;
using PenguinTools.ViewModels;
using System.Windows;

namespace PenguinTools;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        this.viewModel = viewModel;
        Title = string.Format(Strings.Window_Title, App.Name, App.Version.ToString(3));
        Loaded += OnLoaded;
    }

    private void OnLoaded(object s, RoutedEventArgs e)
    {
        _ = viewModel.UpdateCheck();
    }
}