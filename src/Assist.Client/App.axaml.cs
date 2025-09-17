using System;
using Assist.Client.ViewModels;
using Assist.Core.Extensions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Assist.Client;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _serviceProvider?.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddAssistCore();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<StoreViewModel>();
        services.AddSingleton<LiveViewModel>();
        services.AddSingleton<ModulesViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }
}
