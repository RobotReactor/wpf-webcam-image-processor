using System.Drawing;
using Emgu.CV.Util;

namespace WpfWebcamImageProcessor.App.Services
{

    public class ContourResult
    {
        public VectorOfVectorOfPoint? Contours { get; set; }
    }

    public interface IImageProcessingService
    {
        Bitmap? ConvertToGrayscale(Bitmap sourceImage);
        int[]? GenerateHistogram(Bitmap grayscaleImage);

        /// <summary>
        /// Applies Gaussian blur to an image.
        /// </summary>
        /// <param name="input">The input bitmap.</param>
        /// <param name="kernelSize">The size of the Gaussian kernel (must be positive and odd).</param>
        /// <returns>A new bitmap with the blur applied, or null on error.</returns>
        Bitmap? ApplyGaussianBlur(Bitmap input, int kernelSize);

        /// <summary>
        /// Applies erosion morphological operation.
        /// </summary>
        /// <param name="input">The input bitmap (usually grayscale/binary).</param>
        /// <param name="iterations">Number of times erosion is applied.</param>
        /// <returns>A new bitmap with erosion applied, or null on error.</returns>
        Bitmap? ApplyErosion(Bitmap input, int iterations);

        /// <summary>
        /// Applies dilation morphological operation.
        /// </summary>
        /// <param name="input">The input bitmap (usually grayscale/binary).</param>
        /// <param name="iterations">Number of times dilation is applied.</param>
        /// <returns>A new bitmap with dilation applied, or null on error.</returns>
        Bitmap? ApplyDilation(Bitmap input, int iterations);

        /// <summary>
        /// Detects edges using the Canny algorithm.
        /// </summary>
        /// <param name="input">The input bitmap (usually grayscale).</param>
        /// <param name="threshold1">First threshold for the hysteresis procedure.</param>
        /// <param name="threshold2">Second threshold for the hysteresis procedure.</param>
        /// <returns>A new bitmap showing detected edges, or null on error.</returns>
        Bitmap? DetectEdgesCanny(Bitmap input, double threshold1, double threshold2);

        /// <summary>
        /// Detects contours in a binary or grayscale image.
        /// </summary>
        /// <param name="input">The input bitmap (should ideally be binary for best results).</param>
        /// <returns>A ContourResult object containing detected contours.</returns>
        ContourResult DetectContours(Bitmap input);

        /// <summary>
        /// Draws detected contours onto an image.
        /// </summary>
        /// <param name="original">The image (usually color) to draw contours on.</param>
        /// <param name="contours">The contour data to draw.</param>
        /// <returns>A new bitmap with contours drawn.</returns>
        Bitmap DrawContours(Bitmap original, ContourResult contours);
    }
}
