using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.Windows; 
using WpfWebcamImageProcessor.App.Services; 

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable 
    {
        private readonly IImageProcessingService _imageProcessingService;
        private bool _isDisposed = false; 

        private string _title = "Webcam Image Processor";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        // Property to hold the original image Bitmap
        private Bitmap? _originalBitmap;
        public Bitmap? OriginalBitmap
        {
            get { return _originalBitmap; }
            set { SetProperty(ref _originalBitmap, value); }
        }

        // Property to hold the grayscale image Bitmap
        private Bitmap? _grayscaleBitmap;
        public Bitmap? GrayscaleBitmap
        {
            get { return _grayscaleBitmap; }
            set { SetProperty(ref _grayscaleBitmap, value); }
        }

        // Command bound to the Button in the UI
        public DelegateCommand ProcessImageCommand { get; private set; }

        // Constructor: Inject services here
        public MainWindowViewModel(IImageProcessingService imageProcessingService)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));

            // Initialize the command
            ProcessImageCommand = new DelegateCommand(ExecuteProcessImage);
        }

        // Method executed when the Button is clicked
        private void ExecuteProcessImage()
        {

            try
            {
                string sampleImagePath = "sample_image.bmp";

                if (!System.IO.File.Exists(sampleImagePath))
                {
                    MessageBox.Show($"Sample image not found: {System.IO.Path.GetFullPath(sampleImagePath)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Load the sample image (ensure proper disposal)
                Bitmap? loadedBitmap = null;
                Bitmap? grayBitmap = null;
                try
                {
                    loadedBitmap = new Bitmap(sampleImagePath);
                    OriginalBitmap?.Dispose(); 
                    OriginalBitmap = (Bitmap)loadedBitmap.Clone(); 

                    grayBitmap = _imageProcessingService.ConvertToGrayscale(loadedBitmap);

                    if (grayBitmap != null)
                    {
                        GrayscaleBitmap?.Dispose(); // Dispose previous bitmap if any
                        GrayscaleBitmap = grayBitmap;
                    }
                    else
                    {
                        MessageBox.Show("Failed to convert image to grayscale.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        GrayscaleBitmap?.Dispose();
                        GrayscaleBitmap = null; // Clear display on failure
                    }

                }
                finally
                {
                    loadedBitmap?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading or processing image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OriginalBitmap?.Dispose(); // Clear display on error
                OriginalBitmap = null;
                GrayscaleBitmap?.Dispose();
                GrayscaleBitmap = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    OriginalBitmap?.Dispose();
                    GrayscaleBitmap?.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}