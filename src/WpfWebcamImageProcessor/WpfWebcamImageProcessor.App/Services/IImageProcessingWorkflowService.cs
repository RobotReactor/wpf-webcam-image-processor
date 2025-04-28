using System.Threading.Tasks; 
using WpfWebcamImageProcessor.App.Models; 

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Defines the contract for a service that orchestrates the
    /// complete workflow of capturing an image, processing it (e.g., grayscale),
    /// and generating associated data (e.g., histogram).
    /// This decouples the multi-step process logic from the ViewModel.
    /// </summary>
    public interface IImageProcessingWorkflowService
    {
        /// <summary>
        /// Asynchronously executes the entire image processing workflow.
        /// </summary>
        /// <returns>
        /// A Task representing the asynchronous operation. The task result is an
        /// <see cref="ImageProcessingResult"/> object containing the outcome
        /// (success/failure), processed images (original, grayscale), histogram data,
        /// and any error messages.
        /// </returns>
        Task<ImageProcessingResult> ExecuteProcessingAsync();
    }
}
