using WpfWebcamImageProcessor.App.Models;

namespace WpfWebcamImageProcessor.App.Services
{
    public interface IImageProcessingWorkflowService
    {
        Task<ImageProcessingResult> ExecuteProcessingAsync();
    }
}
