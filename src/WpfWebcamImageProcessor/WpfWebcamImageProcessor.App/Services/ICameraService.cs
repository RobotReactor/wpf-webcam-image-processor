using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Defines the contract for services that interact with system cameras
    /// to capture images.
    /// </summary>
    public interface ICameraService
    {
        /// <summary>
        /// Gets a list of available video capture device names or identifiers.
        /// </summary>
        /// <returns>A list of strings representing available camera devices.</returns>
        /// <remarks>
        /// This might be needed later for allowing the user to select a camera. 
        /// I only have one camera for now, so this is basically a placeholder.
        /// </remarks>
        Task<IEnumerable<string>> GetAvailableCamerasAsync(); // Example method for later

        /// <summary>
        /// Asynchronously captures a single still image frame from the default or selected camera device.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation, containing the captured
        /// image as a Bitmap upon successful completion.
        /// Returns null if capture fails or no camera is available.
        /// </returns>
        Task<Bitmap?> CaptureImageAsync(); // Using nullable Bitmap (Bitmap?) for safety
    }
}
