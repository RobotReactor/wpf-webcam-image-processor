using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV; 
using Emgu.CV.CvEnum;
using System.Runtime.Versioning;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Implements the camera service using Emgu CV to capture images from a webcam,
    /// supporting both single captures and continuous streaming.
    /// </summary>
    [SupportedOSPlatform("windows")] // Indicates potential Windows-specific dependencies from EmguCV's native libraries.
    public class CameraService : ICameraService, IDisposable // Implement IDisposable for proper cleanup
    {
        // Use camera index 0, which typically represents the default system webcam.
        private const int DefaultCameraIndex = 0;
        // Specify a preferred camera backend API (DirectShow in this case) for potentially better stability on Windows.
        private const VideoCapture.API PreferredApi = VideoCapture.API.DShow;
        // Holds the active EmguCV VideoCapture object during streaming. Marked volatile for thread safety.
        private volatile VideoCapture? _capture = null;
        // Used to signal cancellation to the background capture loop.
        private CancellationTokenSource? _cts = null;
        // Stores the callback action provided by the consumer to receive captured frames.
        private Action<Mat>? _onFrameReceived = null;
        // Reference to the background task running the capture loop.
        private Task? _captureTask = null;
        // Used to synchronize access when starting or stopping the stream to prevent race conditions.
        private readonly object _startStopLock = new object();
        // Flag to reliably track the active streaming state, used alongside _capture checks. Marked volatile.
        private volatile bool _isStreamingFlag = false;

        /// <summary>
        /// Gets a value indicating whether the camera stream is currently active and ready.
        /// Checks the streaming flag and the status of the VideoCapture object.
        /// </summary>
        public bool IsStreaming => _isStreamingFlag && _capture != null && _capture.IsOpened;

        /// <summary>
        /// Asynchronously captures a single still image frame from the default camera.
        /// Ensures any active stream is stopped before performing the single capture.
        /// </summary>
        /// <returns>A Task resulting in a Bitmap if successful, or null otherwise.</returns>
        public async Task<Bitmap?> CaptureImageAsync()
        {
            // If currently streaming, stop the stream first to avoid conflicts.
            if (_isStreamingFlag)
            {
                Debug.WriteLine("CameraService: Stopping active stream before single capture...");
                StopCaptureStream(); // This sets _isStreamingFlag to false
                // Allow a short time for camera resources to potentially be released.
                await Task.Delay(200);
            }

            Debug.WriteLine("CameraService: Starting single capture...");
            // Perform the capture operation on a background thread.
            return await Task.Run(() =>
            {
                VideoCapture? singleCapture = null;
                try
                {
                    Debug.WriteLine($"CameraService: Creating VideoCapture for single capture (API: {PreferredApi})...");
                    // Attempt to open the camera using the preferred API.
                    singleCapture = new VideoCapture(DefaultCameraIndex, PreferredApi);
                    Thread.Sleep(50); // Brief pause after creation attempt.

                    // If the preferred API failed, try the default API.
                    if (!singleCapture.IsOpened)
                    {
                        Debug.WriteLine($"CameraService: Failed to open with {PreferredApi}, trying default...");
                        singleCapture.Dispose(); // Clean up the failed attempt.
                        singleCapture = new VideoCapture(DefaultCameraIndex); // Try default.
                        Thread.Sleep(50);
                        // If the default API also fails, report error and return null.
                        if (!singleCapture.IsOpened)
                        {
                            Debug.WriteLine($"Error: Unable to open camera with index {DefaultCameraIndex} for single capture (both APIs).");
                            return null;
                        }
                    }
                    Debug.WriteLine("CameraService: Camera opened for single capture.");

                    // Create a Mat object to hold the frame data.
                    using (Mat frame = new Mat())
                    {
                        Debug.WriteLine("CameraService: Reading frame for single capture...");
                        // Read a single frame from the opened camera.
                        if (!singleCapture.Read(frame) || frame.IsEmpty)
                        {
                            Debug.WriteLine("Error: Failed to read frame from camera for single capture.");
                            return null; // Return null if reading fails.
                        }
                        Debug.WriteLine("CameraService: Frame read successfully for single capture.");
                        // Convert the captured Mat frame to a Bitmap for return.
                        return frame.ToBitmap();
                    }
                }
                catch (Exception ex) // Catch any unexpected errors during the process.
                {
                    Debug.WriteLine($"Error during single capture: {ex.Message} - {ex.StackTrace}");
                    return null; // Return null on error.
                }
                finally
                {
                    // Ensure the temporary VideoCapture object is always disposed.
                    Debug.WriteLine("CameraService: Disposing single capture object...");
                    singleCapture?.Dispose();
                    Debug.WriteLine("CameraService: Single capture object disposed.");
                }
            });
        }

        /// <summary>
        /// Starts capturing frames continuously by launching a background task.
        /// The VideoCapture object initialization is performed within the background task.
        /// </summary>
        /// <param name="onFrameReceived">The callback action to execute for each captured frame.</param>
        /// <exception cref="ArgumentNullException">Thrown if onFrameReceived is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the stream fails to start (e.g., camera cannot be opened).</exception>
        public void StartCaptureStream(Action<Mat> onFrameReceived)
        {
            // Use a lock to prevent race conditions if Start/Stop are called concurrently.
            lock (_startStopLock)
            {
                Debug.WriteLine("CameraService: StartCaptureStream entered.");
                // Check if already streaming to prevent multiple streams.
                if (_isStreamingFlag)
                {
                    Debug.WriteLine("Warning: Capture stream already running.");
                    return;
                }

                // Store the provided callback action. Ensure it's not null.
                _onFrameReceived = onFrameReceived ?? throw new ArgumentNullException(nameof(onFrameReceived));

                // Ensure previous resources are cleaned up before starting anew.
                _capture?.Dispose();
                _capture = null;
                _cts?.Dispose();
                _cts = new CancellationTokenSource(); // Create a new cancellation token source.
                var token = _cts.Token;

                // Set the streaming flag immediately. The CaptureLoop will reset it if initialization fails.
                _isStreamingFlag = true;

                Debug.WriteLine("CameraService: CancellationTokenSource created.");
                // Start the CaptureLoop method on a background thread using Task.Run.
                _captureTask = Task.Run(() => CaptureLoop(token), token);
                Debug.WriteLine("CameraService: Capture loop task started.");
            }
        }

        /// <summary>
        /// The core background loop responsible for initializing the camera connection
        /// and continuously grabbing/retrieving frames until cancellation is requested.
        /// This method runs entirely on a background thread.
        /// </summary>
        /// <param name="token">The CancellationToken used to signal when the loop should stop.</param>
        private void CaptureLoop(CancellationToken token)
        {
            Debug.WriteLine("CameraService: CaptureLoop entered.");
            bool initialized = false; // Tracks if camera initialization succeeded within this loop.
            Mat frame = new Mat(); // Reusable Mat object to store retrieved frames.
            VideoCapture? localCapture = null; // Local reference to the capture object created in this loop.
            Action<Mat>? localCallbackRef = _onFrameReceived; // Local reference to the callback action.

            try
            {
                // Attempt to initialize the VideoCapture object within the background task.
                Debug.WriteLine($"CameraService: CaptureLoop - Attempting creation: new VideoCapture({DefaultCameraIndex}, {PreferredApi})");
                localCapture = new VideoCapture(DefaultCameraIndex, PreferredApi);
                Debug.WriteLine($"CameraService: CaptureLoop - VideoCapture instance created (null? {localCapture == null}).");
                Thread.Sleep(100); // Brief pause, sometimes helps hardware initialize.

                // If the preferred API failed, try the default API.
                if (!localCapture.IsOpened)
                {
                    Debug.WriteLine($"CameraService: CaptureLoop - Failed to open with {PreferredApi}. Trying default...");
                    localCapture.Dispose();
                    localCapture = new VideoCapture(DefaultCameraIndex);
                    Thread.Sleep(100);
                    // If default also fails, throw an exception to signal failure.
                    if (!localCapture.IsOpened)
                    {
                        Debug.WriteLine($"CameraService: CaptureLoop - Error: Unable to open camera with index {DefaultCameraIndex}.");
                        localCapture.Dispose(); // Dispose the failed object.
                        throw new InvalidOperationException($"Unable to open camera with index {DefaultCameraIndex}.");
                    }
                    Debug.WriteLine("CameraService: CaptureLoop - Opened successfully with default API.");
                }
                else
                {
                    Debug.WriteLine($"CameraService: CaptureLoop - Opened successfully with {PreferredApi} API.");
                }

                // Assign the successfully created/opened object to the class field.
                _capture = localCapture;
                initialized = true; // Mark initialization as successful.
                Debug.WriteLine("CameraService: CaptureLoop starting while loop.");

                // Continuously process frames until cancellation is requested.
                while (!token.IsCancellationRequested)
                {
                    bool grabbed = false;
                    try
                    {
                        // Grab() gets the next frame from the camera buffer quickly.
                        grabbed = localCapture.Grab();
                    }
                    catch (Exception grabEx) // Catch potential native exceptions during Grab.
                    {
                        Debug.WriteLine($"CameraService: Exception during Grab: {grabEx.Message} - {grabEx.StackTrace}");
                        break; // Exit the loop if grabbing causes an error.
                    }

                    // If grabbing failed, pause briefly and try again.
                    if (!grabbed)
                    {
                        Debug.WriteLine("CameraService: Grab failed, potentially end of stream or temporary error.");
                        Thread.Sleep(50);
                        continue;
                    }

                    // Retrieve() decodes the grabbed frame into the reusable Mat object.
                    if (localCapture.Retrieve(frame) && !frame.IsEmpty)
                    {
                        try
                        {
                            // If retrieve is successful, invoke the callback with the frame data.
                            localCallbackRef?.Invoke(frame);
                        }
                        catch (Exception callbackEx) // Catch errors within the callback code itself.
                        {
                            // Log callback errors but don't stop the capture loop.
                            Debug.WriteLine($"CameraService: Error in onFrameReceived callback: {callbackEx.Message}");
                        }
                    }
                    // If retrieve fails or frame is empty, loop continues to try grabbing next frame.
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when StopCaptureStream signals cancellation.
                Debug.WriteLine("CameraService: CaptureLoop cancelled.");
            }
            catch (Exception ex) // Catch other errors, including initialization failures.
            {
                Debug.WriteLine($"CameraService: Error in CaptureLoop: {ex.Message} - {ex.StackTrace}");
            }
            finally
            {
                // Dispose the reusable Mat frame object.
                frame.Dispose();
                // If initialization failed within this task, ensure the class-level capture object
                // remains null and the streaming flag is reset.
                if (!initialized)
                {
                    _capture = null;
                    _isStreamingFlag = false;
                }
                // If initialization succeeded, the StopCaptureStream method is responsible
                // for disposing the main _capture object.
                Debug.WriteLine("CameraService: CaptureLoop exited.");
            }
        }


        /// <summary>
        /// Stops the continuous frame capture stream safely.
        /// Signals the background loop to stop and cleans up resources.
        /// </summary>
        public void StopCaptureStream()
        {
            // Use lock to prevent conflicts if Stop is called while Start is still initializing.
            lock (_startStopLock)
            {
                Debug.WriteLine("CameraService: StopCaptureStream entered.");
                // Check if the stream is actually running before trying to stop.
                if (!_isStreamingFlag || _cts == null)
                {
                    Debug.WriteLine("CameraService: StopCaptureStream - Stream not active or already stopping.");
                    return;
                }

                Debug.WriteLine("CameraService: Signaling cancellation...");
                try
                {
                    // Request cancellation of the background task.
                    _cts.Cancel();
                }
                catch (ObjectDisposedException) { Debug.WriteLine("CameraService: CTS already disposed in StopCaptureStream."); }
                catch (Exception ex) { Debug.WriteLine($"CameraService: Error cancelling CTS: {ex.Message}"); }

                Debug.WriteLine("CameraService: Waiting for capture task to complete...");
                try
                {
                    // Wait for the background task to finish, with a timeout.
                    bool completed = _captureTask?.Wait(TimeSpan.FromSeconds(2)) ?? true;
                    if (!completed) { Debug.WriteLine("Warning: Capture task did not complete within timeout."); }
                    else { Debug.WriteLine("CameraService: Capture task completed or timed out."); }
                }
                // Catch exceptions related to waiting on a cancelled/disposed task.
                catch (AggregateException aggEx) when (aggEx.InnerExceptions.All(ex => ex is OperationCanceledException || ex is TaskCanceledException))
                { Debug.WriteLine("CameraService: Capture task wait cancelled as expected."); }
                catch (ObjectDisposedException) { Debug.WriteLine("CameraService: Capture task already disposed during Wait."); }
                catch (Exception ex) { Debug.WriteLine($"CameraService: Error waiting for capture task: {ex.Message}"); }
                finally
                {
                    // Regardless of wait outcome, perform cleanup.
                    Debug.WriteLine("CameraService: Disposing capture object in StopCaptureStream...");
                    _capture?.Dispose(); // Dispose the main VideoCapture object.
                    _capture = null;
                    Debug.WriteLine("CameraService: Disposing CancellationTokenSource...");
                    _cts?.Dispose(); // Dispose the cancellation token source.
                    _cts = null;
                    _captureTask = null; // Clear task reference.
                    _onFrameReceived = null; // Clear callback reference.
                    _isStreamingFlag = false; // Update the streaming flag after cleanup.
                    Debug.WriteLine("CameraService: Capture stream stopped and resources released.");
                }
            }
        }

        /// <summary>
        /// Gets a list of available video capture device names or identifiers (placeholder implementation).
        /// </summary>
        /// <returns>A Task containing a list with a placeholder for the default camera.</returns>
        public Task<IEnumerable<string>> GetAvailableCamerasAsync()
        {
            List<string> cameras = new List<string> { $"Default Camera ({DefaultCameraIndex})" };
            return Task.FromResult<IEnumerable<string>>(cameras);
        }

        /// <summary>
        /// Releases resources used by the CameraService, particularly the VideoCapture object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs the actual resource cleanup, ensuring the capture stream is stopped.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            Debug.WriteLine($"CameraService: Dispose({disposing}) called.");
            if (disposing)
            {
                // Ensure streaming is stopped and associated resources are released when the service is disposed.
                StopCaptureStream();
            }
        }
    }
}
