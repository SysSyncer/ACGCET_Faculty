using System.IO;
using System.Windows;
using ACGCET_Faculty.Models;
using ACGCET_Faculty.ViewModels.Dashboard;
using ACGCET_Faculty.Views.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ACGCET_Faculty
{
    public partial class App : Application
    {
        private readonly IHost? _host;

        public App()
        {
            try
            {
                _host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.SetBasePath(AppContext.BaseDirectory);
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Prefer build-time injected secret (release builds); fall back to appsettings.json (local dev)
                        string cs = !string.IsNullOrEmpty(Services.AppSecrets.ConnectionString)
                                    && !Services.AppSecrets.ConnectionString.Contains("##")
                            ? Services.AppSecrets.ConnectionString
                            : context.Configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string not found.");

                        // Singleton DbContext shared across Faculty session — one user, one context.
                        // All navigation ViewModels receive the same context instance from DI.
                        services.AddDbContext<FacultyDbContext>(options =>
                            options.UseSqlServer(cs),
                            contextLifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
                            optionsLifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);

                        // ViewModels
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<LoginViewModel>();
                        services.AddTransient<FacultyDashboardViewModel>();

                        // Main Window
                        services.AddSingleton<MainWindow>(provider => new MainWindow
                        {
                            DataContext = provider.GetRequiredService<MainViewModel>()
                        });

                        services.AddTransient<LoginView>();
                        services.AddTransient<FacultyDashboardView>();
                    })
                    .Build();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}\n{ex.StackTrace}", "Critical Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                if (_host != null)
                {
                    await _host.StartAsync();
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    this.MainWindow = mainWindow;
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}\n{ex.StackTrace}", "Critical Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                using (_host) { await _host.StopAsync(); }
            }
            base.OnExit(e);
        }
    }
}
