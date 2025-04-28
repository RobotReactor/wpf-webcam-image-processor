using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;

namespace WpfWebcamImageProcessor.App.Converters
{
    /// <summary>
    /// Converts an Emgu.CV.Mat object into a System.Windows.Media.Imaging.BitmapSource
    /// suitable for display in WPF Image controls. This conversion allows using EmguCV's
    /// image representation within the WPF user interface.
    /// </summary>
    public class MatToBitmapSourceConverter : IValueConverter
    {
        /// <summary>
        /// Performs the conversion from an EmguCV Mat to a WPF BitmapSource.
        /// </summary>
        /// <param name="value">The input value, expected to be an Emgu.CV.Mat object.</param>
        /// <param name="targetType">The target type for the conversion (not directly used).</param>
        /// <param name="parameter">An optional parameter for the converter (not used).</param>
        /// <param name="culture">The culture information for the conversion (not used).</param>
        /// <returns>A BitmapSource if conversion is successful; otherwise, null.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // First, validate the input is a usable Mat object.
            if (value is not Mat mat || mat.IsEmpty || mat.Ptr == IntPtr.Zero)
            {
                // Return null if the input is null, not a Mat, empty, or invalid.
                return null;
            }

            Bitmap? tempBitmap = null; // Holds the intermediate System.Drawing.Bitmap
            try
            {
                // Convert the EmguCV Mat to a System.Drawing.Bitmap.
                // This relies on the Emgu.CV.Bitmap helper extensions.
                tempBitmap = mat.ToBitmap();

                // If the intermediate conversion fails, we cannot proceed.
                if (tempBitmap == null)
                {
                    Console.WriteLine("MatToBitmapSourceConverter: Mat.ToBitmap() returned null.");
                    return null;
                }

                // Convert the System.Drawing.Bitmap to a WPF BitmapSource using a MemoryStream.
                // This is a standard way to bridge between System.Drawing and System.Windows.Media.
                using (MemoryStream memory = new MemoryStream())
                {
                    // Save the bitmap data into the stream using PNG format.
                    // PNG is generally a good choice as it's lossless and supports transparency.
                    tempBitmap.Save(memory, ImageFormat.Png);

                    // Reset the stream position to the beginning so it can be read.
                    memory.Position = 0;

                    // Create the WPF BitmapImage object.
                    BitmapImage bitmapImage = new BitmapImage();
                    // Initialize the BitmapImage before setting its properties.
                    bitmapImage.BeginInit();
                    // Set the stream containing the image data as the source.
                    bitmapImage.StreamSource = memory;
                    // Load the image data immediately and close the stream afterwards.
                    // This is necessary because the MemoryStream will be disposed.
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    // Finalize the initialization.
                    bitmapImage.EndInit();
                    // Freeze the object to make it immutable and thread-safe for WPF's UI thread.
                    bitmapImage.Freeze();

                    // Return the resulting BitmapSource.
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                // Log errors that occur during the conversion process.
                Console.WriteLine($"Error converting Mat to BitmapSource: {ex.Message}");
                // Indicate failure by returning null.
                return null;
            }
            finally
            {
                // Ensure the intermediate Bitmap object is disposed to release resources.
                tempBitmap?.Dispose();
            }
        }

        /// <summary>
        /// Converts a BitmapSource back to a Mat. This conversion direction is not implemented.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown because the conversion is not supported.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is designed only for Mat -> BitmapSource conversion.
            throw new NotImplementedException("Converting BitmapSource back to Mat is not supported.");
        }
    }
}
