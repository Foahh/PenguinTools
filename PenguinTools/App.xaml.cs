using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Media;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) { Directory.SetCurrentDirectory(basePath); }

        var services = new ServiceCollection();
        var resources = new EmbeddedResourceStore(Assembly.GetExecutingAssembly());

        services.AddSingleton<MainWindow>();
        services.AddSingleton<ActionService>();
        services.AddSingleton<IEmbeddedResourceStore>(resources);
        services.AddSingleton<IMediaTool>(_ =>
        {
            if (resources.HasResource("mua.exe"))
            {
                var toolPath = resources.ExtractToTemp("mua.exe");
                return new MuaMediaTool(toolPath);
            }

            if (resources.HasResource("mua"))
            {
                var toolPath = resources.ExtractToTemp("mua");
                return new MuaMediaTool(toolPath);
            }

            return new MuaMediaTool("mua");
        });
        services.AddSingleton<AssetManager>(_ =>
        {
            using var stream = resources.OpenRead("assets.json");
            return new AssetManager(stream);
        });
        services.AddSingleton<IReleaseService, GitHubReleaseService>();

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

        DispatcherUnhandledException += (_, ex) =>
        {
            var errorWindow = new ExceptionWindow { StackTrace = ex.Exception.ToString() };
            errorWindow.ShowDialog();
            if (ex.Exception is OperationCanceledException or DiagnosticException) { ex.Handled = true; }
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ServiceProvider is IDisposable disposable) disposable.Dispose();
        base.OnExit(e);
    }
}
