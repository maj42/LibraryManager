using LibraryManager.ViewModels;
using LibraryManager.Services;
using Microsoft.Extensions.DependencyInjection;
using LibraryManager.Services.FileStorage;
using System;
using System.Windows;
using LibraryManager.Services.FileManagement;

namespace LibraryManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };

            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            services.AddSingleton<IPdfFileManager, LocalPdfFileManager>();
            services.AddSingleton<MainViewModel>();
        }
    }
}
