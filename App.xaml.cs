using LibraryManager.Services.FileManagement;
using LibraryManager.Services.FileStorage;
using LibraryManager.Services.PdfPreview;
using LibraryManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace LibraryManager
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow(ServiceProvider.GetRequiredService<MainViewModel>());

            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            services.AddSingleton<IPdfFileManager, LocalPdfFileManager>();
            services.AddSingleton<IPdfViewerService, PdfViewerService>();
            services.AddSingleton<MainViewModel>();
        }
    }
}
