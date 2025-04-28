using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Runtime.Versioning;

namespace WpfWebcamImageProcessor.App.Services
{
    [SupportedOSPlatform("windows")]
    public class ImageProcessingService : IImageProcessingService
    {
        public Bitmap? ConvertToGrayscale(Bitmap sourceImage)
        {
            if (sourceImage == null) return null;
            try
            {
                using (Mat inputMat = Emgu.CV.BitmapExtension.ToMat(sourceImage))
                {
                    if (inputMat.IsEmpty) return null;
                    using (Mat grayMat = new Mat())
                    {
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                        CvInvoke.CvtColor(inputMat, grayMat, code);
                        return Emgu.CV.BitmapExtension.ToBitmap(grayMat);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error in ConvertToGrayscale: {ex.Message}"); return null; }
        }

        public int[]? GenerateHistogram(Bitmap grayscaleImage)
        {
            if (grayscaleImage == null) return null;
            Mat? convertedGrayMat = null;

            try
            {
                using (Mat inputMat = BitmapExtension.ToMat(grayscaleImage))
                {
                    if (inputMat.IsEmpty) { Console.WriteLine("Error: Input bitmap converted to an empty Mat."); return null; }

                    Mat grayMatToProcess;


                    if (inputMat.NumberOfChannels == 1)
                    {
                        grayMatToProcess = inputMat;
                    }
                    else 
                    {
                        convertedGrayMat = new Mat();
                        ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                        CvInvoke.CvtColor(inputMat, convertedGrayMat, code);
                        grayMatToProcess = convertedGrayMat;
                    }

                    if (grayMatToProcess.IsEmpty || grayMatToProcess.NumberOfChannels != 1)
                    {
                        Console.WriteLine("Error: Could not obtain a valid single-channel grayscale image for histogram.");
                        return null;
                    }

                    DenseHistogram hist = new DenseHistogram(256, new RangeF(0.0f, 256.0f));
                    using (var imgArray = new VectorOfMat(grayMatToProcess))
                    {
                        CvInvoke.CalcHist(imgArray, new int[] { 0 }, null, hist, new int[] { 256 }, new float[] { 0, 256 }, false);
                    }

                    float[] histDataFloat = new float[256];
                    try
                    {
                        hist.CopyTo(histDataFloat);
                    }
                    catch (Exception copyEx)
                    {
                        Console.WriteLine($"Error copying histogram data: {copyEx.Message}");
                        return null;
                    }


                    if (histDataFloat == null || histDataFloat.Length != 256) 
                    {
                        Console.WriteLine($"Error: Histogram data extraction resulted in unexpected array size or null.");
                        return null;
                    }

                    int[] histDataInt = new int[256];
                    for (int i = 0; i < histDataFloat.Length; i++) { histDataInt[i] = Convert.ToInt32(histDataFloat[i]); }
                    return histDataInt;
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error in GenerateHistogram: {ex.Message}"); return null; }
            finally
            {
                convertedGrayMat?.Dispose();
            }
        }

        public Bitmap? ApplyGaussianBlur(Bitmap input, int kernelSize)
        {
            if (input == null) return null;
            if (kernelSize <= 0 || kernelSize % 2 == 0) { Console.WriteLine($"ApplyGaussianBlur: Invalid kernel size {kernelSize}. Must be positive and odd."); return null; }
            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                using Mat blurredMat = new Mat();
                CvInvoke.GaussianBlur(inputMat, blurredMat, new Size(kernelSize, kernelSize), 0, 0);

                return BitmapExtension.ToBitmap(blurredMat);
            }
            catch (Exception ex) { Console.WriteLine($"Error in ApplyGaussianBlur: {ex.Message}"); return null; }
        }

        public Bitmap? ApplyErosion(Bitmap input, int iterations)
        {
            if (input == null || iterations <= 0) return null;
            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                using Mat erodedMat = new Mat();
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                CvInvoke.Erode(inputMat, erodedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);

                return BitmapExtension.ToBitmap(erodedMat);
            }
            catch (Exception ex) { Console.WriteLine($"Error in ApplyErosion: {ex.Message}"); return null; }
        }

        public Bitmap? ApplyDilation(Bitmap input, int iterations)
        {
            if (input == null || iterations <= 0) return null;
            try
            {
                using Mat inputMat = Emgu.CV.BitmapExtension.ToMat(input);
                using Mat dilatedMat = new Mat();
                using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                CvInvoke.Dilate(inputMat, dilatedMat, kernel, new Point(-1, -1), iterations, BorderType.Default, default);

                return BitmapExtension.ToBitmap(dilatedMat);
            }
            catch (Exception ex) { Console.WriteLine($"Error in ApplyDilation: {ex.Message}"); return null; }
        }

        public Bitmap? DetectEdgesCanny(Bitmap input, double threshold1, double threshold2)
        {
            if (input == null) return null;
            Mat? grayMat = null; 
            try
            {

                using Mat inputMat = BitmapExtension.ToMat(input);
                grayMat = new Mat(); 

                if (inputMat.NumberOfChannels != 1)
                {
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, grayMat, code);
                }
                else
                {
                    inputMat.CopyTo(grayMat); 
                }

                using Mat edgesMat = new Mat();
                CvInvoke.Canny(grayMat, edgesMat, threshold1, threshold2);
                return Emgu.CV.BitmapExtension.ToBitmap(edgesMat);
            }
            catch (Exception ex) { Console.WriteLine($"Error in DetectEdgesCanny: {ex.Message}"); return null; }
            finally
            {
                grayMat?.Dispose(); 
            }
        }

        public ContourResult DetectContours(Bitmap input)
        {
            var result = new ContourResult();
            if (input == null) return result;
            Mat? grayMat = null;
            try
            {
                using Mat inputMat = BitmapExtension.ToMat(input);
                grayMat = new Mat();

                if (inputMat.NumberOfChannels != 1)
                {
                    ColorConversion code = inputMat.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray;
                    CvInvoke.CvtColor(inputMat, grayMat, code);

                }
                else
                {
                    inputMat.CopyTo(grayMat);
                }


                result.Contours = new VectorOfVectorOfPoint();
                using Mat hierarchy = new Mat();
                CvInvoke.FindContours(grayMat, result.Contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            }
            catch (Exception ex) { Console.WriteLine($"Error in DetectContours: {ex.Message}"); result.Contours = null; }
            finally
            {
                grayMat?.Dispose(); 
            }
            return result;
        }

        public Bitmap DrawContours(Bitmap original, ContourResult contours)
        {
            if (original == null) return new Bitmap(1, 1); 
            if (contours?.Contours == null || contours.Contours.Size == 0) return (Bitmap)original.Clone();

            try
            {
                using Mat outputMat = Emgu.CV.BitmapExtension.ToMat(original);
                CvInvoke.DrawContours(outputMat, contours.Contours, -1, new MCvScalar(0, 255, 0), 2);
                return Emgu.CV.BitmapExtension.ToBitmap(outputMat);
            }
            catch (Exception ex) { Console.WriteLine($"Error in DrawContours: {ex.Message}"); return (Bitmap)original.Clone(); }
        }
    }
}
