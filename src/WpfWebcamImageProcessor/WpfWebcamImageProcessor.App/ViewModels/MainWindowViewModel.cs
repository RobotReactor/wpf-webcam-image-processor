using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows; 
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Models;
using WpfWebcamImageProcessor.App.Exceptions;
using Emgu.CV;
using Emgu.CV.CvEnum; 

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
    /// Coordinates image capture (single or live stream) and processing workflow execution via injected services,
    /// manages UI state (like busy status, selected filter, live view status), and exposes data (like the
    /// image to display and histogram model) for binding in the View.
    /// Implements IDisposable to ensure proper cleanup of image resources (Mat objects) and camera stream.
    /// </summary>
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        // Injected Services
        private readonly IImageProcessingWorkflowService _workflowService; // Handles the single capture workflow
        private readonly IImageProcessingService _imageProcessingService; // Handles individual filter application
        private readonly IHistogramService _histogramService; // Manages histogram data and PlotModel
        private readonly ICameraService _cameraService; // Handles camera interaction (single capture and streaming)

        /// <summary>
        /// Gets the injected service instance that manages the histogram chart model.
        /// The View binds its PlotView control to HistogramService.HistogramPlotModel.
        /// </summary>
        public IHistogramService HistogramService => _histogramService;


        // Backing Fields for Properties
        private bool _isBusy = false; // Tracks if a single capture/process operation is running
        private bool _isLiveViewActive = false; // Tracks if the live camera stream is active
        private bool _isDisposed = false;
        // Store the base images from the last successful workflow run as Mat objects.
        // These are held internally and used as input when applying different filters via the slider.
        private Mat? _currentOriginalMat = null;
        private Mat? _currentGrayscaleMat = null;
        private string _title = "Webcam Image Processor";
        // Stores the Mat object currently being displayed in the main image area of the UI.
        // This changes based on the selected filter. It needs to be disposed when replaced or when the VM is disposed.
        private Mat? _displayMat;
        private bool _isHistogramGenerated = false;
        // Tracks the current position (integer value 0-5) of the filter selection slider.
        private int _selectedFilterIndex = 0;

        /// <summary>
        /// Gets or sets the title displayed in the application window's title bar.
        /// </summary>
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        /// <summary>
        /// Gets the Mat object representing the image to be displayed (original or filtered).
        /// The View uses a value converter (MatToBitmapSourceConverter) to display this Mat object
        /// in an Image control.
        /// </summary>
        public Mat? DisplayMat { get => _displayMat; private set => SetProperty(ref _displayMat, value); }

        /// <summary>
        /// Gets a value indicating if a histogram was successfully generated in the last processing run.
        /// </summary>
        public bool IsHistogramGenerated { get => _isHistogramGenerated; private set => SetProperty(ref _isHistogramGenerated, value); }

        /// <summary>
        /// Gets a value indicating if an image processing operation is currently in progress.
        /// Used by the View to disable related UI elements via command CanExecute binding.
        /// </summary>
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { RaiseCanExecuteChanged(); } } }

        /// <summary>
        /// Gets a value indicating if the live camera stream and processing loop is currently active.
        /// Used by the View to manage the state of the Start/Stop Live View buttons.
        /// </summary>
        public bool IsLiveViewActive { get => _isLiveViewActive; private set { if (SetProperty(ref _isLiveViewActive, value)) { RaiseCanExecuteChanged(); } } }


        /// <summary>
        /// Gets or sets the integer index corresponding to the currently selected filter via the slider.
        /// The setter validates the index, updates the internal state, and triggers the application
        /// of the corresponding filter to the displayed image if appropriate.
        /// </summary>
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                // Ensure the incoming value from the slider stays within the bounds defined by the ActiveFilter enum.
                int maxIndex = Enum.GetValues(typeof(ActiveFilter)).Length - 1;
                int clampedValue = Math.Clamp(value, 0, maxIndex);

                // Update the index and apply the filter only if the value actually changed.
                // SetProperty handles the change check and raises the PropertyChanged event.
                if (SetProperty(ref _selectedFilterIndex, clampedValue))
                {
                    // Check if the necessary base images are loaded before trying to apply a filter.
                    // This prevents errors if the slider is moved before the first capture.
                    if (!IsBusy && CanApplyFilterNow())
                    {
                        ApplySelectedFilter();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently selected filter type (the ActiveFilter enum value)
        /// based on the current integer index from the slider.
        /// </summary>
        public ActiveFilter CurrentFilter => (ActiveFilter)_selectedFilterIndex;

        /// <summary>
        /// Command bound to the "Capture and Process Image" button in the View.
        /// Initiates the single-frame capture and processing workflow.
        /// </summary>
        public DelegateCommand ProcessImageCommand { get; private set; }

        /// <summary>
        /// Command bound to the "Start Live View" button in the View.
        /// Starts the continuous camera feed and real-time processing.
        /// </summary>
        public DelegateCommand StartLiveViewCommand { get; private set; }

        /// <summary>
        /// Command bound to the "Stop Live View" button in the View.
        /// Stops the continuous camera feed.
        /// </summary>
        public DelegateCommand StopLiveViewCommand { get; private set; }


        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// Sets up dependencies and commands.
        /// </summary>
        /// <param name="workflowService">Service for the single-frame processing sequence.</param>
        /// <param name="imageProcessingService">Service for applying individual filters.</param>
        /// <param name="histogramService">Service for managing histogram data.</param>
        /// <param name="cameraService">Service for camera interaction (capture and streaming).</param>
        /// <exception cref="ArgumentNullException">Thrown if any injected service is null.</exception>
        public MainWindowViewModel(
            IImageProcessingWorkflowService workflowService,
            IImageProcessingService imageProcessingService,
            IHistogramService histogramService,
            ICameraService cameraService)
        {
            // Store references to the injected services, ensuring they are not null.
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _histogramService = histogramService ?? throw new ArgumentNullException(nameof(histogramService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

            // Set up the commands, linking them to their execution logic and CanExecute conditions.
            ProcessImageCommand = new DelegateCommand(async () => await ExecuteProcessImageAsync(), CanExecuteProcessImage);
            StartLiveViewCommand = new DelegateCommand(ExecuteStartLiveView, CanStartLiveView);
            StopLiveViewCommand = new DelegateCommand(ExecuteStopLiveView, CanStopLiveView);
        }

        /// <summary>
        /// Determines if the single capture/process command can execute.
        /// It should only be enabled when not busy and live view is not active.
        /// </summary>
        private bool CanExecuteProcessImage() => !IsBusy && !IsLiveViewActive;

        /// <summary>
        /// Determines if the live view start command can execute.
        /// It should only be enabled when not busy and live view is not already active.
        /// </summary>
        private bool CanStartLiveView() => !IsBusy && !IsLiveViewActive;

        /// <summary>
        /// Determines if the live view stop command can execute.
        /// It should only be enabled when live view is currently active.
        /// </summary>
        private bool CanStopLiveView() => IsLiveViewActive;

        /// <summary>
        /// Notifies the commands that their execution status might have changed.
        /// This should be called whenever IsBusy or IsLiveViewActive changes.
        /// </summary>
        private void RaiseCanExecuteChanged()
        {
            // Use Prism's mechanism to re-evaluate the CanExecute methods for each command.
            ProcessImageCommand.RaiseCanExecuteChanged();
            StartLiveViewCommand.RaiseCanExecuteChanged();
            StopLiveViewCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Asynchronously executes the single-frame capture and processing workflow
        /// when the ProcessImageCommand is invoked.
        /// </summary>
        private async Task ExecuteProcessImageAsync()
        {
            if (!CanExecuteProcessImage()) return; // Prevent concurrent execution

            // Ensure live view is stopped before starting a single capture.
            if (IsLiveViewActive) ExecuteStopLiveView();

            IsBusy = true; // Indicate start of single capture operation
            ClearImageData(); // Clean up images from the previous run
            _histogramService.ClearHistogram(); // Reset the histogram display
            ImageProcessingResult? processingResult = null;
            bool success = false; // Track workflow success for final cleanup logic

            try
            {
                // Delegate the core processing steps to the workflow service.
                processingResult = await _workflowService.ExecuteProcessingAsync();

                // If the workflow service reported success, update the ViewModel state.
                if (processingResult.Success)
                {
                    // Dispose previously stored images before assigning the new ones.
                    _currentOriginalMat?.Dispose();
                    _currentGrayscaleMat?.Dispose();

                    // Store the new base images. The ViewModel now owns these Mat objects.
                    _currentOriginalMat = processingResult.OriginalMat;
                    _currentGrayscaleMat = processingResult.GrayscaleMat;

                    // Update the histogram chart via the histogram service.
                    _histogramService.UpdateHistogram(processingResult.HistogramData);
                    IsHistogramGenerated = processingResult.HistogramData != null;

                    // Apply the filter indicated by the current slider position for initial display.
                    ApplySelectedFilter();
                    success = true; // Mark the operation as successful for cleanup logic.
                }
                else
                {
                    // If the workflow service reported failure, handle the error.
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
                // transferred to the ViewModel's fields.
                if (processingResult != null)
                {
                    if (!success || _currentOriginalMat != processingResult.OriginalMat) processingResult.OriginalMat?.Dispose();
                    if (!success || _currentGrayscaleMat != processingResult.GrayscaleMat) processingResult.GrayscaleMat?.Dispose();
                }
            }
        }

        /// <summary>
        /// Initiates the continuous camera capture stream when the StartLiveViewCommand is invoked.
        /// </summary>
        private void ExecuteStartLiveView()
        {
            if (!CanStartLiveView()) return;

            try
            {
                Console.WriteLine("ViewModel: Starting Live View...");
                // Clear any results from a previous single capture first.
                ClearImageData();
                _histogramService.ClearHistogram();
                IsHistogramGenerated = false;

                // Tell the camera service to start streaming, providing the method
                // to call back for each received frame.
                _cameraService.StartCaptureStream(ProcessLiveFrame);
                IsLiveViewActive = true; // Update the state flag.
                Console.WriteLine("ViewModel: Live View Started.");
            }
            catch (Exception ex)
            {
                // Handle errors during stream startup, such as camera access issues.
                HandleProcessingError($"Failed to start live view: {ex.Message}");
                IsLiveViewActive = false; // Ensure state is reset on failure.
            }
        }

        /// <summary>
        /// Stops the continuous camera capture stream when the StopLiveViewCommand is invoked.
        /// </summary>
        private void ExecuteStopLiveView()
        {
            if (!CanStopLiveView()) return;

            try
            {
                Console.WriteLine("ViewModel: Stopping Live View...");
                // Tell the camera service to stop streaming and release resources.
                _cameraService.StopCaptureStream();
                IsLiveViewActive = false; // Update the state flag.
                Console.WriteLine("ViewModel: Live View Stopped.");
            }
            catch (Exception ex)
            {
                // Log errors during stopping, although the stream might already be considered stopped.
                Console.WriteLine($"Error stopping live view: {ex.Message}");
                IsLiveViewActive = false; // Ensure state is correct.
            }
        }

        /// <summary>
        /// Callback method executed by the CameraService for each frame captured during live streaming.
        /// This method runs on a background thread provided by the CameraService.
        /// It processes the frame (grayscale, histogram, selected filter) and updates the UI
        /// properties via the Dispatcher.
        /// </summary>
        /// <param name="frameMat">The captured frame provided by the CameraService.</param>
        private void ProcessLiveFrame(Mat frameMat)
        {
            // Basic validation of the received frame.
            if (frameMat == null || frameMat.IsEmpty || frameMat.Ptr == IntPtr.Zero) return;

            // Since the frameMat provided by the service might be reused,
            // immediately clone it to ensure exclusive access for processing.
            Mat? clonedFrame = null;
            Mat? grayscaleMat = null;
            Mat? processedMat = null; // Holds the final image to display after filtering
            int[]? histogramData = null;
            bool frameProcessedSuccessfully = false;

            try
            {
                clonedFrame = frameMat.Clone(); // Work with a stable copy

                // Perform base processing: convert to grayscale and generate histogram.
                grayscaleMat = new Mat(); // Initialize the destination Mat
                if (clonedFrame.NumberOfChannels == 1) { clonedFrame.CopyTo(grayscaleMat); } // Already grayscale
                else if (clonedFrame.NumberOfChannels == 3 || clonedFrame.NumberOfChannels == 4)
                {
                    ColorConversion code = clonedFrame.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(clonedFrame, grayscaleMat, code);
                }
                else { throw new ImageProcessingException("Unsupported channel count for grayscale conversion in live frame."); }

                histogramData = _imageProcessingService.GenerateHistogram(grayscaleMat);

                // Apply the filter currently selected via the UI slider.
                ActiveFilter filter = CurrentFilter;
                // Define fixed parameters for filters (could be properties later).
                const int defaultBlurKernelSize = 5;
                const int defaultMorphIterations = 1;
                const double defaultCannyThreshold1 = 50;
                const double defaultCannyThreshold2 = 150;
                // Intermediate Mats for multi-step operations like Opening.
                Mat? tempMat1 = null;
                ContourResult? contours = null;

                try // Inner try specifically for the filter application step.
                {
                    switch (filter)
                    {
                        case ActiveFilter.Original:
                            processedMat = clonedFrame.Clone();
                            break;
                        case ActiveFilter.Grayscale:
                            processedMat = grayscaleMat.Clone();
                            break;
                        case ActiveFilter.Blur:
                            processedMat = _imageProcessingService.ApplyGaussianBlur(grayscaleMat, defaultBlurKernelSize);
                            break;
                        case ActiveFilter.Edges:
                            processedMat = _imageProcessingService.DetectEdgesCanny(grayscaleMat, defaultCannyThreshold1, defaultCannyThreshold2);
                            break;
                        case ActiveFilter.ErosionDilation:
                            tempMat1 = _imageProcessingService.ApplyErosion(grayscaleMat, defaultMorphIterations);
                            if (tempMat1 != null)
                                processedMat = _imageProcessingService.ApplyDilation(tempMat1, defaultMorphIterations);
                            break;
                        case ActiveFilter.Contours:
                            if (clonedFrame != null)
                            { // Need original frame to draw on
                                contours = _imageProcessingService.DetectContours(grayscaleMat);
                                processedMat = _imageProcessingService.DrawContours(clonedFrame, contours);
                            }
                            else
                            {
                                processedMat = grayscaleMat.Clone(); // Fallback if original clone isn't available
                            }
                            break;
                        default:
                            processedMat = grayscaleMat.Clone(); // Default to showing grayscale
                            break;
                    }
                    // Consider processing successful if it resulted in a non-null Mat.
                    frameProcessedSuccessfully = processedMat != null;
                }
                catch (Exception filterEx)
                {
                    // Log errors during filter application but try to continue.
                    Console.WriteLine($"Error applying filter {filter} in live view: {filterEx.Message}");
                    // Fallback: attempt to display grayscale if the filter failed.
                    processedMat?.Dispose(); // Dispose partially created mat from failed filter.
                    processedMat = grayscaleMat?.Clone(); // Clone grayscale as fallback.
                    frameProcessedSuccessfully = processedMat != null;
                }
                finally
                {
                    // Dispose intermediate Mats created during filtering.
                    tempMat1?.Dispose();
                    contours?.Contours?.Dispose(); // VectorOfVectorOfPoint is IDisposable
                }

                // If processing (including fallback) yielded a valid image, update the UI.
                if (frameProcessedSuccessfully && processedMat != null)
                {
                    // Updates to UI-bound properties must happen on the UI thread.
                    // Use the application's dispatcher for this.
                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        // Double-check if live view is still active before updating the UI.
                        // This prevents updates if the user stopped the stream while this frame was processing.
                        if (!IsLiveViewActive)
                        {
                            processedMat.Dispose(); // Don't update UI, just dispose the processed frame.
                            return;
                        }

                        // Get a reference to the currently displayed Mat.
                        var oldDisplay = DisplayMat;
                        // Assign the newly processed Mat to the property bound to the UI.
                        // The ViewModel now owns processedMat.
                        DisplayMat = processedMat;
                        // Dispose the previously displayed Mat *after* assigning the new one.
                        oldDisplay?.Dispose();

                        // Update the histogram chart.
                        if (histogramData != null)
                        {
                            _histogramService.UpdateHistogram(histogramData);
                            IsHistogramGenerated = true; // Update status flag.
                        }
                        else
                        {
                            _histogramService.ClearHistogram();
                            IsHistogramGenerated = false;
                        }
                    });
                    // Note: Ownership of processedMat is transferred to the DisplayMat property
                    // via the dispatcher action. It should not be disposed here in the background thread.
                }
                else
                {
                    // If processing failed to produce a valid Mat for display, dispose it if it exists.
                    processedMat?.Dispose();
                }

            }
            catch (Exception ex)
            {
                // Log errors during the overall frame processing (like cloning or base processing).
                Console.WriteLine($"Error processing live frame: {ex.Message}");
                // Ensure any created Mats are disposed on error.
                processedMat?.Dispose();
            }
            finally
            {
                // Dispose the main Mats created at the start of this method's scope.
                // grayscaleMat is created here. clonedFrame is created here.
                // processedMat's ownership is transferred or it's disposed above.
                clonedFrame?.Dispose();
                grayscaleMat?.Dispose();
            }
        }


        // ApplySelectedFilter is primarily used for single capture results or if slider changes when NOT live
        private void ApplySelectedFilter()
        {
            if (!CanApplyFilterNow()) return;

            var oldDisplayMat = DisplayMat;
            Mat? newDisplayMat = null;
            const int defaultBlurKernelSize = 5;
            const int defaultMorphIterations = 1;
            const double defaultCannyThreshold1 = 50;
            const double defaultCannyThreshold2 = 150;
            Mat? tempMat1 = null;
            ContourResult? contours = null;

            try
            {
                ActiveFilter filterToApply = CurrentFilter;
                Console.WriteLine($"Applying filter: {filterToApply}");

                switch (filterToApply)
                {
                    case ActiveFilter.Original:
                        newDisplayMat = _currentOriginalMat?.Clone();
                        break;
                    case ActiveFilter.Grayscale:
                        newDisplayMat = _currentGrayscaleMat?.Clone();
                        break;
                    case ActiveFilter.Blur:
                        if (_currentGrayscaleMat != null)
                            newDisplayMat = _imageProcessingService.ApplyGaussianBlur(_currentGrayscaleMat, defaultBlurKernelSize);
                        break;
                    case ActiveFilter.Edges:
                        if (_currentGrayscaleMat != null)
                            newDisplayMat = _imageProcessingService.DetectEdgesCanny(_currentGrayscaleMat, defaultCannyThreshold1, defaultCannyThreshold2);
                        break;
                    case ActiveFilter.ErosionDilation:
                        if (_currentGrayscaleMat != null)
                        {
                            tempMat1 = _imageProcessingService.ApplyErosion(_currentGrayscaleMat, defaultMorphIterations);
                            if (tempMat1 != null)
                                newDisplayMat = _imageProcessingService.ApplyDilation(tempMat1, defaultMorphIterations);
                        }
                        break;
                    case ActiveFilter.Contours:
                        if (_currentGrayscaleMat != null && _currentOriginalMat != null)
                        {
                            contours = _imageProcessingService.DetectContours(_currentGrayscaleMat);
                            newDisplayMat = _imageProcessingService.DrawContours(_currentOriginalMat, contours);
                        }
                        break;
                    default:
                        newDisplayMat = _currentOriginalMat?.Clone();
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleProcessingError($"Error applying filter '{CurrentFilter}': {ex.Message}");
                newDisplayMat?.Dispose();
                newDisplayMat = _currentOriginalMat?.Clone();
            }
            finally
            {
                tempMat1?.Dispose();
                contours?.Contours?.Dispose();
            }

            DisplayMat = newDisplayMat;
            oldDisplayMat?.Dispose();
        }

        // CanApplyFilterNow checks if base images are available for filter application
        private bool CanApplyFilterNow()
        {
            ActiveFilter filter = CurrentFilter;
            if (filter == ActiveFilter.Original) return _currentOriginalMat != null;
            if (filter == ActiveFilter.Contours) return _currentOriginalMat != null && _currentGrayscaleMat != null;
            return _currentGrayscaleMat != null;
        }

        // HandleProcessingError displays errors and resets state
        private void HandleProcessingError(string message)
        {
            // Consider replacing MessageBox with a dialog service.
            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            ClearImageData();
            _histogramService.ClearHistogram();
            IsHistogramGenerated = false;
        }

        // ClearImageData disposes and nulls out image Mats
        private void ClearImageData()
        {
            _currentOriginalMat?.Dispose();
            _currentGrayscaleMat?.Dispose();
            _currentOriginalMat = null;
            _currentGrayscaleMat = null;
            DisplayMat?.Dispose();
            DisplayMat = null;
        }


        // IDisposable Implementation
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Stop the camera stream if running when the ViewModel is disposed
                    if (IsLiveViewActive)
                    {
                        ExecuteStopLiveView();
                    }
                    ClearImageData(); // Dispose Mats
                }
                _isDisposed = true;
            }
        }
    }
}
