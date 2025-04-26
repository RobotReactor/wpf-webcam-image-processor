using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WpfWebcamImageProcessor.App.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        public BitmapSource? ConvertToGrayscale(BitmapSource sourceImage)
        {
            return null;  
        }

        public int[]? GenerateHistogram(BitmapSource grayscaleImage)
        {
            return null;

        }

    }
}