using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;

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
        /// <param name="sourceImage">The original <see cref="BitmapSource"/> image to convert.</param>
        /// <returns>A new <see cref="BitmapSource"/> image containing the grayscale version.</returns>
        BitmapSource? ConvertToGrayscale(BitmapSource sourceImage); // Updated parameter and return type

        /// <summary>
        /// Generates a histogram representing the distribution of pixel intensity values...
        /// </summary>
        /// <param name="grayscaleImage">The 8-bit grayscale <see cref="BitmapSource"/> image to analyze.</param>
        /// <returns>An array of integers representing the histogram... Returns null if input is invalid.</returns>
        int[]? GenerateHistogram(BitmapSource grayscaleImage); // Updated parameter type
    }
}