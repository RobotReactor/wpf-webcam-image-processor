using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows; 
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Models; 

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public enum ActiveFilter
    {
        Original,      
        Grayscale,     
        Blur,           
        ErosionDilation,
        Edges,          
        Contours        
    }

    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly IImageProcessingWorkflowService _workflowService;
        private readonly IImageProcessingService _imageProcessingService;

        public HistogramViewModel ChartViewModel { get; private set; }

        private bool _isBusy = false;
        private bool _isDisposed = false;
        private Bitmap? _currentOriginalBitmap = null;
        private Bitmap? _currentGrayscaleBitmap = null; 

        private string _title = "Webcam Image Processor";
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        private Bitmap? _displayBitmap;
        public Bitmap? DisplayBitmap { get => _displayBitmap; private set => SetProperty(ref _displayBitmap, value); }

        private bool _isHistogramGenerated = false;
        public bool IsHistogramGenerated { get => _isHistogramGenerated; private set => SetProperty(ref _isHistogramGenerated, value); }

        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        private int _selectedFilterIndex = 0; 
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                int maxIndex = Enum.GetValues(typeof(ActiveFilter)).Length - 1;
                int clampedValue = Math.Clamp(value, 0, maxIndex);

                if (SetProperty(ref _selectedFilterIndex, clampedValue))
                {
                    if (CanApplyFilterNow())
                    {
                        ApplySelectedFilter();
                    }
                }
            }
        }

        public ActiveFilter CurrentFilter => (ActiveFilter)_selectedFilterIndex;

        public DelegateCommand ProcessImageCommand { get; private set; }

        public MainWindowViewModel(
            IImageProcessingWorkflowService workflowService,
            IImageProcessingService imageProcessingService)
        {
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));

            ChartViewModel = new HistogramViewModel(); 

            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
            Debug.WriteLine("ViewModel Initialized. Default Filter Index: " + _selectedFilterIndex); 
        }

        /// <summary>
        /// Determines if the main processing command can execute.
        /// </summary>
        private bool CanExecuteProcessImage() => !IsBusy;

        /// <summary>
        /// Executes the main workflow: captures image, processes it (grayscale, histogram),
        /// stores base images, and applies the currently selected filter for display.
        /// </summary>
        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return;
            Debug.WriteLine("ExecuteProcessImageAsync: Starting...");
            IsBusy = true;
            ClearImageData();
            ChartViewModel.ClearHistogram();
            ImageProcessingResult? processingResult = null;
            bool success = false; 

            try
            {
                processingResult = await _workflowService.ExecuteProcessingAsync();

                if (processingResult.Success)
                {
                    _currentOriginalBitmap?.Dispose();
                    _currentGrayscaleBitmap?.Dispose();

                    _currentOriginalBitmap = processingResult.OriginalBitmap;
                    _currentGrayscaleBitmap = processingResult.GrayscaleBitmap;

                    ChartViewModel.UpdateHistogram(processingResult.HistogramData);
                    IsHistogramGenerated = processingResult.HistogramData != null;

                    ApplySelectedFilter(); 
                    success = true;
                }
                else
                {
                    HandleProcessingError(processingResult.ErrorMessage ?? "Image processing failed.");
                }
            }
            catch (Exception ex)
            {
                HandleProcessingError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                IsBusy = false;

                if (processingResult != null)
                {
                    if (!success || _currentOriginalBitmap != processingResult.OriginalBitmap) processingResult.OriginalBitmap?.Dispose();
                    if (!success || _currentGrayscaleBitmap != processingResult.GrayscaleBitmap) processingResult.GrayscaleBitmap?.Dispose();
                }
                Debug.WriteLine("ExecuteProcessImageAsync: Finished.");
            }
        }

        /// <summary>
        /// Checks if the prerequisites (base images) for applying the currently selected filter are met.
        /// </summary>
        private bool CanApplyFilterNow()
        {
            ActiveFilter filter = CurrentFilter;
            bool canApply = false;
            if (filter == ActiveFilter.Original) canApply = _currentOriginalBitmap != null;
            else if (filter == ActiveFilter.Contours) canApply = _currentOriginalBitmap != null && _currentGrayscaleBitmap != null;
            else canApply = _currentGrayscaleBitmap != null; 

            Debug.WriteLine($"CanApplyFilterNow for {filter}: Result={canApply} (_currentOriginalBitmap null? {(_currentOriginalBitmap == null)}, _currentGrayscaleBitmap null? {(_currentGrayscaleBitmap == null)})"); // DEBUG
            return canApply;
        }


        /// <summary>
        /// Applies the image processing filter corresponding to the current slider index.
        /// Updates the DisplayBitmap property for the UI.
        /// </summary>
        private void ApplySelectedFilter()
        {
            if (!CanApplyFilterNow()) { Debug.WriteLine("ApplySelectedFilter: Exiting because CanApplyFilterNow=false"); return; }

            var oldDisplayBitmap = DisplayBitmap; 
            Bitmap? newDisplayBitmap = null;

            const int defaultBlurKernelSize = 5; 
            const int defaultMorphIterations = 1; 
            const double defaultCannyThreshold1 = 50; 
            const double defaultCannyThreshold2 = 150; 

            try
            {
                ActiveFilter filterToApply = CurrentFilter; 
                Debug.WriteLine($"ApplySelectedFilter: Applying filter: {filterToApply}"); 

                switch (filterToApply)
                {
                    case ActiveFilter.Original:
                        Debug.WriteLine("ApplySelectedFilter: Case Original."); 
                        newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null;
                        Debug.WriteLine($"ApplySelectedFilter: Case Original - newDisplayBitmap is null? {(newDisplayBitmap == null)}"); 
                        break;

                    case ActiveFilter.Grayscale:
                        Debug.WriteLine("ApplySelectedFilter: Case Grayscale.");
                        newDisplayBitmap = _currentGrayscaleBitmap != null ? (Bitmap)_currentGrayscaleBitmap.Clone() : null;
                        Debug.WriteLine($"ApplySelectedFilter: Case Grayscale - newDisplayBitmap is null? {(newDisplayBitmap == null)}"); 
                        break;

                    case ActiveFilter.Blur:
                        Debug.WriteLine("ApplySelectedFilter: Case Blur.");
                        if (_currentGrayscaleBitmap != null)
                            newDisplayBitmap = _imageProcessingService.ApplyGaussianBlur(_currentGrayscaleBitmap, defaultBlurKernelSize);
                        break;

                    case ActiveFilter.ErosionDilation: 
                        Debug.WriteLine("ApplySelectedFilter: Case ErosionDilation.");
                        if (_currentGrayscaleBitmap != null)
                        {
                            using Bitmap? eroded = _imageProcessingService.ApplyErosion(_currentGrayscaleBitmap, defaultMorphIterations);
                            if (eroded != null)
                            {
                                newDisplayBitmap = _imageProcessingService.ApplyDilation(eroded, defaultMorphIterations);
                            }
                        }
                        break;

                    case ActiveFilter.Edges:
                        Debug.WriteLine("ApplySelectedFilter: Case Edges."); 
                        if (_currentGrayscaleBitmap != null)
                            newDisplayBitmap = _imageProcessingService.DetectEdgesCanny(_currentGrayscaleBitmap, defaultCannyThreshold1, defaultCannyThreshold2);
                        break;

                    case ActiveFilter.Contours:
                        Debug.WriteLine("ApplySelectedFilter: Case Contours."); 
                        // Make sure both base images are available
                        if (_currentGrayscaleBitmap != null && _currentOriginalBitmap != null)
                        {
                            var contours = _imageProcessingService.DetectContours(_currentGrayscaleBitmap);
                            newDisplayBitmap = _imageProcessingService.DrawContours(_currentOriginalBitmap, contours);
                        }
                        break;


                    default: // Should not happen if slider range is correct
                        Debug.WriteLine("ApplySelectedFilter: Case Default."); 
                        newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null; 
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplySelectedFilter: Exception during filter application: {ex.Message}"); 
                HandleProcessingError($"Error applying filter '{CurrentFilter}': {ex.Message}");
                newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null;
            }

            Debug.WriteLine($"ApplySelectedFilter: Setting DisplayBitmap. newDisplayBitmap is null? {(newDisplayBitmap == null)}");
            DisplayBitmap = newDisplayBitmap;

            oldDisplayBitmap?.Dispose();
            Debug.WriteLine("ApplySelectedFilter: Finished."); // DEBUG
        }

        /// <summary>
        /// Handles displaying errors to the user and resetting state.
        /// TODO: Replace MessageBox with a proper dialog service or status message.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void HandleProcessingError(string message)
        {
            Debug.WriteLine($"HandleProcessingError: {message}"); // DEBUG
            Console.WriteLine($"ViewModel Error: {message}"); // TODO: Replace with Logger
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Replace MessageBox
            ClearImageData(); 
            ChartViewModel.ClearHistogram();
            IsHistogramGenerated = false;
        }

        /// <summary>
        /// Clears all internally stored and displayed image data, disposing bitmaps.
        /// </summary>
        private void ClearImageData()
        {
            Debug.WriteLine("ClearImageData called."); 
            _currentOriginalBitmap?.Dispose();
            _currentGrayscaleBitmap?.Dispose();
            _currentOriginalBitmap = null;
            _currentGrayscaleBitmap = null;

            DisplayBitmap?.Dispose();
            DisplayBitmap = null;
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
                    Debug.WriteLine("ViewModel Dispose called."); 
                    Console.WriteLine("Disposing ViewModel resources...");
                    ClearImageData(); 
                }
                _isDisposed = true;
            }
        }
    }
}
