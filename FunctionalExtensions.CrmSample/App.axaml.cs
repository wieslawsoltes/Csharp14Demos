using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FunctionalExtensions.CrmSample.Runtime;
using FunctionalExtensions.CrmSample.ViewModels;
using FunctionalExtensions.CrmSample.Views;

namespace FunctionalExtensions.CrmSample;

public sealed partial class App : Application
{
    private CrmEnvironment? _environment;

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _environment ??= CrmBootstrapper.BuildEnvironment();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MainViewModel(_environment);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            desktop.Exit += async (_, _) =>
            {
                await mainViewModel.DisposeAsync().ConfigureAwait(false);
                await _environment.DisposeAsync().ConfigureAwait(false);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
