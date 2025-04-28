using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing; 
using System.Threading.Tasks;
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Models;
using WpfWebcamImageProcessor.App.Exceptions;
using Emgu.CV; 

namespace WpfWebcamImageProcessor.App.ViewModels
{
    /// <summary>
    /// Defines the different image processing or display modes available in the UI.
    /// The order here directly corresponds to the integer values of the filter selection slider.
    /// </summary>
    public enum ActiveFilter
    {
        Original,        // Slider Value 0: Show the original captured image
        Grayscale,       // Slider Value 1: Show the grayscale version
        Blur,            // Slider Value 2: Apply Gaussian blur
        Edges,           // Slider Value 3: Show Canny edge detection results
        ErosionDilation, // Slider Value 4: Apply opening (Erode then Dilate)
        Contours         // Slider Value 5: Show detected contours drawn on the original
    }

    /// <summary>
    /// Main ViewModel for the application window (MainWindow.xaml).
    /// Coordinates image capture, processing workflow execution via injected services,
    /// manages UI state (like busy status, selected filter), and exposes data (like the
    /// image to display and histogram model) for binding in the View.
    /// Implements IDisposable to ensure proper cleanup of image resources (Mat objects).
    /// </summary>
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        // Injected Services
        // Service responsible for the overall capture -> grayscale -> histogram workflow.
        private readonly IImageProcessingWorkflowService _workflowService;
        // Service responsible for individual image processing filter operations.
        private readonly IImageProcessingService _imageProcessingService;
        // Service responsible for managing the histogram's PlotModel data and updates.
        private readonly IHistogramService _histogramService;

        /// <summary>
        /// Gets the injected service instance that manages the histogram chart model.
        /// The View's PlotView binds to HistogramService.HistogramPlotModel to display the chart.
        /// </summary>
        public IHistogramService HistogramService => _histogramService;


        // Backing Fields for Properties
        private bool _isBusy = false;
        private bool _isDisposed = false;
        // Store the base images from the last successful workflow run as Mat objects.
        // These are used as input for applying different filters. They must be disposed properly.
        private Mat? _currentOriginalMat = null;
        private Mat? _currentGrayscaleMat = null;
        private string _title = "Webcam Image Processor";
        // Stores the Mat object currently being displayed in the main image area.
        // This changes based on the selected filter. It must be disposed properly.
        private Mat? _displayMat;
        private bool _isHistogramGenerated = false;
        // Tracks the current position (0-5) of the filter selection slider.
        private int _selectedFilterIndex = 0;

        /// <summary>
        /// Gets or sets the title displayed in the application window's title bar.
        /// </summary>
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        /// <summary>
        /// Gets the Mat object representing the image to be displayed (original or filtered).
        /// The View uses a converter (MatToBitmapSourceConverter) to display this Mat.
        /// </summary>
        public Mat? DisplayMat { get => _displayMat; private set => SetProperty(ref _displayMat, value); }

        /// <summary>
        /// Gets a value indicating if a histogram was successfully generated in the last processing run.
        /// </summary>
        public bool IsHistogramGenerated { get => _isHistogramGenerated; private set => SetProperty(ref _isHistogramGenerated, value); }

