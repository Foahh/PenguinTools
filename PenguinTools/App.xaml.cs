using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    private IHost _host = null!;
    internal new static Window MainWindow => Services.GetRequiredService<MainWindow>();
    internal static IServiceProvider Services => ((App)Current)._host.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) Directory.SetCurrentDirectory(basePath);

        ResourceUtils.Initialize();

        _host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
        {
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
        }).Build();

        _host.Start();

        var window = Services.GetRequiredService<MainWindow>();
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
        _host.Dispose();
    }
}