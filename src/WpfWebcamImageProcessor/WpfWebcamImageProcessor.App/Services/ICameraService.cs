using System; 
using System.Collections.Generic;
using System.Drawing; 
using System.Threading.Tasks;
using Emgu.CV;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Defines the contract for services that interact with system cameras.
    /// Supports both single image capture and continuous frame streaming.
    /// </summary>
    public interface ICameraService
    {
        /// <summary>
        /// Gets a list of available video capture device names or identifiers.
        /// </summary>
        /// <returns>A list of strings representing available camera devices.</returns>
        Task<IEnumerable<string>> GetAvailableCamerasAsync();

        /// <summary>
        /// Asynchronously captures a single still image frame from the default or selected camera device.
        /// Suitable for one-off captures.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation, containing the captured
        /// image as a Bitmap upon successful completion, or null if capture fails.
        /// </returns>
        Task<Bitmap?> CaptureImageAsync();

        /// <summary>
        /// Starts capturing frames continuously from the default camera.
        /// Captured frames are delivered via the provided callback action.
        /// </summary>
        /// <param name="onFrameReceived">
        /// The action to execute when a new frame is captured.
        /// The Action receives the captured frame as an Emgu.CV.Mat object.
        /// IMPORTANT: The Mat object provided to the callback might be reused by the service;
        /// consumers should process or clone it quickly. The callback will likely be invoked on a background thread.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if capture is already running.</exception>
        void StartCaptureStream(Action<Mat> onFrameReceived);

        /// <summary>
        /// Stops the continuous frame capture stream.
        /// </summary>
        void StopCaptureStream();

        /// <summary>
        /// Gets a value indicating whether the camera stream is currently active.
        /// </summary>
        bool IsStreaming { get; }
    }
}
