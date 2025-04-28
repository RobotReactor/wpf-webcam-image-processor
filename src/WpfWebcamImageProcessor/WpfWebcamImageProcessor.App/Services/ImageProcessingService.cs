using System;
using System.Drawing;
using System.Runtime.Versioning; // For platform support attributes
using Emgu.CV;                  // EmguCV main namespace
using Emgu.CV.CvEnum;           // EmguCV enums (like ColorConversion, ElementShape)
using Emgu.CV.Structure;        // EmguCV structures (like MCvScalar)
using Emgu.CV.Util;             // EmguCV utility classes (like VectorOfPoint)
using WpfWebcamImageProcessor.App.Exceptions; // Custom exception class

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Implements image processing operations using the EmguCV library.
    /// Handles tasks like grayscale conversion, histogram calculation, and various filtering effects.
    /// </summary>
    // This service relies on EmguCV, which may have Windows-specific native dependencies.
    [SupportedOSPlatform("windows")]
    public class ImageProcessingService : IImageProcessingService
    {
        /// <summary>
        /// Converts a color or multi-channel bitmap to an 8-bit single-channel grayscale bitmap.
        /// </summary>
        /// <param name="sourceImage">The original bitmap to convert.</param>
        /// <returns>A new grayscale bitmap.</returns>
        /// <exception cref="ArgumentNullException">Thrown if sourceImage is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if the conversion process fails (e.g., empty input Mat, internal EmguCV error).</exception>
        public Bitmap ConvertToGrayscale(Bitmap sourceImage)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage), "Input source image cannot be null.");

            try
            {
                // Convert the input Bitmap to EmguCV's Mat format for processing.
                // The 'using' statement ensures the Mat object is disposed correctly.
                using (Mat inputMat = BitmapExtension.ToMat(sourceImage)) // Requires Emgu.CV.Bitmap package
                {
                    // Check if the conversion resulted in a valid Mat object.
                    if (inputMat.IsEmpty)
                        throw new ImageProcessingException("Input bitmap could not be converted to a valid Mat object (it was empty).");

                    // Prepare a Mat object to hold the grayscale result.
                    using (Mat grayMat = new Mat())
                    {
                        // Determine the appropriate EmguCV color conversion code based on the input image's channels.
                        // Handles 4-channel (BGRA) and 3-channel (BGR) inputs.
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;

                        // Perform the color space conversion.
                        CvInvoke.CvtColor(inputMat, grayMat, code);

                        // Convert the resulting grayscale Mat back to a System.Drawing.Bitmap.
                        return BitmapExtension.ToBitmap(grayMat);
                    }
                }
            }
            catch (CvException cvEx) // Catch specific EmguCV errors
            {
                // Log and wrap EmguCV specific exceptions for better context.
                Console.WriteLine($"EmguCV Error in ConvertToGrayscale: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace with logging
                throw new ImageProcessingException("Grayscale conversion failed due to an internal library error.", cvEx);
            }
            catch (Exception ex) // Catch other potential errors during conversion or disposal.
            {
                Console.WriteLine($"Error in ConvertToGrayscale: {ex.Message}"); // TODO: Replace with logging
                throw new ImageProcessingException("An unexpected error occurred during grayscale conversion.", ex);
            }
        }

        /// <summary>
        /// Calculates the histogram (pixel intensity distribution) for a grayscale image.
        /// Assumes the input is effectively grayscale, but will attempt conversion if needed.
        /// </summary>
        /// <param name="grayscaleImage">The input bitmap, expected to be grayscale.</param>
        /// <returns>An array of 256 integers representing pixel counts for each intensity level (0-255).</returns>
        /// <exception cref="ArgumentNullException">Thrown if grayscaleImage is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if a valid single-channel image cannot be obtained or if histogram calculation fails.</exception>
        public int[] GenerateHistogram(Bitmap grayscaleImage) // Note: Return type changed to non-nullable int[]
        {
            if (grayscaleImage == null)
                throw new ArgumentNullException(nameof(grayscaleImage), "Input grayscale image cannot be null.");

            Mat? convertedGrayMat = null; // Manages disposal if conversion is needed

            try
            {
                using (Mat inputMat = BitmapExtension.ToMat(grayscaleImage))
                {
                    if (inputMat.IsEmpty)
                        throw new ImageProcessingException("Input bitmap converted to an empty Mat, cannot generate histogram.");

                    Mat grayMatToProcess;

                    // Ensure we have a single-channel image for histogram calculation.
                    if (inputMat.NumberOfChannels == 1)
                    {
                        grayMatToProcess = inputMat; // Use directly if already single-channel.
                    }
                    else
                    {
                        // Attempt conversion if input wasn't single-channel.
                        convertedGrayMat = new Mat();
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                        CvInvoke.CvtColor(inputMat, convertedGrayMat, code);
                        grayMatToProcess = convertedGrayMat;
                        Console.WriteLine("Warning: GenerateHistogram input was not grayscale, attempted conversion."); // TODO: Replace log
                    }

                    // Verify the result is usable before proceeding.
                    if (grayMatToProcess.IsEmpty || grayMatToProcess.NumberOfChannels != 1)
                    {
                        throw new ImageProcessingException("Could not obtain a valid single-channel grayscale image for histogram calculation.");
                    }

                    // Initialize a histogram object with 256 bins covering the 0-255 intensity range.
                    DenseHistogram hist = new DenseHistogram(256, new RangeF(0.0f, 256.0f)); // Range is exclusive of the upper bound for CalcHist

                    // Calculate the histogram for the first channel (index 0) of the grayscale image.
                    using (var imgArray = new VectorOfMat(grayMatToProcess))
                    {
                        // Arguments: source array, channels to analyze, mask (none), histogram object, bin counts, ranges, accumulate flag
                        CvInvoke.CalcHist(imgArray, new int[] { 0 }, null, hist, new int[] { 256 }, new float[] { 0, 256 }, false);
                    }

                    // Copy the histogram bin counts directly into a float array.
                    float[] histDataFloat = new float[256];
                    hist.CopyTo(histDataFloat);

                    // Convert the float counts (which should be whole numbers) to integers.
                    int[] histDataInt = new int[256];
                    for (int i = 0; i < histDataFloat.Length; i++)
                    {
                        histDataInt[i] = Convert.ToInt32(histDataFloat[i]);
                    }
                    return histDataInt; // Return the integer array
                }
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in GenerateHistogram: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Histogram calculation failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateHistogram: {ex.Message}"); // TODO: Replace log
                throw new ImageProcessingException("An unexpected error occurred during histogram generation.", ex);
            }
            finally
            {
                // Ensure the intermediate converted Mat is disposed if it was created.
                convertedGrayMat?.Dispose();
            }
        }

        /// <summary>
        /// Applies Gaussian blur using a kernel of the specified size. Smoothing effect.
        /// </summary>
        /// <param name="input">The input bitmap (can be color or grayscale).</param>
        /// <param name="kernelSize">Size of the kernel (must be positive and odd, e.g., 3, 5, 7).</param>
        /// <returns>A new bitmap with Gaussian blur applied.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if kernelSize is not positive and odd.</exception>
        /// <exception cref="ImageProcessingException">Thrown if blurring fails.</exception>
        public Bitmap ApplyGaussianBlur(Bitmap input, int kernelSize)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            // Gaussian kernel dimensions must be positive and odd.
            if (kernelSize <= 0 || kernelSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(kernelSize), "Kernel size must be positive and odd for GaussianBlur.");

            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                using Mat blurredMat = new Mat();
                // Apply the blur. SigmaX/Y = 0 lets EmguCV calculate appropriate sigma values from the kernel size.
                CvInvoke.GaussianBlur(inputMat, blurredMat, new Size(kernelSize, kernelSize), 0, 0);
                return BitmapExtension.ToBitmap(blurredMat);
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in ApplyGaussianBlur: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Gaussian blur failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApplyGaussianBlur: {ex.Message}"); // TODO: Replace log
                throw new ImageProcessingException("An unexpected error occurred during Gaussian blur.", ex);
            }
        }

        /// <summary>
        /// Performs morphological erosion, shrinking bright regions. Useful for removing noise.
        /// </summary>
        /// <param name="input">Input bitmap (typically grayscale or binary).</param>
        /// <param name="iterations">Number of times to apply the erosion operation.</param>
        /// <returns>A new bitmap with erosion applied.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if iterations is not positive.</exception>
        /// <exception cref="ImageProcessingException">Thrown if erosion fails.</exception>
        public Bitmap ApplyErosion(Bitmap input, int iterations)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations), "Number of iterations must be positive.");

            try
            {
                using Mat inputMat = Emgu.CV.BitmapExtension.ToMat(input);
                using Mat erodedMat = new Mat();
                // Use a default 3x3 rectangular structuring element for the morphological operation.
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                // Apply erosion 'iterations' times.
                CvInvoke.Erode(inputMat, erodedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);
                return BitmapExtension.ToBitmap(erodedMat);
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in ApplyErosion: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Erosion failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApplyErosion: {ex.Message}"); // TODO: Replace log
                throw new ImageProcessingException("An unexpected error occurred during erosion.", ex);
            }
        }

        /// <summary>
        /// Performs morphological dilation, expanding bright regions. Useful for closing gaps.
        /// </summary>
        /// <param name="input">Input bitmap (typically grayscale or binary).</param>
        /// <param name="iterations">Number of times to apply the dilation operation.</param>
        /// <returns>A new bitmap with dilation applied.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if iterations is not positive.</exception>
        /// <exception cref="ImageProcessingException">Thrown if dilation fails.</exception>
        public Bitmap ApplyDilation(Bitmap input, int iterations)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations), "Number of iterations must be positive.");

            try
            {
                using Mat inputMat = Emgu.CV.BitmapExtension.ToMat(input);
                using Mat dilatedMat = new Mat();
                // Use a default 3x3 rectangular structuring element.
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                // Apply dilation 'iterations' times.
                CvInvoke.Dilate(inputMat, dilatedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);
                return BitmapExtension.ToBitmap(dilatedMat);
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in ApplyDilation: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Dilation failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApplyDilation: {ex.Message}"); // TODO: Replace log
                throw new ImageProcessingException("An unexpected error occurred during dilation.", ex);
            }
        }

        /// <summary>
        /// Applies the Canny edge detection algorithm to find sharp intensity changes.
        /// Input image is converted to grayscale if it isn't already.
        /// </summary>
        /// <param name="input">Input bitmap.</param>
        /// <param name="threshold1">First threshold for the hysteresis procedure (lower bound).</param>
        /// <param name="threshold2">Second threshold for the hysteresis procedure (upper bound).</param>
        /// <returns>A new binary (black/white) bitmap showing detected edges.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if edge detection fails.</exception>
        public Bitmap DetectEdgesCanny(Bitmap input, double threshold1, double threshold2)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            Mat? grayMat = null; // Manages disposal of intermediate grayscale image

            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                grayMat = new Mat(); // Initialize here

                // Canny requires a single-channel (grayscale) image. Convert if necessary.
                if (inputMat.NumberOfChannels != 1)
                {
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, grayMat, code);
                    Console.WriteLine("Warning: DetectEdgesCanny input was not grayscale, attempted conversion."); // TODO: Replace log
                }
                else
                {
                    inputMat.CopyTo(grayMat); // Input was already suitable.
                }

                if (grayMat.IsEmpty)
                    throw new ImageProcessingException("Could not obtain valid grayscale image for Canny edge detection.");

                // Prepare a Mat object for the Canny output (edges).
                using Mat edgesMat = new Mat();
                // Apply the Canny algorithm.
                CvInvoke.Canny(grayMat, edgesMat, threshold1, threshold2);
                // Convert the resulting edge map back to a Bitmap.
                return BitmapExtension.ToBitmap(edgesMat);
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in DetectEdgesCanny: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Canny edge detection failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectEdgesCanny: {ex.Message}"); // TODO: Replace log
                throw new ImageProcessingException("An unexpected error occurred during Canny edge detection.", ex);
            }
            finally
            {
                // Dispose the intermediate grayscale Mat if it was created.
                grayMat?.Dispose();
            }
        }

        /// <summary>
        /// Finds contours (outlines of shapes) in a grayscale or binary image.
        /// Note: Applying thresholding before this step often yields better results.
        /// </summary>
        /// <param name="input">Input bitmap (should be grayscale or binary).</param>
        /// <returns>A ContourResult object containing the detected contours (VectorOfVectorOfPoint).</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if contour detection fails.</exception>
        public ContourResult DetectContours(Bitmap input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var result = new ContourResult();
            Mat? processedMat = null; // Manages disposal of the image used for detection

            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                processedMat = new Mat();

                // Ensure input is single-channel (grayscale/binary) for FindContours.
                if (inputMat.NumberOfChannels != 1)
                {
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, processedMat, code);
                    Console.WriteLine("Warning: DetectContours input was not grayscale, attempted conversion."); // TODO: Replace log
                }
                else
                {
                    inputMat.CopyTo(processedMat); // Already single channel.
                }

                // OPTIONAL BUT RECOMMENDED: Threshold the image to get a binary map.
                // This generally produces much cleaner contours than running on raw grayscale.
                // Adjust threshold value (e.g., 128) as needed, potentially using Otsu's method.
                CvInvoke.Threshold(processedMat, processedMat, 128, 255, ThresholdType.Binary);

                if (processedMat.IsEmpty)
                    throw new ImageProcessingException("Could not obtain valid image for contour detection after processing.");

                // Initialize the list to store contours.
                result.Contours = new VectorOfVectorOfPoint();
                // Hierarchy matrix is required by FindContours, even if not used later.
                using Mat hierarchy = new Mat();
                // Find only the external contours for simplicity. Use ChainApproxSimple to compress contour points.
                CvInvoke.FindContours(processedMat, result.Contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                Console.WriteLine($"DetectContours found {result.Contours?.Size ?? 0} contours."); // TODO: Replace log
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in DetectContours: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                result.Contours = null; // Indicate failure
                throw new ImageProcessingException("Contour detection failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectContours: {ex.Message}"); // TODO: Replace log
                result.Contours = null; // Indicate failure
                throw new ImageProcessingException("An unexpected error occurred during contour detection.", ex);
            }
            finally
            {
                // Dispose the intermediate processed Mat.
                processedMat?.Dispose();
            }
            return result; // Return result (Contours might be null or empty list)
        }

        /// <summary>
        /// Draws the outlines of detected contours onto a copy of the original image.
        /// </summary>
        /// <param name="original">The original bitmap (typically color) to draw upon.</param>
        /// <param name="contours">The result object containing contours to draw.</param>
        /// <returns>A new bitmap with contours drawn in green.</returns>
        /// <exception cref="ArgumentNullException">Thrown if original or contours object is null.</exception>
        /// <exception cref="ImageProcessingException">Thrown if drawing fails.</exception>
        public Bitmap DrawContours(Bitmap original, ContourResult contours)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (contours == null) throw new ArgumentNullException(nameof(contours));

            // If no contours were provided or detected, return a clone of the original.
            if (contours.Contours == null || contours.Contours.Size == 0)
            {
                Console.WriteLine("DrawContours: No contours to draw."); // TODO: Replace log
                return (Bitmap)original.Clone();
            }

            try
            {
                // Draw onto a Mat representation of the original image.
                // Using the original color image allows drawing colored contours.
                using Mat outputMat = BitmapExtension.ToMat(original);
                // Draw all detected contours (index -1) using a specific color (Green) and thickness.
                CvInvoke.DrawContours(outputMat, contours.Contours, -1, new MCvScalar(0, 255, 0), 2);
                // Convert the modified Mat back to a Bitmap.
                return BitmapExtension.ToBitmap(outputMat);
            }
            catch (CvException cvEx)
            {
                Console.WriteLine($"EmguCV Error in DrawContours: {cvEx.Status} - {cvEx.ErrorMessage}"); // TODO: Replace log
                throw new ImageProcessingException("Contour drawing failed due to an internal library error.", cvEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DrawContours: {ex.Message}"); // TODO: Replace log
                // Return a clone of the original on failure to avoid returning a partially drawn image.
                // Consider logging the error more formally.
                throw new ImageProcessingException("An unexpected error occurred during contour drawing.", ex);
            }
        }
    }
}
