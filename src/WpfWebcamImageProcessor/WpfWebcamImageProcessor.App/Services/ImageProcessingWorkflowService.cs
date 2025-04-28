using System;
using System.Drawing;
using System.Threading.Tasks;
using WpfWebcamImageProcessor.App.Models; 
using WpfWebcamImageProcessor.App.Exceptions; 

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Orchestrates the multi-step process of capturing an image from the camera,
    /// converting it to grayscale, and generating its histogram data.
    /// This service encapsulates the sequence of operations, making the calling code (ViewModel) simpler.
    /// </summary>
    public class ImageProcessingWorkflowService : IImageProcessingWorkflowService
    {
        private readonly ICameraService _cameraService;
        private readonly IImageProcessingService _imageProcessingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcessingWorkflowService"/> class.
        /// </summary>
        /// <param name="cameraService">The service responsible for capturing images from the camera.</param>
        /// <param name="imageProcessingService">The service responsible for performing image manipulations.</param>
        /// <exception cref="ArgumentNullException">Thrown if either service dependency is null.</exception>
        public ImageProcessingWorkflowService(ICameraService cameraService, IImageProcessingService imageProcessingService)
        {
            // Store injected dependencies, ensuring they are not null.
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        }

        /// <summary>
        /// Asynchronously executes the full image capture and basic processing workflow.
        /// Handles potential errors during capture or processing steps.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation. The task result is an
        /// <see cref="ImageProcessingResult"/> object containing the outcome (success/failure),
        /// processed images (original, grayscale), histogram data, and any error messages.
        /// </returns>
        public async Task<ImageProcessingResult> ExecuteProcessingAsync()
        {
            // Initialize the object to hold the results of this workflow.
            var result = new ImageProcessingResult();
            Bitmap? capturedBitmap = null;
            Bitmap? grayscaleBitmap = null; // Keep local reference for histogram step and disposal management

            try
            {
                Console.WriteLine("Workflow: Capturing image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();

                // If camera capture fails, we cannot proceed with processing.
                if (capturedBitmap == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to capture image from camera.";
                    Console.WriteLine("Workflow: Capture failed.");
                    return result; // Exit the workflow early.
                }
                Console.WriteLine("Workflow: Capture successful.");
                // Store a clone of the captured image in the result. The original 'capturedBitmap'
                // will be disposed in the finally block. Cloning ensures the result holds a valid object.
                result.OriginalBitmap = (Bitmap)capturedBitmap.Clone();

                Console.WriteLine("Workflow: Converting to grayscale...");
                // Attempt grayscale conversion. This call might throw exceptions.
                grayscaleBitmap = _imageProcessingService.ConvertToGrayscale(capturedBitmap);
                result.GrayscaleBitmap = grayscaleBitmap; // Assign to result (result now owns this bitmap if successful)
                Console.WriteLine("Workflow: Grayscale conversion successful.");

                // Proceed only if grayscale conversion was successful.
                Console.WriteLine("Workflow: Generating histogram...");
                // Attempt histogram generation. This call might throw exceptions.
                // GenerateHistogram should now throw on failure rather than returning null for processing errors.
                result.HistogramData = _imageProcessingService.GenerateHistogram(grayscaleBitmap);
                Console.WriteLine("Workflow: Histogram generation successful.");

                // If all steps completed without throwing exceptions, mark as successful.
                result.Success = true;

            }
            // Catch specific exceptions originating from the image processing service.
            catch (ArgumentNullException argEx)
            {
                Console.WriteLine($"Workflow Error: Null argument provided to processing service - {argEx.ParamName}: {argEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed due to invalid input: {argEx.Message}";
            }
            catch (ArgumentOutOfRangeException argRangeEx)
            {
                Console.WriteLine($"Workflow Error: Invalid argument value provided to processing service - {argRangeEx.ParamName}: {argRangeEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed due to invalid parameter: {argRangeEx.Message}";
            }
            catch (ImageProcessingException procEx)
            {
                // Catch custom exceptions indicating a failure within an image processing step.
                Console.WriteLine($"Workflow Error: Image Processing Exception - {procEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed: {procEx.Message}";
                // Include inner exception details if helpful for debugging.
                if (procEx.InnerException != null)
                {
                    result.ErrorMessage += $" (Details: {procEx.InnerException.Message})";
                }
            }
            // Catch any other unexpected exceptions during the workflow.
            catch (Exception ex)
            {
                Console.WriteLine($"Workflow Error: Unexpected error - {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"An unexpected error occurred during processing: {ex.Message}";
            }
            finally
            {
                // Always dispose the initially captured bitmap, as the result holds a clone.
                capturedBitmap?.Dispose();

                // If the overall workflow failed, ensure any bitmaps potentially assigned
                // to the result object earlier in the try block are disposed to prevent leaks.
                // The caller (ViewModel) should not receive bitmap data if Success is false.
                if (!result.Success)
                {
                    result.OriginalBitmap?.Dispose(); result.OriginalBitmap = null;
                    result.GrayscaleBitmap?.Dispose(); result.GrayscaleBitmap = null;
                    result.HistogramData = null; // Clear histogram data on failure too

                    // Also dispose the local grayscale variable if it exists and wasn't the one
                    // assigned to the (now nulled) result.GrayscaleBitmap. This handles cases
                    // where grayscale succeeded but histogram failed.
                    if (grayscaleBitmap != null && result.GrayscaleBitmap != grayscaleBitmap)
                    {
                        grayscaleBitmap.Dispose();
                    }
                }
                // If successful, the ownership of OriginalBitmap (clone) and GrayscaleBitmap
                // is effectively transferred to the caller via the result object.
            }

            // Return the result object containing status, data, and any error message.
            return result;
        }
    }
}
