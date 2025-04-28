using Prism.Commands;
using Prism.Mvvm;
using System;
// using System.Diagnostics; // Can remove Debug
using System.Drawing;
using System.Threading.Tasks;
// using System.Windows; // Avoid direct reference for MessageBox
using WpfWebcamImageProcessor.App.Services; // Service Interfaces
using WpfWebcamImageProcessor.App.Models; // ImageProcessingResult
using WpfWebcamImageProcessor.App.Exceptions; // ImageProcessingException

namespace WpfWebcamImageProcessor.App.ViewModels
{
    /// <summary>
    /// Represents the available image processing filters or display modes
    /// controlled by the UI slider. The order corresponds to the slider's integer values.
    /// </summary>
    public enum ActiveFilter
    {
        Original,       // Slider Value 0
        Grayscale,      // Slider Value 1
        Blur,           // Slider Value 2
        ErosionDilation, // Slider Value 3
        Edges,          // Slider Value 4
        Contours        // Slider Value 5
    }

    /// <summary>
    /// The main ViewModel for the application window. It orchestrates the image capture
    /// and processing workflow, manages the display state, and exposes commands and data
    /// for the View (MainWindow.xaml).
    /// </summary>
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        // --- Injected Services ---
        private readonly IImageProcessingWorkflowService _workflowService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IHistogramService _histogramService; // *** ADDED: Field for histogram service ***

        // --- Child ViewModel Removed ---

        // --- Public Property for Histogram Service (for Binding) ---
        /// <summary>
        /// Gets the service responsible for managing the histogram chart model.
        /// The View binds to HistogramService.HistogramPlotModel.
        /// </summary>
        public IHistogramService HistogramService => _histogramService; // *** ADDED: Expose service for binding ***


        // --- Backing Fields ---
        private bool _isBusy = false;
        private bool _isDisposed = false;
        private Bitmap? _currentOriginalBitmap = null;
        private Bitmap? _currentGrayscaleBitmap = null;
        private string _title = "Webcam Image Processor";
        private Bitmap? _displayBitmap;
        private bool _isHistogramGenerated = false;
        private int _selectedFilterIndex = 0;

        // --- Public Properties (for View Binding) ---
        /// <summary>
        /// Gets or sets the title displayed in the application window.
        /// </summary>
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        /// <summary>
        /// Gets the Bitmap currently intended for display in the main image control.
        /// This will be the original image or the result of the selected filter.
        /// </summary>
        public Bitmap? DisplayBitmap { get => _displayBitmap; private set => SetProperty(ref _displayBitmap, value); }

        /// <summary>
        /// Gets a value indicating whether histogram data has been successfully generated
        /// during the last processing cycle.
        /// </summary>
        public bool IsHistogramGenerated { get => _isHistogramGenerated; private set => SetProperty(ref _isHistogramGenerated, value); }

        /// <summary>
        /// Gets a value indicating if a background operation (like image processing) is currently running.
        /// Used to disable UI elements during processing.
        /// </summary>
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        /// <summary>
        /// Gets or sets the current integer index selected by the filter slider.
        /// Setting this property automatically triggers the application of the corresponding filter.
        /// </summary>
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                // Ensure the incoming value is within the valid range of the ActiveFilter enum.
                int maxIndex = Enum.GetValues(typeof(ActiveFilter)).Length - 1;
                int clampedValue = Math.Clamp(value, 0, maxIndex);

