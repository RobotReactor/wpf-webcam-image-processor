using Emgu.CV;

namespace WpfWebcamImageProcessor.App.Models
{
    /// <summary>
    /// A simple data object used to pass the results of the image processing workflow
    /// from the service layer to the ViewModel.
    /// It wraps up whether the processing was successful and holds the resulting data.
    /// Now uses Emgu.CV.Mat for images instead of Bitmap.
    /// </summary>
    public class ImageProcessingResult
    {
        /// <summary>
        /// Tells whether the entire image processing workflow finished successfully.
        /// Defaults to false. Set it to true only if all critical steps completed without errors.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// If something went wrong, this will hold the error message.
        /// It stays null if everything worked fine.
        /// </summary>
        public string? ErrorMessage { get; set; } = null;

        /// <summary>
        /// Holds the original captured image as an Emgu.CV.Mat object.
        /// Will be null if capturing failed or an error happened early on.
        /// NOTE: Mat implements IDisposable. Whoever uses this result object
        /// is responsible for cleaning up (disposing) the Mat when it's no longer needed—
        /// usually when the ViewModel is disposed or a new image replaces it.
        /// </summary>
        public Mat? OriginalMat { get; set; }

        /// <summary>
        /// Holds the grayscale version of the image, also as an Emgu.CV.Mat.
        /// Will be null if the grayscale conversion failed or didn’t happen.
        /// NOTE: Same as OriginalMat, this Mat needs to be disposed properly.
        /// </summary>
        public Mat? GrayscaleMat { get; set; }

        /// <summary>
        /// Holds the histogram data — basically the count of pixels for each intensity level.
        /// Will be null if generating the histogram failed or wasn’t attempted.
        /// </summary>
        public int[]? HistogramData { get; set; }
    }
}
