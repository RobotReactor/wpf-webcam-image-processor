using System.Windows;

namespace WpfWebcamImageProcessor.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// This is the main window of the application.
    /// In an MVVM application using Prism's ViewModelLocator,
    /// the primary role of the code-behind is typically just to call InitializeComponent().
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
