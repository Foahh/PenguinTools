using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Services;
using PenguinTools.ViewModels;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace PenguinTools;

public partial class App : Application
{
    public static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("Failed to retrieve application name");
    public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException("Failed to retrieve application version");
    public static readonly DateTime BuildDate = BuildDateAttribute.GetAssemblyBuildDate();

    internal static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) Directory.SetCurrentDirectory(basePath);

        ResourceUtils.Initialize();

        var services = new ServiceCollection();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<ActionService>();
        services.AddSingleton<AssetManager>();
        services.AddSingleton<IUpdateService, GitHubUpdateService>();
        
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<WorkflowViewModel>();
        services.AddTransient<ChartViewModel>();
        services.AddTransient<JacketViewModel>();
        services.AddTransient<MusicViewModel>();
        services.AddTransient<StageViewModel>();
        services.AddTransient<MiscViewModel>();
        services.AddTransient<OptionViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        var window = ServiceProvider.GetRequiredService<MainWindow>();
        window.Show();

        DispatcherUnhandledException += (s, ex) =>
        {
            var errorWindow = new ExceptionWindow { StackTrace = ex.Exception.ToString() };
            errorWindow.ShowDialog();
            if (ex.Exception is OperationCanceledException or DiagnosticException) ex.Handled = true;
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ResourceUtils.Release();
    }
}