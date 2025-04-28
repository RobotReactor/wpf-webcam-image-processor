using Prism.DryIoc;
using Prism.Ioc;
using System.Configuration;
using System.Data;
using System.Windows;
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Views;

namespace WpfWebcamImageProcessor.App
{
    /// <summary>
    /// Provides the application's entry point and sets up the Prism framework infrastructure,
    /// including the dependency injection container (DryIoc) and initial window creation.
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>
        /// Registers types (services, views, viewmodels) with the Prism dependency injection container.
        /// </summary>
        /// <param name="containerRegistry">The container registry provided by Prism.</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IImageProcessingService, ImageProcessingService>();
            containerRegistry.RegisterSingleton<ICameraService, CameraService>();
            containerRegistry.RegisterSingleton<IHistogramService, HistogramService>();
            containerRegistry.Register<IImageProcessingWorkflowService, ImageProcessingWorkflowService>();
        }

        /// <summary>
        /// Creates and returns the main window (Shell) of the application.
        /// </summary>
        /// <returns>The main application window instance.</returns>
        /// <remarks>
        /// This method is called by Prism after <see cref="RegisterTypes"/> has completed.
        /// It resolves the main window type from the container, allowing dependencies to be injected.
        /// </remarks>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

    }
}
