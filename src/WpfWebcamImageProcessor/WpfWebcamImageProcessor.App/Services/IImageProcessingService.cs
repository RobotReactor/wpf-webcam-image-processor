using System.Drawing;

namespace WpfWebcamImageProcessor.App.Services
{
    public interface IImageProcessingService
    {
        /// <summary>
        /// Converts a given source image to an 8-bit grayscale representation.
        /// </summary>
        /// <param name="sourceImage">The original <see cref="Bitmap"/> image to convert.</param> // Updated type
        /// <returns>A new <see cref="Bitmap"/> image containing the grayscale version, or null on error.</returns> // Updated type
        Bitmap? ConvertToGrayscale(Bitmap sourceImage); // Changed parameter and return type to Bitmap?

        /// <summary>
        /// Generates a histogram ... in a grayscale image.
        /// </summary>
        /// <param name="grayscaleImage">The 8-bit grayscale <see cref="Bitmap"/> image to analyze.</param> // Updated type
        /// <returns>An array of integers ... Returns null if input is invalid.</returns>
        int[]? GenerateHistogram(Bitmap grayscaleImage); // Changed parameter type
    }
}