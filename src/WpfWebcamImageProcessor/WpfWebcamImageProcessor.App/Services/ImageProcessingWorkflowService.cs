using System;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV;
using WpfWebcamImageProcessor.App.Models; 
using WpfWebcamImageProcessor.App.Exceptions;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Orchestrates the multi-step process of capturing an image from the camera,
    /// converting it to a grayscale Mat, and generating its histogram data using Mat objects internally.
    /// This service encapsulates the sequence of operations, making the calling code (like a ViewModel) simpler.
    /// </summary>
    public class ImageProcessingWorkflowService : IImageProcessingWorkflowService
    {
        private readonly ICameraService _cameraService;
        private readonly IImageProcessingService _imageProcessingService;

        /// <summary>
        /// Initializes a new instance of the ImageProcessingWorkflowService class.
        /// It requires instances of the camera and image processing services to function.
        /// </summary>
        /// <param name="cameraService">The service responsible for capturing images from the camera.</param>
        /// <param name="imageProcessingService">The service responsible for performing image manipulations.</param>
        /// <exception cref="ArgumentNullException">Thrown if either service dependency is null.</exception>
        public ImageProcessingWorkflowService(ICameraService cameraService, IImageProcessingService imageProcessingService)
        {
            // Store injected dependencies, ensuring they are provided.
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        }

        /// <summary>
        /// Asynchronously executes the full image capture and basic processing workflow using Mat objects.
        /// This involves capturing, converting to grayscale, and generating a histogram.
        /// It also handles potential errors during these steps.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation. The task result is an
        /// ImageProcessingResult object containing the outcome (success/failure),
        /// processed images (original Mat, grayscale Mat), histogram data,
        /// and any error messages if failures occurred.
        /// </returns>
        public async Task<ImageProcessingResult> ExecuteProcessingAsync()
        {
            // Initialize the object that will hold the results of this workflow.
            var result = new ImageProcessingResult();
            Bitmap? capturedBitmap = null; // Holds the initial capture from the camera service
            Mat? originalMat = null; // Mat version of the original captured image
            Mat? grayscaleMat = null; // Mat version of the grayscale image, used for histogram

            try
            {
                // First, attempt to capture an image from the camera service.
                Console.WriteLine("Workflow: Capturing image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();

                // If camera capture fails (returns null), processing cannot proceed.
                if (capturedBitmap == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to capture image from camera.";
                    Console.WriteLine("Workflow: Capture failed.");
                    return result; // Exit the workflow early.
                }
                Console.WriteLine("Workflow: Capture successful.");

                // Convert the captured Bitmap to an EmguCV Mat object for internal processing.
                // This service takes ownership of this initial Mat object.
                originalMat = Emgu.CV.BitmapExtension.ToMat(capturedBitmap);
                if (originalMat.IsEmpty)
                {
                    // If the conversion results in an empty Mat, something is wrong.
                    throw new ImageProcessingException("Captured bitmap resulted in an empty Mat object.");
                }
                // Assign the Mat to the result object. Ownership transfers to the caller upon successful completion.
                result.OriginalMat = originalMat;

                // Next, convert the original captured Bitmap (not the Mat) to a grayscale Mat.
                // The image processing service creates and returns a new Mat object.
                Console.WriteLine("Workflow: Converting to grayscale Mat...");
                grayscaleMat = _imageProcessingService.ConvertToGrayscaleMat(capturedBitmap);
                // Assign the grayscale Mat to the result. Ownership transfers to the caller upon successful completion.
                result.GrayscaleMat = grayscaleMat;
                Console.WriteLine("Workflow: Grayscale conversion successful.");

                // Finally, generate the histogram directly from the grayscale Mat object.
                // This might throw an exception if the grayscale Mat is invalid or processing fails.
                Console.WriteLine("Workflow: Generating histogram from grayscale Mat...");
                result.HistogramData = _imageProcessingService.GenerateHistogram(grayscaleMat);
                Console.WriteLine("Workflow: Histogram generation successful.");

                // If execution reaches this point without any exceptions being thrown,
                // consider the overall workflow successful.
                result.Success = true;
            }
            // Catch specific exceptions that might be thrown by the underlying services.
            catch (ArgumentNullException argEx)
            {
                Console.WriteLine($"Workflow Error: Null argument - {argEx.ParamName}: {argEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed due to invalid input: {argEx.Message}";
            }
            catch (ArgumentOutOfRangeException argRangeEx)
            {
                Console.WriteLine($"Workflow Error: Invalid argument value - {argRangeEx.ParamName}: {argRangeEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed due to invalid parameter: {argRangeEx.Message}";
            }
            catch (ImageProcessingException procEx) // Catch custom exceptions from the processing service
            {
                Console.WriteLine($"Workflow Error: Image Processing Exception - {procEx.Message}");
                result.Success = false;
                result.ErrorMessage = $"Processing failed: {procEx.Message}";
                // Optionally include details from the original exception that caused this one.
                if (procEx.InnerException != null) { result.ErrorMessage += $" (Details: {procEx.InnerException.Message})"; }
            }
            catch (Exception ex) // Catch any other unexpected exceptions.
            {
                Console.WriteLine($"Workflow Error: Unexpected error - {ex.Message}");
                result.Success = false;
                result.ErrorMessage = $"An unexpected error occurred during processing: {ex.Message}";
            }
            finally
            {
                // Ensure resources are released properly in this cleanup section.

                // Always dispose the initial captured Bitmap, as it's no longer needed directly.
                // The result object holds the Mat version or it failed.
                capturedBitmap?.Dispose();

                // If the overall workflow failed, ensure any Mat objects created within this scope
                // OR assigned to the result object are properly disposed to prevent memory leaks.
                if (!result.Success)
                {
                    // Dispose the Mat objects held by the result object, as they are invalid or incomplete.
                    result.OriginalMat?.Dispose(); result.OriginalMat = null;
                    result.GrayscaleMat?.Dispose(); result.GrayscaleMat = null;
                    result.HistogramData = null; // Clear histogram data as well.

                    // Also ensure the local Mat variables are disposed if they hold references
                    // that weren't successfully transferred to the result object (important if errors occurred mid-process).
                    if (originalMat != null && result.OriginalMat != originalMat) originalMat.Dispose();
                    if (grayscaleMat != null && result.GrayscaleMat != grayscaleMat) grayscaleMat.Dispose();
                }
                else
                {
                    // grayscaleMat is assigned directly to result.GrayscaleMat, so no extra check needed here on success.
                    if (originalMat != null && result.OriginalMat != originalMat) originalMat.Dispose();
                }
            }

            // Return the result object containing the outcome, data, and any error message.
            return result;
        }
    }
}
