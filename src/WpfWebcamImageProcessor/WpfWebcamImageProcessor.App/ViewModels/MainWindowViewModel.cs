using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using WpfWebcamImageProcessor.App.Services;
using System.Linq;

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly ICameraService _cameraService;
        private bool _isBusy = false; 
        private bool _isDisposed = false;

        private string _title = "Webcam Image Processor";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private Bitmap? _originalBitmap;
        public Bitmap? OriginalBitmap
        {
            get { return _originalBitmap; }
            private set { SetProperty(ref _originalBitmap, value); }
        }

        private Bitmap? _grayscaleBitmap;
        public Bitmap? GrayscaleBitmap
        {
            get { return _grayscaleBitmap; }
            private set { SetProperty(ref _grayscaleBitmap, value); }
        }

        private int[]? _histogramData;
        public int[]? HistogramData
        {
            get { return _histogramData; }
            private set { SetProperty(ref _histogramData, value); }
        }

        public DelegateCommand ProcessImageCommand { get; private set; }

        public MainWindowViewModel(IImageProcessingService imageProcessingService, ICameraService cameraService)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService)); 

            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage)
                                    .ObservesProperty(() => IsBusy); 
        }

        private bool CanExecuteProcessImage()
        {
            return !_isBusy; 
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    ProcessImageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Renamed to indicate async, changed return type to Task
        private async Task ExecuteProcessImageAsync()
        {
            if (IsBusy) return; 

            IsBusy = true;
            Bitmap? capturedBitmap = null;
            Bitmap? grayBitmap = null;

            try
            {
                Console.WriteLine("Attempting to capture image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();

                if (capturedBitmap == null)
                {
                    MessageBox.Show("Failed to capture image from camera.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return; 
                }
                Console.WriteLine("Image captured successfully.");

                OriginalBitmap?.Dispose();

                OriginalBitmap = (Bitmap)capturedBitmap.Clone();

                Console.WriteLine("Attempting grayscale conversion...");
                grayBitmap = _imageProcessingService.ConvertToGrayscale(capturedBitmap);

                GrayscaleBitmap?.Dispose();
                GrayscaleBitmap = grayBitmap; 

                if (GrayscaleBitmap == null) 
                {
                    MessageBox.Show("Failed to convert image to grayscale.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; 
                }
                Console.WriteLine("Grayscale conversion successful.");

                Console.WriteLine("Attempting histogram generation...");
                int[]? histogram = _imageProcessingService.GenerateHistogram(GrayscaleBitmap);
                HistogramData = histogram;

                if (HistogramData == null)
                {
                    MessageBox.Show("Failed to generate histogram.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Console.WriteLine("Histogram generation successful.");

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                OriginalBitmap?.Dispose(); OriginalBitmap = null;
                GrayscaleBitmap?.Dispose(); GrayscaleBitmap = null;

                HistogramData = null;
            }
            finally
            {
                capturedBitmap?.Dispose();

                IsBusy = false; 
            }
        }

        // --- Cleanup ---
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
                    Console.WriteLine("Disposing ViewModel resources...");
                    OriginalBitmap?.Dispose();
                    GrayscaleBitmap?.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}