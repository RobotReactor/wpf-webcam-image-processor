using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using Emgu.CV; 
using Emgu.CV.Structure; 
using WpfWebcamImageProcessor.App.Services; 
using WpfWebcamImageProcessor.App.Models;
using WpfWebcamImageProcessor.App.ViewModels;
using WpfWebcamImageProcessor.App.Exceptions; 
using System.Linq; 

namespace WpfWebcamImageProcessor.Tests
{
    /// <summary>
    /// Unit tests for the MainWindowViewModel.
    /// </summary>
    [TestClass]
    public class MainWindowViewModelTests
    {
        // Mocks for the service dependencies
        private Mock<IImageProcessingWorkflowService> _mockWorkflowService = null!;
        private Mock<IImageProcessingService> _mockImageProcessingService = null!;
        private Mock<IHistogramService> _mockHistogramService = null!;
        private Mock<ICameraService> _mockCameraService = null!;

        // Instance of the ViewModel we are testing
        private MainWindowViewModel _viewModel = null!;

        /// <summary>
        /// Sets up the ViewModel and mocks before each test.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Create fresh mocks for isolation in each test
            _mockWorkflowService = new Mock<IImageProcessingWorkflowService>();
            _mockImageProcessingService = new Mock<IImageProcessingService>();
            _mockHistogramService = new Mock<IHistogramService>();
            _mockCameraService = new Mock<ICameraService>();

            // Default behavior for CameraService mock
            _mockCameraService.SetupGet(c => c.IsStreaming).Returns(() => _viewModel.IsLiveViewActive);

            // Create the ViewModel with all the mocked services
            _viewModel = new MainWindowViewModel(
                _mockWorkflowService.Object,
                _mockImageProcessingService.Object,
                _mockHistogramService.Object,
                _mockCameraService.Object);
        }

        // Process Image Tests

