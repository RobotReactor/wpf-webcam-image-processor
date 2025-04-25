using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public class MainWindowViewModel : BindableBase 
    {
        private string _title = "Webcam Image Processor";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
            // Constructor - waiting for Emgu.CV to be added before adding more functionality
        }
    }
}
