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
    /// Converts a System.Drawing.Bitmap object into a WPF-compatible
    /// System.Windows.Media.Imaging.BitmapSource. This allows Bitmaps,
    /// often used by imaging libraries or older frameworks, to be displayed
    /// in WPF Image controls.
    /// </summary>
    public class BitmapToBitmapSourceConverter : IValueConverter
    {
        /// <summary>
        /// Converts the source Bitmap to a BitmapSource.
        /// </summary>
        /// <param name="value">The input value, expected to be a System.Drawing.Bitmap.</param>
        /// <param name="targetType">The type of the binding target property (not used by this converter).</param>
        /// <param name="parameter">An optional converter parameter (not used by this converter).</param>
        /// <param name="culture">The culture to use in the converter (not used by this converter).</param>
        /// <returns>A BitmapSource representation of the input Bitmap, or null if the input is invalid or conversion fails.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the input is a valid Bitmap object before proceeding.
            if (value is not Bitmap bmp)
            {
                return null;
            }

            try
            {
                // Use an in-memory stream to temporarily hold the image data during conversion.
                // The using statement guarantees the stream is disposed of properly afterwards.
                using (MemoryStream memory = new MemoryStream())
                {
                    // Save the bitmap's data into the memory stream. PNG format is chosen here
                    // as it supports transparency and is lossless, making it a good intermediate format.
                    bmp.Save(memory, ImageFormat.Png);

                    // Reset the stream's read position to the beginning. This is essential
                    // so that the BitmapImage can read the data we just saved.
                    memory.Position = 0;

                    // Create a BitmapImage, which is a WPF-specific BitmapSource implementation.
                    BitmapImage bitmapImage = new BitmapImage();

                    // Begin the initialization process. Properties like StreamSource must be set
                    // between BeginInit() and EndInit().
                    bitmapImage.BeginInit();

                    // Point the BitmapImage to the memory stream containing the PNG data.
                    bitmapImage.StreamSource = memory;

                    // Tell the BitmapImage to load the image data immediately and close the stream.
                    // This is crucial because the MemoryStream will be disposed when the 'using' block ends.
                    // Caching ensures the image data is fully loaded into the BitmapImage object.
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                    // Finalize the initialization.
                    bitmapImage.EndInit();

                    // Freeze the BitmapImage to make it immutable and thread-safe. This is recommended
                    // for objects used in WPF data binding, improving performance and preventing cross-thread issues.
                    bitmapImage.Freeze();

                    // Return the fully loaded and prepared BitmapSource.
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                // If any error occurs during the conversion process, log it for debugging.
                Console.WriteLine($"Error converting Bitmap to BitmapSource: {ex.Message}");
                // Return null to indicate the conversion failed. The binding will likely show nothing.
                return null;
            }
        }

        /// <summary>
        /// Converting from BitmapSource back to Bitmap is not supported by this converter.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is designed for one-way data flow from a Bitmap source to a BitmapSource target.
            throw new NotImplementedException("BitmapSource to Bitmap conversion is not implemented.");
        }
    }
}
