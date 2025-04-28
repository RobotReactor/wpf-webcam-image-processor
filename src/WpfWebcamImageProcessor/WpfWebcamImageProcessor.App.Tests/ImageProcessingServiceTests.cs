using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using WpfWebcamImageProcessor.App.Services;
using WpfWebcamImageProcessor.App.Exceptions;
using System.Linq; 
using Emgu.CV.Util;

namespace WpfWebcamImageProcessor.Tests
{
    /// <summary>
    /// Unit tests for ImageProcessingService methods.
    /// </summary>
    [TestClass]
    public class ImageProcessingServiceTests
    {
        private ImageProcessingService _imageProcessingService = null!;

        /// <summary>
        /// Runs before each test method to initialize the service instance.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _imageProcessingService = new ImageProcessingService();
        }

        // Convert to grayscale Mat tests

        [TestMethod]
        public void ConvertToGrayscaleMat_WithValidColorBitmap_ReturnsGrayscaleMat()
        {
            // Create a 2x2 bitmap with different colors.
            Bitmap sourceBitmap = new Bitmap(2, 2, PixelFormat.Format24bppRgb);
            sourceBitmap.SetPixel(0, 0, Color.Red);
            sourceBitmap.SetPixel(1, 0, Color.Green);
            sourceBitmap.SetPixel(0, 1, Color.Blue);
            sourceBitmap.SetPixel(1, 1, Color.White);
            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.ConvertToGrayscaleMat(sourceBitmap);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(1, resultMat.NumberOfChannels); // Grayscale should have 1 channel
                Assert.AreEqual(sourceBitmap.Width, resultMat.Cols);
                Assert.AreEqual(sourceBitmap.Height, resultMat.Rows);
                Assert.AreEqual(Emgu.CV.CvEnum.DepthType.Cv8U, resultMat.Depth);
            }
            finally
            {
                sourceBitmap.Dispose();
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConvertToGrayscaleMat_WithNullInput_ThrowsArgumentNullException()
        {
            // Passing null should throw an ArgumentNullException
            Bitmap? sourceBitmap = null;
            _imageProcessingService.ConvertToGrayscaleMat(sourceBitmap!);
        }

        // Generate Histogram tests

        [TestMethod]
        public void GenerateHistogram_WithUniformGrayMat_ReturnsCorrectCounts()
        {
            // Create a solid gray Mat and generate its histogram
            int width = 10, height = 10;
            byte grayValue = 128;
            using Mat grayMat = new Mat(height, width, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            grayMat.SetTo(new MCvScalar(grayValue));
            int[]? histogram = null;

            histogram = _imageProcessingService.GenerateHistogram(grayMat);

            Assert.IsNotNull(histogram);
            Assert.AreEqual(256, histogram.Length);
            Assert.AreEqual(width * height, histogram[grayValue]);

            for (int i = 0; i < histogram.Length; i++)
            {
                if (i != grayValue)
                    Assert.AreEqual(0, histogram[i]);
            }

            Assert.AreEqual(width * height, histogram.Sum());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GenerateHistogram_WithNullMat_ThrowsArgumentNullException()
        {
            // Should throw for null input
            Mat? grayscaleMat = null;
            _imageProcessingService.GenerateHistogram(grayscaleMat!);
        }

        [TestMethod]
        [ExpectedException(typeof(ImageProcessingException))]
        public void GenerateHistogram_WithColorMat_ThrowsImageProcessingException()
        {
            // Histograms should only be generated from grayscale images
            using Mat colorMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            colorMat.SetTo(new MCvScalar(100, 150, 200));
            _imageProcessingService.GenerateHistogram(colorMat);
        }

        // Apply Gaussian Blur tests

        [TestMethod]
        public void ApplyGaussianBlur_WithValidMat_ReturnsBlurredMat()
        {
            using Mat inputMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(128));
            int kernelSize = 3;
            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.ApplyGaussianBlur(inputMat, kernelSize);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(inputMat.Rows, resultMat.Rows);
                Assert.AreEqual(inputMat.Cols, resultMat.Cols);
                Assert.AreEqual(inputMat.NumberOfChannels, resultMat.NumberOfChannels);
                Assert.AreEqual(inputMat.Depth, resultMat.Depth);
            }
            finally
            {
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ApplyGaussianBlur_WithNullMat_ThrowsArgumentNullException()
        {
            Mat? inputMat = null;
            int kernelSize = 3;
            _imageProcessingService.ApplyGaussianBlur(inputMat!, kernelSize);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyGaussianBlur_WithEvenKernelSize_ThrowsArgumentOutOfRangeException()
        {
            // Kernel size must be odd and > 0
            using Mat inputMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(128));
            _imageProcessingService.ApplyGaussianBlur(inputMat, 4); // 4 is invalid
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyGaussianBlur_WithZeroKernelSize_ThrowsArgumentOutOfRangeException()
        {
            using Mat inputMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(128));
            _imageProcessingService.ApplyGaussianBlur(inputMat, 0);
        }

        // Apply erosion tests
        [TestMethod]
        public void ApplyErosion_WithValidMat_ReturnsErodedMat()
        {
            using Mat inputMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(255));
            int iterations = 1;
            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.ApplyErosion(inputMat, iterations);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(inputMat.Rows, resultMat.Rows);
                Assert.AreEqual(inputMat.Cols, resultMat.Cols);
                Assert.AreEqual(inputMat.NumberOfChannels, resultMat.NumberOfChannels);
                Assert.AreEqual(inputMat.Depth, resultMat.Depth);
            }
            finally
            {
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ApplyErosion_WithNullMat_ThrowsArgumentNullException()
        {
            Mat? inputMat = null;
            int iterations = 1;
            _imageProcessingService.ApplyErosion(inputMat!, iterations);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyErosion_WithZeroIterations_ThrowsArgumentOutOfRangeException()
        {
            using Mat inputMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            _imageProcessingService.ApplyErosion(inputMat, 0);
        }

        // Apply dilation tests

        [TestMethod]
        public void ApplyDilation_WithValidMat_ReturnsDilatedMat()
        {
            using Mat inputMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(0));

            // Add a white dot to see the effect of dilation
            using (Image<Gray, byte> inputImage = inputMat.ToImage<Gray, byte>())
            {
                inputImage[5, 5] = new Gray(255);
            }

            int iterations = 1;
            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.ApplyDilation(inputMat, iterations);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(inputMat.Rows, resultMat.Rows);
                Assert.AreEqual(inputMat.Cols, resultMat.Cols);
                Assert.AreEqual(inputMat.NumberOfChannels, resultMat.NumberOfChannels);
                Assert.AreEqual(inputMat.Depth, resultMat.Depth);
            }
            finally
            {
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ApplyDilation_WithNullMat_ThrowsArgumentNullException()
        {
            Mat? inputMat = null;
            int iterations = 1;
            _imageProcessingService.ApplyDilation(inputMat!, iterations);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ApplyDilation_WithZeroIterations_ThrowsArgumentOutOfRangeException()
        {
            using Mat inputMat = new Mat(5, 5, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            _imageProcessingService.ApplyDilation(inputMat, 0);
        }

        // Detect edges canny Tests

        [TestMethod]
        public void DetectEdgesCanny_WithValidMat_ReturnsEdgesMat()
        {
            using Mat inputMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(0));
            CvInvoke.Rectangle(inputMat, new Rectangle(0, 0, 5, 10), new MCvScalar(255), -1);

            double threshold1 = 50;
            double threshold2 = 150;
            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.DetectEdgesCanny(inputMat, threshold1, threshold2);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(inputMat.Rows, resultMat.Rows);
                Assert.AreEqual(inputMat.Cols, resultMat.Cols);
                Assert.AreEqual(1, resultMat.NumberOfChannels);
                Assert.AreEqual(Emgu.CV.CvEnum.DepthType.Cv8U, resultMat.Depth);
            }
            finally
            {
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectEdgesCanny_WithNullMat_ThrowsArgumentNullException()
        {
            Mat? inputMat = null;
            _imageProcessingService.DetectEdgesCanny(inputMat!, 50, 150);
        }

        // Detect contours tests

        [TestMethod]
        public void DetectContours_WithSimpleShape_FindsOneContour()
        {
            using Mat inputMat = new Mat(20, 20, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            inputMat.SetTo(new MCvScalar(0));
            CvInvoke.Rectangle(inputMat, new Rectangle(5, 5, 10, 10), new MCvScalar(255), -1);

            ContourResult? result = null;
            try
            {
                result = _imageProcessingService.DetectContours(inputMat);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Contours);
                Assert.AreEqual(1, result.Contours.Size);
            }
            finally
            {
                result?.Contours?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectContours_WithNullMat_ThrowsArgumentNullException()
        {
            Mat? inputMat = null;
            _imageProcessingService.DetectContours(inputMat!);
        }

        // Draw contours tests

        [TestMethod]
        public void DrawContours_WithValidInput_ReturnsDrawnMat()
        {
            using Mat originalMat = new Mat(20, 20, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            originalMat.SetTo(new MCvScalar(50, 50, 50));

            var contours = new ContourResult();
            contours.Contours = new VectorOfVectorOfPoint(
                new VectorOfPoint(new Point[] { new Point(5, 5), new Point(15, 5), new Point(15, 15), new Point(5, 15) })
            );

            Mat? resultMat = null;
            try
            {
                resultMat = _imageProcessingService.DrawContours(originalMat, contours);

                Assert.IsNotNull(resultMat);
                Assert.AreEqual(originalMat.Rows, resultMat.Rows);
                Assert.AreEqual(originalMat.Cols, resultMat.Cols);
                Assert.AreEqual(originalMat.NumberOfChannels, resultMat.NumberOfChannels);
            }
            finally
            {
                contours?.Contours?.Dispose();
                resultMat?.Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DrawContours_WithNullOriginalMat_ThrowsArgumentNullException()
        {
            Mat? originalMat = null;
            var contours = new ContourResult { Contours = new VectorOfVectorOfPoint() };
            try { _imageProcessingService.DrawContours(originalMat!, contours); }
            finally { contours?.Contours?.Dispose(); }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DrawContours_WithNullContourResult_ThrowsArgumentNullException()
        {
            using Mat originalMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            ContourResult? contours = null;
            _imageProcessingService.DrawContours(originalMat, contours!);
        }
    }
}
