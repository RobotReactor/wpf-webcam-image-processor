using Prism.DryIoc;
using Prism.Ioc;
using System.Configuration;
using System.Data;
using System.Windows;
using WpfWebcamImageProcessor.App.Views;

namespace WpfWebcamImageProcessor.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// 
    /// Addressing errors in next commit
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainWindow>();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

    }

}
