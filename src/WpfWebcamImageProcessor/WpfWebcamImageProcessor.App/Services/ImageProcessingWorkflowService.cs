using System;
using System.Drawing;
using System.Threading.Tasks;
using WpfWebcamImageProcessor.App.Models;

namespace WpfWebcamImageProcessor.App.Services
{
    // Implements the workflow for capturing and processing an image
    public class ImageProcessingWorkflowService : IImageProcessingWorkflowService
    {
        private readonly ICameraService _cameraService;
        private readonly IImageProcessingService _imageProcessingService;

        // Inject required services
        public ImageProcessingWorkflowService(ICameraService cameraService, IImageProcessingService imageProcessingService)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        }

        public async Task<ImageProcessingResult> ExecuteProcessingAsync()
        {
            var result = new ImageProcessingResult(); 
            Bitmap? capturedBitmap = null;
            Bitmap? grayscaleBitmap = null; 

            try
            {
                // --- Step 1: Capture Image ---
                Console.WriteLine("WorkflowService: Attempting to capture image...");
                capturedBitmap = await _cameraService.CaptureImageAsync();

                if (capturedBitmap == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to capture image from camera.";
                    return result; 
                }
                Console.WriteLine("WorkflowService: Image captured successfully.");
                result.OriginalBitmap = (Bitmap)capturedBitmap.Clone();


                // --- Step 2: Convert to Grayscale ---
                Console.WriteLine("WorkflowService: Attempting grayscale conversion...");
                grayscaleBitmap = _imageProcessingService.ConvertToGrayscale(capturedBitmap);

                if (grayscaleBitmap == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to convert image to grayscale.";
                }
                else
                {
                    Console.WriteLine("WorkflowService: Grayscale conversion successful.");
                    result.GrayscaleBitmap = grayscaleBitmap; 
                }

                // --- Step 3: Generate Histogram ---
                if (result.GrayscaleBitmap != null)
                {
                    Console.WriteLine("WorkflowService: Attempting histogram generation...");
                    result.HistogramData = _imageProcessingService.GenerateHistogram(result.GrayscaleBitmap);

                    if (result.HistogramData == null)
                    {
                        result.Success = false; 
                        result.ErrorMessage = (result.ErrorMessage ?? "") + " Failed to generate histogram.";
                    }
                    else
                    {
                        Console.WriteLine("WorkflowService: Histogram generation successful.");

                        if (string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            result.Success = true;
                        }
                    }
                }
                else
                {
                    result.Success = false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowService: An unexpected error occurred: {ex.Message}"); 
                result.Success = false;
                result.ErrorMessage = $"An unexpected error occurred during processing: {ex.Message}";

                result.OriginalBitmap?.Dispose();
                result.OriginalBitmap = null;
                result.GrayscaleBitmap?.Dispose();
                result.GrayscaleBitmap = null;
                result.HistogramData = null;
            }
            finally
            {
                capturedBitmap?.Dispose();
            }

            return result;
        }
    }
}