        /// <summary>
        /// Gets a value indicating if an image processing operation is currently in progress.
        /// Used by the View to disable the capture button via command CanExecute binding.
        /// </summary>
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { ProcessImageCommand.RaiseCanExecuteChanged(); } } }

        /// <summary>
        /// Gets or sets the integer index corresponding to the currently selected filter via the slider.
        /// The setter ensures the value is valid and triggers applying the selected filter.
        /// </summary>
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                // Ensure the incoming value from the slider stays within the bounds of the ActiveFilter enum definition.
                int maxIndex = Enum.GetValues(typeof(ActiveFilter)).Length - 1;
                int clampedValue = Math.Clamp(value, 0, maxIndex);

                // Update the index and apply the filter only if the value actually changed.
                // SetProperty handles the change check and raises PropertyChanged.
                if (SetProperty(ref _selectedFilterIndex, clampedValue))
                {
                    // Check if the necessary base images are loaded before trying to apply a filter.
                    if (CanApplyFilterNow())
                    {
                        ApplySelectedFilter();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently selected filter type (enum value) based on the slider's index.
        /// This provides a more readable way to check the current filter in logic.
        /// </summary>
        public ActiveFilter CurrentFilter => (ActiveFilter)_selectedFilterIndex;

        /// <summary>
        /// Command bound to the "Capture and Process Image" button in the View.
        /// Initiates the image processing workflow when executed.
        /// </summary>
        public DelegateCommand ProcessImageCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// Required services are injected via the constructor by the DI container (Prism/DryIoc).
        /// </summary>
        /// <param name="workflowService">Service for the main processing sequence.</param>
        /// <param name="imageProcessingService">Service for applying individual filters.</param>
        /// <param name="histogramService">Service for managing histogram data.</param>
        /// <exception cref="ArgumentNullException">Thrown if any injected service is null.</exception>
        public MainWindowViewModel(
            IImageProcessingWorkflowService workflowService,
            IImageProcessingService imageProcessingService,
            IHistogramService histogramService)
        {
            // Store references to the injected services. Ensure they are not null.
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _histogramService = histogramService ?? throw new ArgumentNullException(nameof(histogramService));

            // Set up the command, linking it to the execution method (ExecuteProcessImageAsync)
            // and the condition method (CanExecuteProcessImage) that determines if the command is enabled.
            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
        }

        // Command Execution Logic

        /// <summary>
        /// Determines if the ProcessImageCommand can be executed.
        /// Prevents starting a new process while one is already running.
        /// Used by the DelegateCommand to enable/disable the bound UI element.
        /// </summary>
        /// <returns>True if not busy; otherwise, false.</returns>
        private bool CanExecuteProcessImage() => !IsBusy;

        /// <summary>
        /// Asynchronously executes the main image processing workflow when the ProcessImageCommand is invoked.
        /// Handles setting busy state, clearing previous results, calling the workflow service,
        /// updating state based on the result, applying the initial filter view, and error handling.
        /// </summary>
        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return; // Prevent concurrent execution

            IsBusy = true; // Signal that processing has started
            ClearImageData(); // Clean up images from the previous run
            _histogramService.ClearHistogram(); // Reset the histogram display

            ImageProcessingResult? processingResult = null;
            bool success = false; // Track workflow success for final cleanup logic

            try
            {
                // Delegate the core processing steps (capture, grayscale, histogram data) to the workflow service.
                processingResult = await _workflowService.ExecuteProcessingAsync();

                // If the workflow service reported success, update the ViewModel state with the new data.
                if (processingResult.Success)
                {
                    // Dispose previously stored images before assigning the new ones.
                    _currentOriginalMat?.Dispose();
                    _currentGrayscaleMat?.Dispose();

                    // Store the new base images. The ViewModel now owns these Mat objects and is responsible for their disposal.
                    _currentOriginalMat = processingResult.OriginalMat;
                    _currentGrayscaleMat = processingResult.GrayscaleMat;

                    // Update the histogram chart via the histogram service using the new data.
                    _histogramService.UpdateHistogram(processingResult.HistogramData);
                    IsHistogramGenerated = processingResult.HistogramData != null;

                    // Apply the filter indicated by the current slider position to update the main display.
                    // Since the slider defaults to 0, this will initially show the Original image.
                    ApplySelectedFilter();
                    success = true; // Mark the operation as successful for cleanup logic.
                }
                else
                {
                    // If the workflow service reported failure, handle the error (show message, clear state).
                    HandleProcessingError(processingResult.ErrorMessage ?? "Image processing workflow failed.");
                }
            }
            catch (Exception ex) // Catch any unexpected exceptions from the workflow or result handling.
            {
                HandleProcessingError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure the busy indicator is reset regardless of success or failure.
                IsBusy = false;

                // Defensive cleanup: Dispose Mat objects returned by the service if they weren't successfully
                // assigned to the ViewModel's fields. This handles potential errors occurring after the
                // service call returned but before assignment, or if the overall operation failed.
                if (processingResult != null)
                {
                    if (!success || _currentOriginalMat != processingResult.OriginalMat) processingResult.OriginalMat?.Dispose();
                    if (!success || _currentGrayscaleMat != processingResult.GrayscaleMat) processingResult.GrayscaleMat?.Dispose();
                }
            }
        }

        /// <summary>
        /// Checks if the necessary base image(s) are available to apply the currently selected filter.
        /// For example, 'Original' needs the original Mat, most filters need the grayscale Mat,
        /// and 'Contours' needs both.
        /// </summary>
        /// <returns>True if the filter can be applied based on available images; otherwise, false.</returns>
        private bool CanApplyFilterNow()
        {
            ActiveFilter filter = CurrentFilter;
            if (filter == ActiveFilter.Original) return _currentOriginalMat != null;
            if (filter == ActiveFilter.Contours) return _currentOriginalMat != null && _currentGrayscaleMat != null;
            // All other filters currently operate on the grayscale Mat.
            return _currentGrayscaleMat != null;
        }


        /// <summary>
        /// Applies the image processing filter corresponding to the current slider index (<see cref="CurrentFilter"/>).
        /// Updates the <see cref="DisplayMat"/> property, which triggers the UI update via binding and a converter.
        /// Uses fixed default parameters for filters since the slider only selects the filter type.
        /// </summary>
        private void ApplySelectedFilter()
        {
            // Don't proceed if the required base images aren't loaded yet.
            if (!CanApplyFilterNow()) return;

            // Store reference to the currently displayed Mat for later disposal.
            var oldDisplayMat = DisplayMat;
            Mat? newDisplayMat = null; // Will hold the result of the filter operation.

            // Define default parameters used when filter selection is done via slider index.
            const int defaultBlurKernelSize = 5;    // Must be positive and odd
            const int defaultMorphIterations = 1;   // Must be positive
            const double defaultCannyThreshold1 = 50;
            const double defaultCannyThreshold2 = 150;

            // Intermediate Mats used in multi-step filters like Opening.
            Mat? tempMat1 = null;
            ContourResult? contours = null; // Holds contour data

            try
            {
                ActiveFilter filterToApply = CurrentFilter;
                Console.WriteLine($"Applying filter: {filterToApply}"); // Informative console output

                // Select and apply the appropriate filter based on the slider's current value.
                switch (filterToApply)
                {
                    case ActiveFilter.Original:
                        // Clone the stored original Mat to display it without modifying the stored version.
                        newDisplayMat = _currentOriginalMat?.Clone();
                        break;

                    case ActiveFilter.Grayscale:
                        // Clone the stored grayscale Mat.
                        newDisplayMat = _currentGrayscaleMat?.Clone();
                        break;

                    case ActiveFilter.Blur:
                        // Apply Gaussian blur to the grayscale Mat.
                        if (_currentGrayscaleMat != null)
                            newDisplayMat = _imageProcessingService.ApplyGaussianBlur(_currentGrayscaleMat, defaultBlurKernelSize);
                        break;

                    case ActiveFilter.ErosionDilation:
                        // Apply "Opening" (Erode then Dilate) to the grayscale Mat.
                        if (_currentGrayscaleMat != null)
                        {
                            // Perform erosion first. The service returns a new Mat.
                            tempMat1 = _imageProcessingService.ApplyErosion(_currentGrayscaleMat, defaultMorphIterations);
                            if (tempMat1 != null) // Proceed only if erosion succeeded
                            {
                                // Perform dilation on the eroded result. The service returns another new Mat.
                                newDisplayMat = _imageProcessingService.ApplyDilation(tempMat1, defaultMorphIterations);
                            }
                        }
                        break;

                    case ActiveFilter.Edges:
                        // Apply Canny edge detection to the grayscale Mat.
                        if (_currentGrayscaleMat != null)
                            newDisplayMat = _imageProcessingService.DetectEdgesCanny(_currentGrayscaleMat, defaultCannyThreshold1, defaultCannyThreshold2);
                        break;

                    case ActiveFilter.Contours:
                        // Detect contours on the grayscale image and draw them on the original color image.
                        if (_currentGrayscaleMat != null && _currentOriginalMat != null)
                        {
                            // Find the contours first.
                            contours = _imageProcessingService.DetectContours(_currentGrayscaleMat);
                            // Draw the found contours onto a *clone* of the original color image.
                            newDisplayMat = _imageProcessingService.DrawContours(_currentOriginalMat, contours);
                        }
                        break;

                    default:
                        // Fallback: Should not be reached if slider clamping works correctly. Display original.
                        newDisplayMat = _currentOriginalMat?.Clone();
                        break;
                }
            }
            catch (Exception ex) // Catch any exceptions during filter application.
            {
                // Report the error and attempt to revert to a stable display state (original image).
                HandleProcessingError($"Error applying filter '{CurrentFilter}': {ex.Message}");
                newDisplayMat?.Dispose(); // Dispose partially created Mat if error occurred mid-filter
                newDisplayMat = _currentOriginalMat?.Clone();
            }
            finally
            {
                // Ensure any intermediate Mat objects created within the try block are disposed.
                tempMat1?.Dispose();
                // Dispose contour data structure if necessary (VectorOfVectorOfPoint is IDisposable).
                contours?.Contours?.Dispose();
            }

            // Update the DisplayMat property. The ViewModel now owns the newDisplayMat.
            // The PropertyChanged event will notify the View to update the Image control (via the converter).
            DisplayMat = newDisplayMat;

            // Dispose the Mat that was previously displayed *after* assigning the new one
            // to prevent the UI briefly showing nothing.
            oldDisplayMat?.Dispose();
        }

        /// <summary>
        /// Centralized method for handling errors during processing or filter application.
        /// Displays an error message and resets the application's image state.
        /// Currently uses MessageBox; should be replaced with a dialog service for better testability.
        /// </summary>
        /// <param name="message">The error message to display to the user.</param>
        private void HandleProcessingError(string message)
        {
            // Display error message to the user via MessageBox.
            // Consider replacing with an injected IDialogService for better MVVM adherence and testability.
            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            // Reset the application state after an error.
            ClearImageData();
            _histogramService.ClearHistogram();
            IsHistogramGenerated = false;
        }

        /// <summary>
        /// Clears and disposes all stored and displayed image Mat objects
        /// to release native resources and reset the UI.
        /// </summary>
        private void ClearImageData()
        {
            // Use null-conditional dispose (?.) for safety, in case fields are already null.
            _currentOriginalMat?.Dispose();
            _currentGrayscaleMat?.Dispose();
            _currentOriginalMat = null;
            _currentGrayscaleMat = null;

            DisplayMat?.Dispose();
            // Setting DisplayMat to null triggers PropertyChanged via SetProperty in its setter,
            // which updates the UI (clears the image display).
            DisplayMat = null;
        }


        // IDisposable Implementation

        /// <summary>
        /// Public method to trigger resource cleanup, following the IDisposable pattern.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Request that the system suppress finalization for this object,
            // as cleanup is handled by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected virtual method implementing the actual resource cleanup logic.
        /// Ensures that resources (specifically, managed IDisposable objects like Mat)
        /// are released correctly. Can be overridden by derived classes if needed.
        /// </summary>
        /// <param name="disposing">True if called explicitly via the Dispose() method;
        /// false if called by the garbage collector's finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Prevent multiple disposal calls.
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Only dispose managed resources if called explicitly (disposing=true).
                    // Do not touch managed resources if called from the finalizer,
                    // as their state might be unpredictable.
                    ClearImageData(); // Centralized cleanup logic for Mat objects.
                }

                // Cleanup for unmanaged resources directly held by this class would go here (if any existed).

                _isDisposed = true; // Mark as disposed to prevent redundant cleanup.
            }
        }
    }
}