                // Only proceed if the value actually changed.
                if (SetProperty(ref _selectedFilterIndex, clampedValue))
                {
                    // Apply the filter if the necessary base images are available.
                    if (CanApplyFilterNow())
                    {
                        ApplySelectedFilter();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently selected filter based on the <see cref="SelectedFilterIndex"/>.
        /// </summary>
        public ActiveFilter CurrentFilter => (ActiveFilter)_selectedFilterIndex;

        /// <summary>
        /// Command triggered by the UI to start the image capture and processing workflow.
        /// </summary>
        public DelegateCommand ProcessImageCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="workflowService">The service that orchestrates the image capture/processing steps.</param>
        /// <param name="imageProcessingService">The service that performs individual image processing operations.</param>
        /// <param name="histogramService">The service that manages the histogram plot model.</param> // *** ADDED PARAM ***
        /// <exception cref="ArgumentNullException">Thrown if required services are null.</exception>
        public MainWindowViewModel(
            IImageProcessingWorkflowService workflowService,
            IImageProcessingService imageProcessingService,
            IHistogramService histogramService) // *** ADDED: Inject IHistogramService ***
        {
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _histogramService = histogramService ?? throw new ArgumentNullException(nameof(histogramService)); // *** ADDED: Assign injected service ***

            // Initialize the command that starts the main process.
            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
        }

        /// <summary>
        /// Determines if the main processing command can execute (typically, only if not already busy).
        /// </summary>
        /// <returns>True if the command can execute, false otherwise.</returns>
        private bool CanExecuteProcessImage() => !IsBusy;

        /// <summary>
        /// Asynchronously executes the main workflow:
        /// 1. Sets the busy indicator.
        /// 2. Clears previous results.
        /// 3. Calls the workflow service to capture, grayscale, and generate histogram data.
        /// 4. Updates internal state (_currentOriginalBitmap, _currentGrayscaleBitmap) and histogram chart.
        /// 5. Applies the currently selected filter for initial display.
        /// 6. Handles errors.
        /// 7. Clears the busy indicator.
        /// </summary>
        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return;

            IsBusy = true;
            ClearImageData(); // Dispose previous bitmaps and clear properties
            _histogramService.ClearHistogram(); // *** CORRECTED: Use injected service ***
            ImageProcessingResult? processingResult = null;
            bool success = false;

            try
            {
                // Delegate the core processing steps to the workflow service.
                processingResult = await _workflowService.ExecuteProcessingAsync();

                // Process the results if the workflow succeeded.
                if (processingResult.Success)
                {
                    // Dispose any previously stored bitmaps before assigning new ones.
                    _currentOriginalBitmap?.Dispose();
                    _currentGrayscaleBitmap?.Dispose();

                    // Store the new base images internally. ViewModel now owns these bitmaps.
                    _currentOriginalBitmap = processingResult.OriginalBitmap;
                    _currentGrayscaleBitmap = processingResult.GrayscaleBitmap;

                    // Update the histogram chart via its dedicated service.
                    _histogramService.UpdateHistogram(processingResult.HistogramData); // *** CORRECTED: Use injected service ***
                    IsHistogramGenerated = processingResult.HistogramData != null;

                    // Apply the filter indicated by the current slider position to update the main display.
                    ApplySelectedFilter();
                    success = true; // Mark as successful
                }
                else
                {
                    // Handle failure reported by the workflow service.
                    HandleProcessingError(processingResult.ErrorMessage ?? "Image processing workflow failed.");
                }
            }
            catch (Exception ex) // Catch unexpected errors during the workflow call or result handling.
            {
                HandleProcessingError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure the busy indicator is always turned off.
                IsBusy = false;

                // Clean up bitmaps from the result if they weren't successfully assigned
                // to the ViewModel (e.g., if an error occurred after the service returned
                // but before assignment, or if the overall operation failed).
                if (processingResult != null)
                {
                    if (!success || _currentOriginalBitmap != processingResult.OriginalBitmap) processingResult.OriginalBitmap?.Dispose();
                    if (!success || _currentGrayscaleBitmap != processingResult.GrayscaleBitmap) processingResult.GrayscaleBitmap?.Dispose();
                }
            }
        }

        /// <summary>
        /// Checks if the necessary base image(s) required for the currently selected filter are available.
        /// </summary>
        /// <returns>True if the filter can be applied, false otherwise.</returns>
        private bool CanApplyFilterNow()
        {
            ActiveFilter filter = CurrentFilter;
            if (filter == ActiveFilter.Original) return _currentOriginalBitmap != null;
            // Contours drawing needs the original color image and the grayscale/binary for detection.
            if (filter == ActiveFilter.Contours) return _currentOriginalBitmap != null && _currentGrayscaleBitmap != null;
            // All other currently implemented filters operate on the grayscale image.
            return _currentGrayscaleBitmap != null;
        }


        /// <summary>
        /// Applies the image processing filter corresponding to the current slider index (<see cref="CurrentFilter"/>).
        /// Updates the <see cref="DisplayBitmap"/> property which is bound to the main image control in the UI.
        /// Uses default parameters for filters controlled by the slider index.
        /// </summary>
        private void ApplySelectedFilter()
        {
            // Exit if prerequisites are not met.
            if (!CanApplyFilterNow()) return;

            // Keep track of the old bitmap to dispose it after the UI updates.
            var oldDisplayBitmap = DisplayBitmap;
            Bitmap? newDisplayBitmap = null;

            // Define default parameters used when filter selection is done via slider index.
            const int defaultBlurKernelSize = 5;
            const int defaultMorphIterations = 1;
            const double defaultCannyThreshold1 = 50;
            const double defaultCannyThreshold2 = 150;

            try
            {
                ActiveFilter filterToApply = CurrentFilter;

                // Execute the appropriate image processing operation based on the selected filter.
                switch (filterToApply)
                {
                    case ActiveFilter.Original:
                        newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null;
                        break;

                    case ActiveFilter.Grayscale:
                        newDisplayBitmap = _currentGrayscaleBitmap != null ? (Bitmap)_currentGrayscaleBitmap.Clone() : null;
                        break;

                    case ActiveFilter.Blur:
                        if (_currentGrayscaleBitmap != null)
                            newDisplayBitmap = _imageProcessingService.ApplyGaussianBlur(_currentGrayscaleBitmap, defaultBlurKernelSize);
                        break;

                    case ActiveFilter.ErosionDilation:
                        if (_currentGrayscaleBitmap != null)
                        {
                            using Bitmap? eroded = _imageProcessingService.ApplyErosion(_currentGrayscaleBitmap, defaultMorphIterations);
                            if (eroded != null)
                                newDisplayBitmap = _imageProcessingService.ApplyDilation(eroded, defaultMorphIterations);
                        }
                        break;

                    case ActiveFilter.Edges:
                        if (_currentGrayscaleBitmap != null)
                            newDisplayBitmap = _imageProcessingService.DetectEdgesCanny(_currentGrayscaleBitmap, defaultCannyThreshold1, defaultCannyThreshold2);
                        break;

                    case ActiveFilter.Contours:
                        if (_currentGrayscaleBitmap != null && _currentOriginalBitmap != null)
                        {
                            var contours = _imageProcessingService.DetectContours(_currentGrayscaleBitmap);
                            newDisplayBitmap = _imageProcessingService.DrawContours(_currentOriginalBitmap, contours);
                        }
                        break;

                    default:
                        // Fallback case (shouldn't be reached with clamping) - display original.
                        newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null;
                        break;
                }
            }
            catch (Exception ex) // Catch errors during the filter application itself.
            {
                // Log the error and attempt to revert to a safe display state.
                HandleProcessingError($"Error applying filter '{CurrentFilter}': {ex.Message}");
                newDisplayBitmap = _currentOriginalBitmap != null ? (Bitmap)_currentOriginalBitmap.Clone() : null;
            }

            // Update the property bound to the UI's Image control.
            DisplayBitmap = newDisplayBitmap;

            // Dispose the previous bitmap *after* assigning the new one to avoid UI flicker.
            oldDisplayBitmap?.Dispose();
        }

        /// <summary>
        /// Handles displaying errors to the user (currently via MessageBox) and resetting the application state.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void HandleProcessingError(string message)
        {
            // TODO: Replace MessageBox.Show with a call to an injected IDialogService
            //       or update a status message property bound to the UI.
            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            // Reset image data and UI state on error.
            ClearImageData();
            _histogramService.ClearHistogram(); // *** CORRECTED: Use injected service ***
            IsHistogramGenerated = false;
        }

        /// <summary>
        /// Clears all internally stored image data (_currentOriginalBitmap, _currentGrayscaleBitmap)
        /// and the displayed image (_displayBitmap), ensuring associated Bitmaps are disposed.
        /// </summary>
        private void ClearImageData()
        {
            // Dispose stored base bitmaps first.
            _currentOriginalBitmap?.Dispose();
            _currentGrayscaleBitmap?.Dispose();
            _currentOriginalBitmap = null;
            _currentGrayscaleBitmap = null;

            // Dispose and clear the bitmap currently bound to the UI.
            DisplayBitmap?.Dispose();
            DisplayBitmap = null; // This will trigger PropertyChanged via SetProperty in the setter.
        }


        /// <summary>
        /// Releases resources used by the ViewModel, particularly undisposed Bitmap objects.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Prevent the finalizer from running unnecessarily.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs the actual resource cleanup.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    ClearImageData(); // Centralized cleanup
                }
                // No unmanaged resources to free directly in this class.
                _isDisposed = true;
            }
        }
    }
}
