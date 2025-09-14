using System.Windows;
using _.Services;
using Microsoft.Extensions.DependencyInjection;

namespace _;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        
        services.AddSingleton<MonitoredProcessService>();
        services.AddSingleton<ProcessAffinityService>();
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<CcdService>();
        services.AddTransient<ViewModels.AddProcessViewModel>();
        services.AddTransient<ViewModels.DefaultCcdViewModel>();
        services.AddSingleton<ProcessMonitorService>();

        var serviceProvider = services.BuildServiceProvider();
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}

