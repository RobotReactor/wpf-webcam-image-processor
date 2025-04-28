using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO; 
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfWebcamImageProcessor.App.Converters
{
    /// <summary>
    /// Converts a System.Drawing.Bitmap object (commonly used in GDI+)
    /// into a System.Windows.Media.Imaging.BitmapSource object, which is
    /// required for displaying images in WPF controls like Image.
    /// </summary>
    public class BitmapToBitmapSourceConverter : IValueConverter
    {
        /// <summary>
        /// Performs the conversion from Bitmap to BitmapSource.
        /// </summary>
        /// <param name="value">The System.Drawing.Bitmap object to convert.</param>
        /// <param name="targetType">The target type (expected to be ImageSource or BitmapSource).</param>
        /// <param name="parameter">Converter parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>A BitmapSource if conversion is successful; otherwise, null.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input value is actually a Bitmap.
            if (value is not Bitmap bmp)
            {
                return null; // Return null if input is not a Bitmap or is null.
            }

            try
            {
                // Use a MemoryStream to hold the bitmap data temporarily.
                // The 'using' statement ensures the stream is disposed correctly.
                using (MemoryStream memory = new MemoryStream())
                {
                    // Save the Bitmap data to the MemoryStream in a format WPF understands (PNG is lossless and widely supported).
                    bmp.Save(memory, ImageFormat.Png); // Could use Bmp, Jpeg etc. if needed

                    // Reset the stream's position to the beginning so the BitmapImage can read it.
                    memory.Position = 0;

                    // Create a new BitmapImage (a type of BitmapSource).
                    BitmapImage bitmapImage = new BitmapImage();

                    // Begin initialization - required before setting properties like StreamSource.
                    bitmapImage.BeginInit();

                    // Set the source of the BitmapImage to our MemoryStream.
                    bitmapImage.StreamSource = memory;

                    // Cache the image data on load. This is important because the MemoryStream
                    // will be disposed once we exit the 'using' block. Caching ensures the
                    // image data is retained by the BitmapImage object.
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                    // End initialization. The BitmapImage is now ready.
                    bitmapImage.EndInit();

                    // Freeze the BitmapImage. This makes it thread-safe and improves performance,
                    // especially important as it might be accessed from different threads in WPF.
                    bitmapImage.Freeze();

                    // Return the created BitmapSource.
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                // Log the error if conversion fails.
                // Consider using a proper logging framework instead of Console.WriteLine in production.
                Console.WriteLine($"Error converting Bitmap to BitmapSource: {ex.Message}");
                return null; // Return null to indicate failure.
            }
        }

        /// <summary>
        /// Converts a BitmapSource back to a Bitmap. This direction is not implemented
        /// as this converter is intended for one-way binding (ViewModel -> View).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter only works one way (Bitmap -> BitmapSource).
            throw new NotImplementedException("Converting BitmapSource back to Bitmap is not supported by this converter.");
        }
    }
}
