using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows; 
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Models;

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {

        private readonly IImageProcessingWorkflowService _workflowService;

        public HistogramViewModel ChartViewModel { get; private set; }

        private bool _isBusy = false;
        private bool _isDisposed = false;

        private string _title = "Webcam Image Processor (Refactored)";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private Bitmap? _originalBitmap;
        public Bitmap? OriginalBitmap { get => _originalBitmap; private set => SetProperty(ref _originalBitmap, value); }

        private Bitmap? _grayscaleBitmap;
        public Bitmap? GrayscaleBitmap { get => _grayscaleBitmap; private set => SetProperty(ref _grayscaleBitmap, value); }

        private bool _isHistogramGenerated = false;
        public bool IsHistogramGenerated { get => _isHistogramGenerated; private set => SetProperty(ref _isHistogramGenerated, value); } // Make setter private again

        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        public DelegateCommand ProcessImageCommand { get; private set; }

        public MainWindowViewModel(IImageProcessingWorkflowService workflowService)
        {
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));

            ChartViewModel = new HistogramViewModel(); 

            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
        }

        // --- Command Methods ---
        private bool CanExecuteProcessImage() => !IsBusy;

        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return;

            IsBusy = true;


            OriginalBitmap?.Dispose();
            GrayscaleBitmap?.Dispose();
            OriginalBitmap = null;
            GrayscaleBitmap = null;
            IsHistogramGenerated = false;
            ChartViewModel.ClearHistogram();


            ImageProcessingResult? processingResult = null;
            try
            {
                processingResult = await _workflowService.ExecuteProcessingAsync();

                if (processingResult.Success)
                {
                    OriginalBitmap = processingResult.OriginalBitmap; 
                    GrayscaleBitmap = processingResult.GrayscaleBitmap; 
                    ChartViewModel.UpdateHistogram(processingResult.HistogramData);
                    IsHistogramGenerated = processingResult.HistogramData != null;
                    Console.WriteLine("ViewModel: Processing successful."); 
                }
                else
                {
                    Console.WriteLine($"ViewModel: Processing failed - {processingResult.ErrorMessage}"); 
                    MessageBox.Show(processingResult.ErrorMessage ?? "Image processing failed.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                    OriginalBitmap?.Dispose();
                    GrayscaleBitmap?.Dispose();
                    OriginalBitmap = null;
                    GrayscaleBitmap = null;
                    IsHistogramGenerated = false;
                    ChartViewModel.ClearHistogram();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"ViewModel: An unexpected error occurred calling workflow: {ex.Message}"); 
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                OriginalBitmap?.Dispose();
                GrayscaleBitmap?.Dispose();
                OriginalBitmap = null;
                GrayscaleBitmap = null;
                IsHistogramGenerated = false;
                ChartViewModel.ClearHistogram();
            }
            finally
            {
                IsBusy = false; 

                if (processingResult != null)
                {
                    if (OriginalBitmap != processingResult.OriginalBitmap) processingResult.OriginalBitmap?.Dispose();
                    if (GrayscaleBitmap != processingResult.GrayscaleBitmap) processingResult.GrayscaleBitmap?.Dispose();
                }
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
                    Console.WriteLine("Disposing ViewModel resources...");
                    OriginalBitmap?.Dispose();
                    GrayscaleBitmap?.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}
