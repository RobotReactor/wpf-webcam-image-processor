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
            if (value is not Bitmap bmp)
            {
                return null;
            }

            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bmp.Save(memory, ImageFormat.Png);
                    memory.Position = 0; 

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Bitmap to BitmapSource: {ex.Message}");
                return null; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}