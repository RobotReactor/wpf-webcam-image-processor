using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Defines the contract for services that perform image processing operations,
    /// such as color conversion and analysis.
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>
        /// Converts a given source image to an 8-bit grayscale representation.
        /// </summary>
        /// <param>In this: sourceImage is the original Bitmap image to convert.</param>
        /// <returns>A new Bitmap image containing the grayscale version.</returns>
        Bitmap ConvertToGrayscale(Bitmap sourceImage);

        /// <summary>
        /// Generates a histogram representing the distribution of pixel intensity values
        /// in a grayscale image.
        /// </summary>
        /// <param> grayscaleImage: The 8-bit grayscale Bitmap image to analyze.</param>
        /// <returns>
        /// An array of integers representing the histogram. The index of the array corresponds
        /// to the grayscale intensity value (0-255), and the value at that index
        /// corresponds to the number of pixels with that intensity. Returns null if input is invalid.
        /// </returns>
        int[]? GenerateHistogram(Bitmap grayscaleImage); // Returns int array (index 0-255 = count)
    }
}