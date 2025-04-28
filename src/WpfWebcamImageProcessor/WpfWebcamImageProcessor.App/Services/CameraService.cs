using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV; 
using System.Runtime.Versioning; 

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Provides camera interaction services using the EmguCV library
    /// to capture images from connected webcam devices.
    /// </summary>
    // This attribute indicates the service relies on Windows-specific APIs,
    // likely due to EmguCV's underlying camera access mechanisms on Windows.
    [SupportedOSPlatform("windows")]
    public class CameraService : ICameraService
    {
        // Specifies the index for the default camera. Index 0 usually represents
        // the built-in or first detected webcam by the operating system.
        private const int DefaultCameraIndex = 0;

        /// <summary>
        /// Asynchronously captures a single still image frame from the default camera device.
        /// This implementation creates and disposes the VideoCapture object for each capture,
        /// which is simple but less efficient for continuous capture scenarios.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation. The task result contains the captured
        /// image as a <see cref="Bitmap"/> if successful. Returns null if the camera
        /// cannot be opened or if reading a frame fails. Returning null is chosen here
        /// as camera unavailability might be a common, non-exceptional scenario.
        /// </returns>
        public async Task<Bitmap?> CaptureImageAsync()
        {
            // Offload the potentially blocking camera interaction to a background thread
            // using Task.Run to keep the UI thread responsive.
            return await Task.Run(() =>
            {
                VideoCapture? capture = null;
                try
                {
                    // Attempt to initialize video capture using the default camera index.
                    capture = new VideoCapture(DefaultCameraIndex);

                    // Verify if the camera was successfully opened.
                    if (!capture.IsOpened)
                    {
                        // Log an error if the camera could not be accessed.
                        // TODO: Replace Console.WriteLine with a proper logging mechanism.
                        Console.WriteLine($"Error: Unable to open camera with index {DefaultCameraIndex}");
                        return null; // Indicate failure by returning null.
                    }

                    // Create a Mat object to store the captured frame.
                    // 'using' ensures the Mat object is disposed of properly.
                    using (Mat frame = new Mat())
                    {
                        // Attempt to read a frame from the camera into the Mat object.
                        if (!capture.Read(frame) || frame.IsEmpty)
                        {
                            // Log an error if reading fails or the frame is empty.
                            // TODO: Replace Console.WriteLine with a proper logging mechanism.
                            Console.WriteLine("Error: Failed to read frame from camera.");
                            return null; // Indicate failure by returning null.
                        }

                        // Convert the captured frame (Mat) to a System.Drawing.Bitmap.
                        // Requires the Emgu.CV.Bitmap package.
                        return frame.ToBitmap();
                    }
                }
                catch (Exception ex) // Catch unexpected errors during capture.
                {
                    // Log any exceptions encountered.
                    // TODO: Replace Console.WriteLine with a proper logging mechanism.
                    Console.WriteLine($"Error capturing image: {ex.Message}");
                    return null; // Indicate failure by returning null.
                }
                finally
                {
                    // Crucial: Ensure the VideoCapture object is released, freeing the camera resource,
                    // regardless of whether the capture was successful or an error occurred.
                    capture?.Dispose();
                }
            });
        }

        /// <summary>
        /// Gets a list of available video capture device names or identifiers.
        /// NOTE: This is a basic placeholder implementation. EmguCV (or underlying libraries)
        /// often lacks a straightforward, cross-platform way to get user-friendly camera names.
        /// A more robust implementation might involve platform-specific APIs (like DirectShow or Media Foundation on Windows)
        /// or attempting to open devices at indices 0, 1, 2, etc., to check for availability.
        /// </summary>
        /// <returns>A Task containing a list with just a placeholder for the default camera.</returns>
        public Task<IEnumerable<string>> GetAvailableCamerasAsync()
        {
            // Provides a minimal list, sufficient for scenarios using only the default camera.
            List<string> cameras = new List<string> { $"Default Camera ({DefaultCameraIndex})" };
            // Return the list wrapped in a completed Task to match the async interface method signature.
            return Task.FromResult<IEnumerable<string>>(cameras);
        }
    }
}
