using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;
using WhiteScan.Services;
using WhiteScan.ViewModels;
using System.Threading.Tasks;

namespace WhiteScan
{

    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Debug.WriteLine("🚀 WhiteScan Application Starting...");
            Debug.WriteLine("🔧 Configuring services...");
            
            ConfigureServices();
            
            Debug.WriteLine("✅ Services configured successfully");
            Debug.WriteLine("🪟 Creating main window...");
            
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow?.Show();
            
            Debug.WriteLine("✅ Main window created and shown");
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<INetworkScannerService, NetworkScannerService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}

