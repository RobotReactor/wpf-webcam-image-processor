using System.Drawing; 

namespace WpfWebcamImageProcessor.App.Models 
{

    public class ImageProcessingResult
    {
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; } = null;

        public Bitmap? OriginalBitmap { get; set; }
        public Bitmap? GrayscaleBitmap { get; set; }
        public int[]? HistogramData { get; set; }

    }
}