using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Resources;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        Title = string.Format(Strings.Window_Title, App.Name, App.Version.ToString(3));
        Loaded += OnLoaded;

        OptionTabHost.Content = services.GetRequiredService<OptionTab>();
        MusicTabHost.Content = services.GetRequiredService<MusicTab>();
        ChartTabHost.Content = services.GetRequiredService<ChartTab>();
        JacketTabHost.Content = services.GetRequiredService<JacketTab>();
        AudioTabHost.Content = services.GetRequiredService<AudioTab>();
        StageTabHost.Content = services.GetRequiredService<StageTab>();
        MiscTabHost.Content = services.GetRequiredService<MiscTab>();
    }

    private void OnLoaded(object s, RoutedEventArgs e)
    {
        _ = _viewModel.UpdateCheck();
    }
}
