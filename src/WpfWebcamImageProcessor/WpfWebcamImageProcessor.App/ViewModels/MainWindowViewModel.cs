using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace WpfWebcamImageProcessor.App.ViewModels
{
    /// <summary>
    /// Provides the data context and logic for the main application window (<see cref="Views.MainWindow"/>).
    /// Handles overall application state and orchestrates interactions between services and the view.
    /// </summary>
    public class MainWindowViewModel : BindableBase 
    {
        private string _title = "Webcam Image Processor";

        /// <summary>
        /// Gets or sets the title displayed in the main application window's title bar.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            // Constructor - waiting for Emgu.CV to be added before adding more functionality
        }
    }
}
