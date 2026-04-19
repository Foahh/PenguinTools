using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Infrastructure;
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
        services.AddPenguinInfrastructure(typeof(EmbeddedResourceStore).Assembly);

        services.AddTransient<IChartScanService, ChartScanService>();
        services.AddTransient<IOptionService, OptionService>();
        services.AddTransient<IWorkflowExportService, WorkflowExportService>();

        services.AddSingleton<IExternalLauncher, ShellExecuteLauncher>();
        services.AddSingleton<IFileDialogService>(sp =>
            new FileDialogService(new Lazy<MainWindow>(() => sp.GetRequiredService<MainWindow>())));
        services.AddSingleton<IDiagnosticsPresenter>(sp =>
            new DiagnosticsPresenter(new Lazy<MainWindow>(() => sp.GetRequiredService<MainWindow>())));
        services.AddSingleton<MainWindow>();
        services.AddSingleton(sp => new ActionService(sp.GetRequiredService<IDiagnosticsPresenter>()));
        services.AddSingleton<IReleaseService, GitHubReleaseService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<WorkflowViewModel>();
        services.AddTransient<ChartViewModel>();
        services.AddTransient<JacketViewModel>();
        services.AddTransient<AudioViewModel>();
        services.AddTransient<StageViewModel>();
        services.AddTransient<MiscViewModel>();
        services.AddTransient<OptionViewModel>();

        services.AddTransient(sp => new OptionTab(sp.GetRequiredService<OptionViewModel>()));
        services.AddTransient(sp => new WorkflowTab(sp.GetRequiredService<WorkflowViewModel>()));
        services.AddTransient(sp => new ChartTab(sp.GetRequiredService<ChartViewModel>()));
        services.AddTransient(sp => new JacketTab(sp.GetRequiredService<JacketViewModel>()));
        services.AddTransient(sp => new AudioTab(sp.GetRequiredService<AudioViewModel>()));
        services.AddTransient(sp => new StageTab(sp.GetRequiredService<StageViewModel>()));
        services.AddTransient(sp => new MiscTab(sp.GetRequiredService<MiscViewModel>()));

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
