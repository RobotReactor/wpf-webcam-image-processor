using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using WpfWebcamImageProcessor.App.Services;

using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

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
        private string _title = "Webcam Image Processor (OxyPlot)"; // Updated title slightly
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private Bitmap? _originalBitmap;
        public Bitmap? OriginalBitmap { get => _originalBitmap; private set => SetProperty(ref _originalBitmap, value); }

        private Bitmap? _grayscaleBitmap;
        public Bitmap? GrayscaleBitmap { get => _grayscaleBitmap; private set => SetProperty(ref _grayscaleBitmap, value); }

        // Flag to indicate if histogram data was successfully generated
        private bool _isHistogramGenerated = false;
        public bool IsHistogramGenerated { get => _isHistogramGenerated; set => SetProperty(ref _isHistogramGenerated, value); } // Public setter

        // OxyPlot Model Property
        private PlotModel _histogramPlotModel;
        public PlotModel HistogramPlotModel
        {
            get => _histogramPlotModel;
            private set => SetProperty(ref _histogramPlotModel, value); // Use SetProperty for notifications
        }

        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        // --- Commands ---
        public DelegateCommand ProcessImageCommand { get; private set; }

        // Constructor
        public MainWindowViewModel(IImageProcessingService imageProcessingService, ICameraService cameraService)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

            var tempPlotModel = new PlotModel { Title = "Grayscale Histogram" };

            tempPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Intensity",
                Minimum = -0.5,
                Maximum = 255.5,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None
            });
            tempPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Count",
                Minimum = 0,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0.05 

            });
            HistogramPlotModel = tempPlotModel;

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
            HistogramPlotModel.Series.Clear(); 
            HistogramPlotModel.InvalidatePlot(true);

            try
            {
                // Step 1: Capture Image
                Console.WriteLine("Attempting to capture image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();
                if (capturedBitmap == null)
                {
                    MessageBox.Show("Failed to capture image from camera.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Console.WriteLine("Image captured successfully.");
                    OriginalBitmap = (Bitmap)capturedBitmap.Clone();
                }

                // Step 2: Convert to Grayscale
                if (capturedBitmap != null)
                {
                    Console.WriteLine("Attempting grayscale conversion...");
                    grayResultBitmap = _imageProcessingService.ConvertToGrayscale(capturedBitmap);
                    if (grayResultBitmap != null)
                    {
                        Console.WriteLine("Grayscale conversion successful.");
                        GrayscaleBitmap = grayResultBitmap; 
                    }
                    else
                    {
                        MessageBox.Show("Failed to convert image to grayscale.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Step 3: Generate and Display Histogram 
                if (GrayscaleBitmap != null)
                {
                    Console.WriteLine("Attempting histogram generation...");
                    int[]? histogramData = _imageProcessingService.GenerateHistogram(GrayscaleBitmap);
                    if (histogramData != null)
                    {
                        Console.WriteLine("Histogram generation successful.");
                        IsHistogramGenerated = true;

                        var rectBarSeries = new RectangleBarSeries
                        {
                            Title = "Count", 
                            StrokeThickness = 1, 
                            FillColor = OxyColors.SteelBlue 
                        };

                        for (int i = 0; i < histogramData.Length; i++)
                        {
                            double x0 = i - 0.5; 
                            double x1 = i + 0.5; 
                            double y0 = 0;       
                            double y1 = histogramData[i]; 

                            rectBarSeries.Items.Add(new RectangleBarItem(x0, y0, x1, y1));
                        }

                        HistogramPlotModel.Series.Clear(); 
                        HistogramPlotModel.Series.Add(rectBarSeries); 
                        HistogramPlotModel.InvalidatePlot(true); 
                    }
                    else
                    {
                        MessageBox.Show("Failed to generate histogram.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsHistogramGenerated = false;
                        HistogramPlotModel.Series.Clear();
                        HistogramPlotModel.InvalidatePlot(true);
                    }
                }
                else
                {
                    IsHistogramGenerated = false;
                    HistogramPlotModel.Series.Clear();
                    HistogramPlotModel.InvalidatePlot(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OriginalBitmap?.Dispose(); OriginalBitmap = null;
                GrayscaleBitmap?.Dispose(); GrayscaleBitmap = null;
                IsHistogramGenerated = false;
                HistogramPlotModel.Series.Clear();
                HistogramPlotModel.InvalidatePlot(true);
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