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
    /// Converts a System.Drawing.Bitmap to a System.Windows.Media.Imaging.BitmapSource
    /// for use in WPF Image controls.
    /// </summary>
    public class BitmapToBitmapSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input value is a Bitmap
            if (value is not Bitmap bmp)
            {
                return null; // Or return DependencyProperty.UnsetValue;
            }

            try
            {
                // Use a MemoryStream to temporarily hold the bitmap data
                using (MemoryStream memory = new MemoryStream())
                {
                    // Save the bitmap to the stream in a format WPF understands (e.g., Bmp or Png)
                    // Using Bmp might be slightly faster but doesn't support transparency well. Png is generally good.
                    bmp.Save(memory, ImageFormat.Png);
                    memory.Position = 0; // Rewind the stream to the beginning

                    // Create a BitmapImage from the MemoryStream
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load fully into memory
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // Optional: Freeze for performance benefits if not changing further

                    return bitmapImage; // Return the WPF-compatible BitmapSource
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Bitmap to BitmapSource: {ex.Message}");
                return null; // Or return DependencyProperty.UnsetValue;
            }
        }

        // ConvertBack is usually not needed for one-way bindings to Image Source
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}