        /// <summary>
        /// Verifies that ExecuteProcessImageAsync properly updates the ViewModel when the workflow succeeds.
        /// </summary>
        [TestMethod]
        public async Task ExecuteProcessImageAsync_WhenExecutedSuccessfully_UpdatesPropertiesAndCallsServices()
        {
            var fakeOriginalMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            var fakeGrayscaleMat = new Mat(10, 10, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            var fakeHistogramData = new int[256];
            var successfulResult = new ImageProcessingResult { Success = true, OriginalMat = fakeOriginalMat, GrayscaleMat = fakeGrayscaleMat, HistogramData = fakeHistogramData };

            _mockWorkflowService.Setup(w => w.ExecuteProcessingAsync()).ReturnsAsync(successfulResult);

            _mockImageProcessingService.Setup(p => p.ApplyGaussianBlur(It.IsAny<Mat>(), It.IsAny<int>()))
                                       .Returns(() => CreateDummyMat(10, 10, 1));

            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsTrue(_viewModel.ProcessImageCommand.CanExecute());

            await _viewModel.ExecuteProcessImageAsync();

            Assert.IsFalse(_viewModel.IsBusy, "ViewModel should not be busy after completion.");
            Assert.IsTrue(_viewModel.IsHistogramGenerated, "Histogram should be generated after successful processing.");
            Assert.IsNotNull(_viewModel.DisplayMat, "DisplayMat should be updated.");
            _mockWorkflowService.Verify(w => w.ExecuteProcessingAsync(), Times.Once());
            _mockHistogramService.Verify(h => h.UpdateHistogram(fakeHistogramData), Times.Once());
            _mockHistogramService.Verify(h => h.ClearHistogram(), Times.Once());
            _mockImageProcessingService.Verify(p => p.ApplyGaussianBlur(It.IsAny<Mat>(), It.IsAny<int>()), Times.Never());

            _viewModel.Dispose();
        }

        /// <summary>
        /// Verifies that ExecuteProcessImageAsync handles failure correctly.
        /// </summary>
        [TestMethod]
        public async Task ExecuteProcessImageAsync_WhenWorkflowFails_HandlesErrorAndResetsState()
        {
            var failedResult = new ImageProcessingResult { Success = false, ErrorMessage = "Test Workflow Failed" };
            _mockWorkflowService.Setup(w => w.ExecuteProcessingAsync()).ReturnsAsync(failedResult);

            await _viewModel.ExecuteProcessImageAsync();

            Assert.IsFalse(_viewModel.IsBusy, "ViewModel should not be busy after failure.");
            Assert.IsFalse(_viewModel.IsHistogramGenerated, "Histogram should not be generated on failure.");
            Assert.IsNull(_viewModel.DisplayMat, "DisplayMat should be cleared on failure.");
            _mockWorkflowService.Verify(w => w.ExecuteProcessingAsync(), Times.Once());
            _mockHistogramService.Verify(h => h.ClearHistogram(), Times.Exactly(2));
            _mockHistogramService.Verify(h => h.UpdateHistogram(It.IsAny<int[]>()), Times.Never());
        }

        // Start Live View Command Tests

        /// <summary>
        /// Tests that starting live view activates the camera and updates flags correctly.
        /// </summary>
        [TestMethod]
        public void StartLiveViewCommand_WhenCanExecute_CallsCameraServiceAndSetsFlag()
        {
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsFalse(_viewModel.IsLiveViewActive);
            Assert.IsTrue(_viewModel.StartLiveViewCommand.CanExecute());

            _viewModel.StartLiveViewCommand.Execute();

            _mockCameraService.Verify(c => c.StartCaptureStream(It.IsAny<Action<Mat>>()), Times.Once());
            Assert.IsTrue(_viewModel.IsLiveViewActive);
            Assert.IsFalse(_viewModel.StartLiveViewCommand.CanExecute());
            Assert.IsFalse(_viewModel.ProcessImageCommand.CanExecute());
            Assert.IsTrue(_viewModel.StopLiveViewCommand.CanExecute());
        }

        /// <summary>
        /// Tests that StartLiveViewCommand does not execute if live view is already active.
        /// </summary>
        [TestMethod]
        public void StartLiveViewCommand_WhenAlreadyActive_CannotExecute()
        {
            // Arrange
            _viewModel.StartLiveViewCommand.Execute();
            Assert.IsTrue(_viewModel.IsLiveViewActive);
            _mockCameraService.Invocations.Clear();

            // Act
            _viewModel.StartLiveViewCommand.Execute();

            // Assert
            _mockCameraService.Verify(c => c.StartCaptureStream(It.IsAny<Action<Mat>>()), Times.Never());
            Assert.IsTrue(_viewModel.IsLiveViewActive);
        }

        // Stop Live View Command Tests

        /// <summary>
        /// Tests that stopping live view stops the camera and resets the flag.
        /// </summary>
        [TestMethod]
        public void StopLiveViewCommand_WhenCanExecute_CallsCameraServiceAndResetsFlag()
        {
            _viewModel.StartLiveViewCommand.Execute();
            Assert.IsTrue(_viewModel.IsLiveViewActive);
            Assert.IsTrue(_viewModel.StopLiveViewCommand.CanExecute());

            _viewModel.StopLiveViewCommand.Execute();

            _mockCameraService.Verify(c => c.StopCaptureStream(), Times.Once());
            Assert.IsFalse(_viewModel.IsLiveViewActive);
            Assert.IsTrue(_viewModel.StartLiveViewCommand.CanExecute());
            Assert.IsTrue(_viewModel.ProcessImageCommand.CanExecute());
            Assert.IsFalse(_viewModel.StopLiveViewCommand.CanExecute());
        }

        /// <summary>
        /// Tests that StopLiveViewCommand does nothing if not already active.
        /// </summary>
        [TestMethod]
        public void StopLiveViewCommand_WhenNotActive_CannotExecute()
        {
            // Arrange
            Assert.IsFalse(_viewModel.IsLiveViewActive);
            Assert.IsFalse(_viewModel.StopLiveViewCommand.CanExecute());

            // Act
            _viewModel.StopLiveViewCommand.Execute();

            // Assert
            _mockCameraService.Verify(c => c.StopCaptureStream(), Times.Never());
            Assert.IsFalse(_viewModel.IsLiveViewActive);
        }

        // Selected Filter Index Tests

        /// <summary>
        /// Tests that changing the SelectedFilterIndex applies the correct filter when images are loaded.
        /// </summary>
        [TestMethod]
        public void SelectedFilterIndex_WhenChangedWithImagesLoaded_CallsApplyFilter()
        {
            var originalMat = CreateDummyMat(5, 5, 1);
            var grayscaleMat = CreateDummyMat(5, 5, 2);
            SetInternalStateForFilterTest(originalMat, grayscaleMat);

            ActiveFilter targetFilter = ActiveFilter.Blur;
            int targetIndex = (int)targetFilter;
            Mat? mockBlurResult = CreateDummyMat(5, 5, 3);

            _mockImageProcessingService.Setup(p => p.ApplyGaussianBlur(grayscaleMat, It.IsAny<int>()))
                                       .Returns(mockBlurResult)
                                       .Verifiable();

            _viewModel.SelectedFilterIndex = targetIndex;

            _mockImageProcessingService.Verify();
            Assert.IsNotNull(_viewModel.DisplayMat);

            _viewModel.Dispose();
        }

        /// <summary>
        /// Tests that changing SelectedFilterIndex does nothing if no images are loaded.
        /// </summary>
        [TestMethod]
        public void SelectedFilterIndex_WhenChangedWithoutImages_DoesNotCallApplyFilter()
        {
            Assert.IsNull(_viewModel.DisplayMat);

            _viewModel.SelectedFilterIndex = (int)ActiveFilter.Blur;

            _mockImageProcessingService.Verify(p => p.ApplyGaussianBlur(It.IsAny<Mat>(), It.IsAny<int>()), Times.Never());
            Assert.IsNull(_viewModel.DisplayMat);
        }

        // Helper Methods

        /// <summary>
        /// Creates a dummy Mat for testing purposes.
        /// </summary>
        private Mat CreateDummyMat(int rows = 10, int cols = 10, byte value = 0)
        {
            var mat = new Mat(rows, cols, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(value));
            return mat;
        }

        /// <summary>
        /// Sets up the ViewModel's internal state to simulate loaded images for filter tests.
        /// </summary>
        private void SetInternalStateForFilterTest(Mat original, Mat grayscale)
        {
            // Clear any existing state
            _viewModel.GetType().GetMethod("ClearImageData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                     ?.Invoke(_viewModel, null);

            // Set private fields via reflection
            var originalField = _viewModel.GetType().GetField("_currentOriginalMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            originalField?.SetValue(_viewModel, original);

            var grayscaleField = _viewModel.GetType().GetField("_currentGrayscaleMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            grayscaleField?.SetValue(_viewModel, grayscale);

            // Apply the default filter
            _viewModel.GetType().GetMethod("ApplySelectedFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                     ?.Invoke(_viewModel, null);

            // Clear mock invocation history
            _mockImageProcessingService.Invocations.Clear();
        }
    }
}
