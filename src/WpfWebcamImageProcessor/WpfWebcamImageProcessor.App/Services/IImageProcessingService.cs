using Emgu.CV;
using Emgu.CV.Util;
using System.Drawing;

namespace WpfWebcamImageProcessor.App.Services
{
    // A simple result class to hold detected contours after processing
    public class ContourResult
    {
        public VectorOfVectorOfPoint? Contours { get; set; }
    }

    /// <summary>
    /// Defines what an image processing service should do.
    /// All methods work mainly with Emgu.CV.Mat objects instead of Bitmaps internally.
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>
        /// Takes a Bitmap (like from a camera) and turns it into a grayscale Mat.
        /// </summary>
        /// <param name="sourceBitmap">The original captured Bitmap image.</param>
        /// <returns>A new grayscale Mat (single-channel).</returns>
        /// <exception cref="System.ArgumentNullException">If sourceBitmap is null.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If something goes wrong during conversion.</exception>
        Mat ConvertToGrayscaleMat(Bitmap sourceBitmap);

        /// <summary>
        /// Builds a histogram from a grayscale Mat image.
        /// </summary>
        /// <param name="grayscaleMat">The single-channel grayscale Mat.</param>
        /// <returns>An array with 256 values — one for each possible pixel intensity.</returns>
        /// <exception cref="System.ArgumentNullException">If grayscaleMat is null.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If the input isn't grayscale or the calculation fails.</exception>
        int[] GenerateHistogram(Mat grayscaleMat);

        // --- Filter Methods ---

        /// <summary>
        /// Applies a Gaussian blur to a Mat image to smooth it out.
        /// </summary>
        /// <param name="inputMat">The input Mat (grayscale or color).</param>
        /// <param name="kernelSize">Size of the blur kernel (must be positive and odd).</param>
        /// <returns>A new blurred Mat.</returns>
        /// <exception cref="System.ArgumentNullException">If inputMat is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">If kernelSize is invalid.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If blurring fails.</exception>
        Mat ApplyGaussianBlur(Mat inputMat, int kernelSize);

        /// <summary>
        /// Applies erosion to a Mat image (shrinks white areas, expands black).
        /// </summary>
        /// <param name="inputMat">The input Mat (usually grayscale or binary).</param>
        /// <param name="iterations">How many times to apply erosion.</param>
        /// <returns>A new Mat with erosion applied.</returns>
        /// <exception cref="System.ArgumentNullException">If inputMat is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">If iterations is invalid.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If erosion fails.</exception>
        Mat ApplyErosion(Mat inputMat, int iterations);

        /// <summary>
        /// Applies dilation to a Mat image (grows white areas, shrinks black).
        /// </summary>
        /// <param name="inputMat">The input Mat (usually grayscale or binary).</param>
        /// <param name="iterations">How many times to apply dilation.</param>
        /// <returns>A new Mat with dilation applied.</returns>
        /// <exception cref="System.ArgumentNullException">If inputMat is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">If iterations is invalid.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If dilation fails.</exception>
        Mat ApplyDilation(Mat inputMat, int iterations);

        /// <summary>
        /// Runs Canny edge detection on a Mat image (it'll convert to grayscale internally if needed).
        /// </summary>
        /// <param name="inputMat">The input Mat.</param>
        /// <param name="threshold1">First threshold for detecting edges.</param>
        /// <param name="threshold2">Second threshold for detecting edges.</param>
        /// <returns>A new Mat showing the edges.</returns>
        /// <exception cref="System.ArgumentNullException">If inputMat is null.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If edge detection fails.</exception>
        Mat DetectEdgesCanny(Mat inputMat, double threshold1, double threshold2);

        /// <summary>
        /// Finds contours (outlines) in a grayscale or binary Mat image.
        /// </summary>
        /// <param name="inputMat">The input Mat (should be binary for best results).</param>
        /// <returns>A ContourResult object with the found contours.</returns>
        /// <exception cref="System.ArgumentNullException">If inputMat is null.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If contour detection fails.</exception>
        ContourResult DetectContours(Mat inputMat);

        /// <summary>
        /// Draws detected contours onto a copy of the original Mat image.
        /// </summary>
        /// <param name="originalMat">The color Mat image to draw on (a clone is modified, not the original).</param>
        /// <param name="contours">The contours to draw.</param>
        /// <returns>A new Mat with the contours drawn.</returns>
        /// <exception cref="System.ArgumentNullException">If originalMat or contours is null.</exception>
        /// <exception cref="WpfWebcamImageProcessor.App.Exceptions.ImageProcessingException">If drawing fails.</exception>
        Mat DrawContours(Mat originalMat, ContourResult contours);
    }
}
