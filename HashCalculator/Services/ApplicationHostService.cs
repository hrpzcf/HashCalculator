using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HashCalculator.Views.Pages;
using HashCalculator.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HashCalculator.Services;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.NavigationView.Navigate(typeof(HomePage));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Application.Current.Windows.OfType<MainWindow>().Any())
        {
            return Task.CompletedTask;
        }
        MainWindow mainWindow = this._serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Loaded += this.OnMainWindowLoaded;
        mainWindow.Show();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
