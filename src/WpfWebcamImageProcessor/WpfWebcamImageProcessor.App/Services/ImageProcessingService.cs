using System;
using System.Drawing;
using System.Runtime.Versioning;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using WpfWebcamImageProcessor.App.Exceptions;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Handles various image processing tasks using the EmguCV library.
    /// This service works primarily with EmguCV's Mat objects internally for better performance
    /// and compatibility with the library's functions.
    /// </summary>
    [SupportedOSPlatform("windows")] // Indicates potential Windows-specific dependencies from EmguCV's native libraries.
    public class ImageProcessingService : IImageProcessingService
    {
        /// <summary>
        /// Converts a standard System.Drawing.Bitmap (often from UI or camera)
        /// into an EmguCV Mat object suitable for grayscale processing.
        /// </summary>
        /// <param name="sourceBitmap">The original Bitmap image.</param>
        /// <returns>A new single-channel (grayscale) Mat object. The caller is responsible for disposing this Mat.</returns>
        /// <exception cref="ArgumentNullException">Thrown if sourceBitmap is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if the conversion process fails.</exception>
        public Mat ConvertToGrayscaleMat(Bitmap sourceBitmap)
        {
            if (sourceBitmap == null)
                throw new ArgumentNullException(nameof(sourceBitmap), "Input source bitmap cannot be null.");

            Mat? grayMat = null; // Result Mat, declared here to manage disposal in catch block
            try
            {
                // Convert Bitmap to Mat using the EmguCV extension method.
                using (Mat inputMat = Emgu.CV.BitmapExtension.ToMat(sourceBitmap))
                {
                    if (inputMat.IsEmpty)
                        throw new ImageProcessingException("Input bitmap resulted in an empty Mat object.");

                    grayMat = new Mat(); // Create the Mat to store the grayscale result.

                    // Handle conversion based on number of channels.
                    if (inputMat.NumberOfChannels == 1)
                    {
                        // If it's already single-channel, assume it's grayscale and just copy.
                        inputMat.CopyTo(grayMat);
                    }
                    else if (inputMat.NumberOfChannels == 3)
                    {
                        // Convert 3-channel (BGR) to grayscale.
                        CvInvoke.CvtColor(inputMat, grayMat, ColorConversion.Bgr2Gray);
                    }
                    else if (inputMat.NumberOfChannels == 4)
                    {
                        // Convert 4-channel (BGRA) to grayscale.
                        CvInvoke.CvtColor(inputMat, grayMat, ColorConversion.Bgra2Gray);
                    }
                    else
                    {
                        // Throw an error for unsupported channel counts.
                        throw new ImageProcessingException($"Unsupported number of channels ({inputMat.NumberOfChannels}) for grayscale conversion.");
                    }

                    // Return the newly created grayscale Mat.
                    return grayMat;
                }
            }
            catch (Emgu.CV.Util.CvException cvEx) // Handle specific EmguCV errors
            {
                grayMat?.Dispose(); // Clean up if partially created
                Console.WriteLine($"EmguCV Error in ConvertToGrayscaleMat: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Grayscale conversion failed due to an internal library error.", cvEx);
            }
            catch (Exception ex) // Handle other unexpected errors
            {
                grayMat?.Dispose(); // Clean up if partially created
                Console.WriteLine($"Error in ConvertToGrayscaleMat: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during grayscale conversion.", ex);
            }
        }

        /// <summary>
        /// Calculates the intensity histogram for a single-channel (grayscale) Mat image.
        /// </summary>
        /// <param name="grayscaleMat">The single-channel grayscale Mat image to analyze.</param>
        /// <returns>An array of 256 integers, where each index represents an intensity level (0-255)
        /// and the value represents the number of pixels at that intensity.</returns>
        /// <exception cref="ArgumentNullException">Thrown if grayscaleMat is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if input is not a valid single-channel image or if calculation fails.</exception>
        public int[] GenerateHistogram(Mat grayscaleMat)
        {
            if (grayscaleMat == null)
                throw new ArgumentNullException(nameof(grayscaleMat), "Input grayscale Mat cannot be null.");
            // Histogram requires a non-empty, single-channel (grayscale) image.
            if (grayscaleMat.IsEmpty || grayscaleMat.NumberOfChannels != 1)
                throw new ImageProcessingException("Input Mat must be a non-empty, single-channel grayscale image for histogram generation.");

            try
            {
                DenseHistogram hist = new DenseHistogram(256, new RangeF(0.0f, 256.0f));

                // Calculate the histogram for the first channel (index 0).
                using (var imgArray = new VectorOfMat(grayscaleMat)) // Wrap Mat in VectorOfMat for CalcHist
                {
                    CvInvoke.CalcHist(imgArray, new int[] { 0 }, null, hist, new int[] { 256 }, new float[] { 0, 256 }, false);
                }

                // Extract the calculated bin counts into a float array.
                float[] histDataFloat = new float[256];
                hist.CopyTo(histDataFloat); // Copy bin values.

                // Convert the float counts to integers for the final result.
                int[] histDataInt = new int[256];
                for (int i = 0; i < histDataFloat.Length; i++)
                {
                    histDataInt[i] = Convert.ToInt32(histDataFloat[i]);
                }
                return histDataInt;
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in GenerateHistogram: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Histogram calculation failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateHistogram: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during histogram generation.", ex);
            }
        }

        /// <summary>
        /// Applies Gaussian blur to smooth an image, reducing noise and detail.
        /// </summary>
        /// <param name="inputMat">The input image (Mat object, can be color or grayscale).</param>
        /// <param name="kernelSize">The size of the square blurring kernel (e.g., 3, 5, 7). Must be positive and odd.</param>
        /// <returns>A new Mat object containing the blurred image. Caller is responsible for disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown if inputMat is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if kernelSize is not positive and odd.</exception>
        /// <exception cref="ImageProcessingException">Thrown if the blurring operation fails.</exception>
        public Mat ApplyGaussianBlur(Mat inputMat, int kernelSize)
        {
            if (inputMat == null) throw new ArgumentNullException(nameof(inputMat));
            if (kernelSize <= 0 || kernelSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(kernelSize), "Kernel size must be positive and odd for GaussianBlur.");

            Mat? blurredMat = null; // Result Mat
            try
            {
                blurredMat = new Mat(); // Create the destination Mat.
                // Apply the Gaussian blur. Setting sigmaX/Y to 0 lets EmguCV calculate them based on kernel size.
                CvInvoke.GaussianBlur(inputMat, blurredMat, new Size(kernelSize, kernelSize), 0, 0);
                return blurredMat; // Return the result; caller must dispose it.
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                blurredMat?.Dispose(); // Clean up if partially created
                Console.WriteLine($"EmguCV Error in ApplyGaussianBlur: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Gaussian blur failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                blurredMat?.Dispose(); // Clean up if partially created
                Console.WriteLine($"Error in ApplyGaussianBlur: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during Gaussian blur.", ex);
            }
        }

        /// <summary>
        /// Performs morphological erosion, which shrinks bright regions and enlarges dark ones.
        /// Useful for removing small white noise or separating connected objects.
        /// </summary>
        /// <param name="inputMat">Input Mat (typically grayscale or binary).</param>
        /// <param name="iterations">How many times to apply the erosion operation.</param>
        /// <returns>A new Mat object with erosion applied. Caller is responsible for disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown if inputMat is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if iterations is not positive.</exception>
        /// <exception cref="ImageProcessingException">Thrown if erosion fails.</exception>
        public Mat ApplyErosion(Mat inputMat, int iterations)
        {
            if (inputMat == null) throw new ArgumentNullException(nameof(inputMat));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");

            Mat? erodedMat = null; // Result Mat
            try
            {
                erodedMat = new Mat(); // Create the destination Mat.
                // Define the structuring element (kernel) used for erosion. A 3x3 rectangle is common.
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                // Apply the erosion operation.
                CvInvoke.Erode(inputMat, erodedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);
                return erodedMat; // Return the result; caller must dispose it.
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                erodedMat?.Dispose(); // Clean up
                Console.WriteLine($"EmguCV Error in ApplyErosion: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Erosion failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                erodedMat?.Dispose(); // Clean up
                Console.WriteLine($"Error in ApplyErosion: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during erosion.", ex);
            }
        }

        /// <summary>
        /// Performs morphological dilation, which expands bright regions and shrinks dark ones.
        /// Useful for filling small holes or connecting nearby bright objects.
        /// </summary>
        /// <param name="inputMat">Input Mat (typically grayscale or binary).</param>
        /// <param name="iterations">How many times to apply the dilation operation.</param>
        /// <returns>A new Mat object with dilation applied. Caller is responsible for disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown if inputMat is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if iterations is not positive.</exception>
        /// <exception cref="ImageProcessingException">Thrown if dilation fails.</exception>
        public Mat ApplyDilation(Mat inputMat, int iterations)
        {
            if (inputMat == null) throw new ArgumentNullException(nameof(inputMat));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");

            Mat? dilatedMat = null; // Result Mat
            try
            {
                dilatedMat = new Mat(); // Create the destination Mat.
                // Define the structuring element (kernel) used for dilation.
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                // Apply the dilation operation.
                CvInvoke.Dilate(inputMat, dilatedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);
                return dilatedMat; // Return the result; caller must dispose it.
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                dilatedMat?.Dispose(); // Clean up
                Console.WriteLine($"EmguCV Error in ApplyDilation: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Dilation failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                dilatedMat?.Dispose(); // Clean up
                Console.WriteLine($"Error in ApplyDilation: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during dilation.", ex);
            }
        }

        /// <summary>
        /// Detects edges in an image using the Canny algorithm, which finds sharp changes in intensity.
        /// Converts the input image to grayscale if it isn't already.
        /// </summary>
        /// <param name="inputMat">Input Mat object.</param>
        /// <param name="threshold1">The lower threshold for the edge detection hysteresis procedure.</param>
        /// <param name="threshold2">The upper threshold for the edge detection hysteresis procedure.</param>
        /// <returns>A new single-channel (binary) Mat showing the detected edges. Caller is responsible for disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown if inputMat is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if edge detection fails.</exception>
        public Mat DetectEdgesCanny(Mat inputMat, double threshold1, double threshold2)
        {
            if (inputMat == null) throw new ArgumentNullException(nameof(inputMat));

            Mat? grayMat = null; // Intermediate grayscale image
            Mat? edgesMat = null; // Result edge map
            try
            {
                grayMat = new Mat();
                // Canny algorithm requires a single-channel grayscale image.
                if (inputMat.NumberOfChannels != 1)
                {
                    // Convert if necessary.
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, grayMat, code);
                }
                else
                {
                    // Input is already single-channel, just copy it.
                    inputMat.CopyTo(grayMat);
                }

                if (grayMat.IsEmpty)
                    throw new ImageProcessingException("Could not obtain valid grayscale image for Canny edge detection.");

                edgesMat = new Mat(); // Create the Mat to store the edge detection result.
                // Apply the Canny algorithm.
                CvInvoke.Canny(grayMat, edgesMat, threshold1, threshold2);
                return edgesMat; // Return the edge map; caller must dispose it.
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                edgesMat?.Dispose(); // Clean up result Mat if created
                Console.WriteLine($"EmguCV Error in DetectEdgesCanny: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Canny edge detection failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                edgesMat?.Dispose(); // Clean up result Mat if created
                Console.WriteLine($"Error in DetectEdgesCanny: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during Canny edge detection.", ex);
            }
            finally
            {
                // Ensure the intermediate grayscale Mat is disposed.
                grayMat?.Dispose();
            }
        }

        /// <summary>
        /// Finds the outlines of shapes in a grayscale or binary image.
        /// Applies thresholding internally to create a binary image, which generally improves contour detection.
        /// </summary>
        /// <param name="inputMat">Input Mat object (grayscale or binary preferred).</param>
        /// <returns>A ContourResult object containing the detected contours (as a VectorOfVectorOfPoint).</returns>
        /// <exception cref="ArgumentNullException">Thrown if inputMat is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if contour detection fails.</exception>
        public ContourResult DetectContours(Mat inputMat)
        {
            if (inputMat == null) throw new ArgumentNullException(nameof(inputMat));

            var result = new ContourResult();
            Mat? processedMat = null; // Intermediate Mat for grayscale/binary version

            try
            {
                processedMat = new Mat();
                // Ensure single-channel image for processing.
                if (inputMat.NumberOfChannels != 1)
                {
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, processedMat, code);
                }
                else
                {
                    inputMat.CopyTo(processedMat); // Already single-channel.
                }

                // Threshold the image to create a binary (black and white) image.
                // FindContours works best on binary images. Adjust threshold value (128) as needed.
                CvInvoke.Threshold(processedMat, processedMat, 128, 255, ThresholdType.Binary);

                if (processedMat.IsEmpty)
                    throw new ImageProcessingException("Could not obtain valid image for contour detection after processing.");

                // Initialize the structure to hold the detected contours.
                result.Contours = new VectorOfVectorOfPoint();
                // The hierarchy Mat is required by FindContours but we don't use it here.
                using Mat hierarchy = new Mat();
                // Find only the external contours (RetrType.External) for simplicity.
                // ChainApproxSimple compresses contours by removing redundant points.
                CvInvoke.FindContours(processedMat, result.Contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                Console.WriteLine($"DetectContours found {result.Contours?.Size ?? 0} contours.");
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in DetectContours: {cvEx.Status} - {cvEx.ErrorMessage}");
                result.Contours = null; // Clear contours on error
                throw new ImageProcessingException("Contour detection failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectContours: {ex.Message}");
                result.Contours = null; // Clear contours on error
                throw new ImageProcessingException("An unexpected error occurred during contour detection.", ex);
            }
            finally
            {
                // Dispose the intermediate Mat used for processing.
                processedMat?.Dispose();
            }
            // Return the result, which might contain an empty or null contour list if detection failed or none were found.
            return result;
        }

        /// <summary>
        /// Draws the detected contour outlines onto a copy of the original image Mat.
        /// </summary>
        /// <param name="originalMat">The original Mat to use as the background.</param>
        /// <param name="contours">The ContourResult object containing the contours to draw.</param>
        /// <returns>A new Mat object with the contours drawn onto it. Caller is responsible for disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown if originalMat or contours object is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if drawing fails.</exception>
        public Mat DrawContours(Mat originalMat, ContourResult contours)
        {
            if (originalMat == null) throw new ArgumentNullException(nameof(originalMat));
            if (contours == null) throw new ArgumentNullException(nameof(contours));

            // Create a clone of the original image to draw on, leaving the input Mat untouched.
            Mat outputMat = originalMat.Clone();

            // If there are no contours to draw, just return the clone.
            if (contours.Contours == null || contours.Contours.Size == 0)
            {
                Console.WriteLine("DrawContours: No contours to draw.");
                return outputMat;
            }

            try
            {
                // Draw all contours found (index -1) onto the output Mat.
                // Uses green color and a thickness of 2 pixels.
                CvInvoke.DrawContours(outputMat, contours.Contours, -1, new MCvScalar(0, 255, 0), 2);
                // Return the image with contours drawn; caller must dispose it.
                return outputMat;
            }
            catch (Emgu.CV.Util.CvException cvEx)
            {
                outputMat.Dispose(); // Dispose the output Mat if drawing failed.
                Console.WriteLine($"EmguCV Error in DrawContours: {cvEx.Status} - {cvEx.ErrorMessage}");
                throw new ImageProcessingException("Contour drawing failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                outputMat.Dispose(); // Dispose the output Mat if drawing failed.
                Console.WriteLine($"Error in DrawContours: {ex.Message}");
                throw new ImageProcessingException("An unexpected error occurred during contour drawing.", ex);
            }
        }
    }
}
