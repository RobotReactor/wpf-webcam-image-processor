using System.Drawing;

namespace WpfWebcamImageProcessor.App.Models
{
    /// <summary>
    /// Data Transfer Object (DTO) used to return the results of the
    /// image processing workflow from the service layer to the ViewModel.
    /// Encapsulates the outcome (success/failure) and the resulting data.
    /// </summary>
    public class ImageProcessingResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the overall processing workflow completed successfully.
        /// Defaults to false. Should be set to true only if all essential steps succeed.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Gets or sets an error message if the workflow failed (Success is false).
        /// Null if the operation was successful.
        /// </summary>
        public string? ErrorMessage { get; set; } = null;

        /// <summary>
        /// Gets or sets the original captured image as a Bitmap.
        /// Null if capture failed or if an error occurred before assignment.
        /// The caller (ViewModel) typically takes ownership of this Bitmap if Success is true.
        /// </summary>
        public Bitmap? OriginalBitmap { get; set; }

        /// <summary>
        /// Gets or sets the generated grayscale image as a Bitmap.
        /// Null if grayscale conversion failed or if an error occurred before assignment.
        /// The caller (ViewModel) typically takes ownership of this Bitmap if Success is true.
        /// </summary>
        public Bitmap? GrayscaleBitmap { get; set; }

        /// <summary>
        /// Gets or sets the calculated histogram data (pixel counts per intensity level).
        /// Null if histogram generation failed or if an error occurred before assignment.
        /// </summary>
        public int[]? HistogramData { get; set; }

        // Note: Consider changing Bitmap types to Mat or BitmapSource later
        // if refactoring the internal processing or ViewModel binding strategy.
    }
}
