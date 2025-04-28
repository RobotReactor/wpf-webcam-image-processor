using OxyPlot; // Required for PlotModel

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Defines the contract for a service that manages the
    /// histogram PlotModel and provides methods to update or clear its data.
    /// </summary>
    public interface IHistogramService
    {
        /// <summary>
        /// Gets the OxyPlot PlotModel instance managed by this service.
        /// ViewModels can bind to this property to display the chart.
        /// </summary>
        PlotModel HistogramPlotModel { get; }

        /// <summary>
        /// Updates the histogram chart with new data.
        /// </summary>
        /// <param name="histogramData">An array of 256 integers representing the pixel counts for each intensity level (0-255). If null or invalid length, the chart is cleared.</param>
        void UpdateHistogram(int[]? histogramData);

        /// <summary>
        /// Clears all data series from the histogram plot.
        /// </summary>
        void ClearHistogram();
    }
}
