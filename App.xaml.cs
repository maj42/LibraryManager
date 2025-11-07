using LibraryManager.Services.Dialogs;
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
            // Services
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            services.AddSingleton<IPdfFileManager, LocalPdfFileManager>();
            services.AddSingleton<IPdfViewerService, PdfViewerService>();
            services.AddSingleton<IDialogService, DialogService>();
            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<PreviewViewModel>();
            // Windows
            services.AddTransient<MainWindow>();
        }
    }
}
