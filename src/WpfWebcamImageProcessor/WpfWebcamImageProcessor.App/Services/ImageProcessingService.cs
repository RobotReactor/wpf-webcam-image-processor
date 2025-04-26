using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace WpfWebcamImageProcessor.App.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        /// <summary>
        /// Converts a given source image to an 8-bit grayscale representation using Emgu CV.
        /// </summary>
        /// <param name="sourceImage">The original <see cref="Bitmap"/> image to convert.</param>
        /// <returns>A new <see cref="Bitmap"/> image containing the grayscale version, or null if conversion fails.</returns>
        public Bitmap? ConvertToGrayscale(Bitmap sourceImage)
        {
            if (sourceImage == null) return null;

            try
            {
                // Convert System.Drawing.Bitmap to Emgu CV Mat
                using (Mat inputMat = sourceImage.ToMat()) 
                {
                    if (inputMat.IsEmpty) return null;

                    // Prepare output Mat
                    using (Mat grayMat = new Mat())
                    {
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                        CvInvoke.CvtColor(inputMat, grayMat, code);

                        return grayMat.ToBitmap(); 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConvertToGrayscale: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a histogram for a grayscale image using Emgu CV.
        /// </summary>
        /// <param name="grayscaleImage">The 8-bit grayscale <see cref="Bitmap"/> image to analyze.</param>
        /// <returns>An array of 256 integers representing the histogram, or null if generation fails.</returns>
        public int[]? GenerateHistogram(Bitmap grayscaleImage) // Now takes Bitmap
        {
            if (grayscaleImage == null) return null;

            // Check if image is already grayscale format
            bool needsConversion = grayscaleImage.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

            try
            {
                using (Mat inputMat = grayscaleImage.ToMat())
                {
                    if (inputMat.IsEmpty)
                    {
                        Console.WriteLine("Error: Input bitmap converted to an empty Mat.");
                        return null;
                    }

                    Mat grayMatToProcess;
                    Mat? convertedGrayMat = null;

                    // Determine if we need to convert, and assign to grayMatToProcess
                    if (inputMat.NumberOfChannels == 1)
                    {
                        grayMatToProcess = inputMat; // Use input directly if already grayscale
                    }
                    else
                    {
                        // Convert if input had multiple channels
                        convertedGrayMat = new Mat(); 
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                        CvInvoke.CvtColor(inputMat, convertedGrayMat, code);
                        grayMatToProcess = convertedGrayMat; 
                    }

                    // Now check the Mat we intend to process *after* any potential conversion
                    if (grayMatToProcess.IsEmpty || grayMatToProcess.NumberOfChannels != 1)
                    {
                        Console.WriteLine("Error: Could not obtain a valid single-channel grayscale image for histogram.");
                        convertedGrayMat?.Dispose(); // Dispose the intermediate if it was created
                        return null;
                    }

                    // --- Histogram Calculation ---
                    DenseHistogram hist = new DenseHistogram(256, new RangeF(0.0f, 256.0f));
                    using (var imgArray = new VectorOfMat(grayMatToProcess))
                    {
                        CvInvoke.CalcHist(imgArray, new int[] { 0 }, null, hist, new int[] { 256 }, new float[] { 0, 256 }, false);
                    }

                    // --- Extract Data ---
                    float[] histDataFloat;
                    using (VectorOfFloat vec = new VectorOfFloat())
                    {
                        hist.CopyTo(vec);
                        histDataFloat = vec.ToArray();
                    }

                    if (histDataFloat == null || histDataFloat.Length != 256)
                    {
                        Console.WriteLine($"Error: Histogram data extraction resulted in unexpected array size: {histDataFloat?.Length ?? 0}");
                        convertedGrayMat?.Dispose();
                        return null;
                    }

                    // --- Convert to int[] ---
                    int[] histDataInt = new int[256];
                    for (int i = 0; i < histDataFloat.Length; i++) 
                    {
                        histDataInt[i] = Convert.ToInt32(histDataFloat[i]);
                    }

                    convertedGrayMat?.Dispose();
                    return histDataInt; // Return the final int array
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateHistogram: {ex.Message}"); 
                return null;
            }
        }
    }
}