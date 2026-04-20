using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Infrastructure;
using PenguinTools.Resources;
using PenguinTools.Services;
using PenguinTools.ViewModels;
using PenguinTools.Views;

namespace PenguinTools;

public partial class App : Application
{
    public static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new InvalidOperationException("Failed to retrieve application name");

    public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version ??
                                             throw new InvalidOperationException(
                                                 "Failed to retrieve application version");

    public static readonly DateTime BuildDate = BuildDateAttribute.GetAssemblyBuildDate();

    internal static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, ex) =>
        {
            var errorWindow = new ExceptionWindow { StackTrace = ex.Exception.ToString() };
            errorWindow.ShowDialog();
            if (ex.Exception is OperationCanceledException or DiagnosticException) ex.Handled = true;
        };

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) Directory.SetCurrentDirectory(basePath);

        var services = new ServiceCollection();
        services.AddPenguinInfrastructure(Assembly.GetExecutingAssembly());

        services.AddTransient<IChartScanService, ChartScanService>();
        services.AddTransient<IOptionService, OptionService>();
        services.AddTransient<IMusicExportService, MusicExportService>();

        services.AddSingleton<IExternalLauncher, ShellExecuteLauncher>();
        services.AddSingleton<IUiSettingsService, UiSettingsService>();
        services.AddSingleton<IGameAssetService, GameAssetService>();
        services.AddSingleton<IFileDialogService>(sp =>
            new FileDialogService(new Lazy<MainWindow>(sp.GetRequiredService<MainWindow>)));
        services.AddSingleton<IDiagnosticsPresenter>(sp =>
            new DiagnosticsPresenter(new Lazy<MainWindow>(sp.GetRequiredService<MainWindow>)));
        services.AddSingleton<MainWindow>();
        services.AddSingleton(sp => new ActionService(sp.GetRequiredService<IDiagnosticsPresenter>()));
        services.AddSingleton<IReleaseService, GitHubReleaseService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MusicViewModel>();
        services.AddTransient<ChartViewModel>();
        services.AddTransient<JacketViewModel>();
        services.AddTransient<AudioViewModel>();
        services.AddTransient<StageViewModel>();
        services.AddTransient<MiscViewModel>();
        services.AddTransient<OptionViewModel>();

        services.AddTransient(sp => new OptionTab(sp.GetRequiredService<OptionViewModel>()));
        services.AddTransient(sp => new MusicTab(sp.GetRequiredService<MusicViewModel>()));
        services.AddTransient(sp => new ChartTab(sp.GetRequiredService<ChartViewModel>()));
        services.AddTransient(sp => new JacketTab(sp.GetRequiredService<JacketViewModel>()));
        services.AddTransient(sp => new AudioTab(sp.GetRequiredService<AudioViewModel>()));
        services.AddTransient(sp => new StageTab(sp.GetRequiredService<StageViewModel>()));
        services.AddTransient(sp => new MiscTab(sp.GetRequiredService<MiscViewModel>()));

        ServiceProvider = services.BuildServiceProvider();

        var uiSettings = ServiceProvider.GetRequiredService<IUiSettingsService>();
        try
        {
            await uiSettings.LoadAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        var assetManager = ServiceProvider.GetRequiredService<AssetManager>();
        var gameAssetService = ServiceProvider.GetRequiredService<IGameAssetService>();
        var hasConfiguredGameDirectory =
            !string.IsNullOrWhiteSpace(uiSettings.Settings.GameDirectory) &&
            Directory.Exists(uiSettings.Settings.GameDirectory);
        var shouldAutoCollectOnStartup = hasConfiguredGameDirectory;

        if (assetManager.ShouldPromptForOptionalAssetsImport && !hasConfiguredGameDirectory)
        {
            var explanation = string.Format(
                CultureInfo.CurrentCulture,
                Strings.UserAssetSetup_Body,
                assetManager.PlusAssetsPath);
            var setupWindow = new UserAssetSetupWindow();
            setupWindow.DataContext = new UserAssetSetupViewModel(gameAssetService, setupWindow, explanation);
            setupWindow.ShowDialog();
        }

        var window = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();

        if (shouldAutoCollectOnStartup) _ = gameAssetService.AutoCollectAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ServiceProvider is IDisposable disposable) disposable.Dispose();
        base.OnExit(e);
    }
}
