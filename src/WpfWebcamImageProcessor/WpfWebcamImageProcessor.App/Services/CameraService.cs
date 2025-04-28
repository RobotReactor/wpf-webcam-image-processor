using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV; 
using Emgu.CV.CvEnum; 
using System.Runtime.Versioning;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Implements the camera service using Emgu CV to capture images from a webcam.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class CameraService : ICameraService
    {
        // Using camera index 0 typically refers to the default system webcam.
        private const int DefaultCameraIndex = 0;

        /// <summary>
        /// Asynchronously captures a single still image frame from the default camera device.
        /// Creates and disposes the VideoCapture object for each capture in this simple implementation.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation, containing the captured
        /// image as a <see cref="Bitmap"/> upon successful completion.
        /// Returns null if capture fails or no camera is available.
        /// </returns>
        public async Task<Bitmap?> CaptureImageAsync()
        {
            // Run the blocking capture logic on a background thread
            return await Task.Run(() =>
            {
                VideoCapture? capture = null;
                try
                {
                    // Create a VideoCapture object for the default camera
                    // The 'using' statement ensures it gets disposed correctly
                    capture = new VideoCapture(DefaultCameraIndex);

                    // Check if the camera opened successfully
                    if (!capture.IsOpened)
                    {
                        Console.WriteLine($"Error: Unable to open camera with index {DefaultCameraIndex}");
                        return null;
                    }

                    using (Mat frame = new Mat())
                    {
                        if (!capture.Read(frame) || frame.IsEmpty)
                        {
                            Console.WriteLine("Error: Failed to read frame from camera.");
                            return null;
                        }

                        return frame.ToBitmap();
                    }
                }
                catch (Exception ex)
                {
                    // Log any exceptions during capture
                    Console.WriteLine($"Error capturing image: {ex.Message}");
                    return null;
                }
                finally
                {
                    // Ensure the capture device is released
                    capture?.Dispose();
                }
            });
        }

        /// <summary>
        /// Gets a list of available video capture device names or identifiers.
        /// NOTE: Basic implementation - EmguCV doesn't have a simple cross-platform
        /// way to enumerate friendly names. This just returns the default index.
        /// A real implementation might try opening index 0, 1, 2... etc.
        /// </summary>
        /// <returns>A list containing just the default camera index as a string.</returns>
        public Task<IEnumerable<string>> GetAvailableCamerasAsync()
        {
            // Placeholder implementation
            List<string> cameras = new List<string> { $"Default Camera ({DefaultCameraIndex})" };
            // Wrap in a completed task to match the async signature
            return Task.FromResult<IEnumerable<string>>(cameras);
        }
    }
}