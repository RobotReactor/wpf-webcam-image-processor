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
        // --- Services ---
        private readonly IImageProcessingService _imageProcessingService;
        private readonly ICameraService _cameraService;
        private bool _isBusy = false;
        private bool _isDisposed = false;

        // --- Properties ---
        private string _title = "Webcam Image Processor";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private Bitmap? _originalBitmap;
        public Bitmap? OriginalBitmap { get => _originalBitmap; private set => SetProperty(ref _originalBitmap, value); }

        private Bitmap? _grayscaleBitmap;
        public Bitmap? GrayscaleBitmap { get => _grayscaleBitmap; private set => SetProperty(ref _grayscaleBitmap, value); }


        private bool _isHistogramGenerated = false;
        public bool IsHistogramGenerated
        {
            get => _isHistogramGenerated;
            set => SetProperty(ref _isHistogramGenerated, value);
        }

        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        public DelegateCommand ProcessImageCommand { get; private set; }
        public MainWindowViewModel(IImageProcessingService imageProcessingService, ICameraService cameraService)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
        }

        private bool CanExecuteProcessImage() => !IsBusy;

        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return;
            IsBusy = true;
            Bitmap? capturedBitmap = null;
            Bitmap? grayResultBitmap = null;

            OriginalBitmap?.Dispose(); OriginalBitmap = null;
            GrayscaleBitmap?.Dispose(); GrayscaleBitmap = null;
            IsHistogramGenerated = false;

            try
            {
                // Step 1: Capture
                Console.WriteLine("Attempting to capture image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();
                if (capturedBitmap == null)
                {
                    MessageBox.Show("Failed to capture image from camera.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Console.WriteLine("Image captured successfully.");
                OriginalBitmap = (Bitmap)capturedBitmap.Clone(); // Display clone

                // Step 2: Grayscale
                Console.WriteLine("Attempting grayscale conversion...");
                grayResultBitmap = _imageProcessingService.ConvertToGrayscale(capturedBitmap);
                GrayscaleBitmap = grayResultBitmap; // Assign result (takes ownership)
                if (GrayscaleBitmap == null)
                {
                    MessageBox.Show("Failed to convert image to grayscale.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Console.WriteLine("Grayscale conversion successful.");

                // Step 3: Histogram (Calculate but don't update chart series)
                Console.WriteLine("Attempting histogram generation...");
                int[]? histogramData = _imageProcessingService.GenerateHistogram(GrayscaleBitmap);
                if (histogramData != null)
                {
                    Console.WriteLine("Histogram generation successful (display disabled).");
                    IsHistogramGenerated = true; // Set flag
                }
                else
                {
                    MessageBox.Show("Failed to generate histogram.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OriginalBitmap?.Dispose(); OriginalBitmap = null;
                GrayscaleBitmap?.Dispose(); GrayscaleBitmap = null;
                IsHistogramGenerated = false;
            }
            finally
            {
                capturedBitmap?.Dispose();
                IsBusy = false;
            }
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
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