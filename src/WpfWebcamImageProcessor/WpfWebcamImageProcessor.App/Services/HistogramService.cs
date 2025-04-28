using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Implements the IHistogramService. Manages an OxyPlot PlotModel
    /// for displaying a grayscale histogram and handles data updates.
    /// </summary>
    public class HistogramService : IHistogramService
    {
        // The PlotModel instance that holds the chart configuration and data.
        // This is exposed via the interface property.
        public PlotModel HistogramPlotModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistogramService"/> class.
        /// Creates and configures the PlotModel upon instantiation.
        /// </summary>
        public HistogramService()
        {
            // Create and configure the PlotModel
            HistogramPlotModel = new PlotModel { Title = "Grayscale Histogram" };

            // Configure the X-axis (Intensity)
            HistogramPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Intensity",
                Minimum = -0.5,
                Maximum = 255.5,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None
            });

            // Configure the Y-axis (Count)
            HistogramPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Count",
                Minimum = 0,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0 // Maximize bar height relative to axis
            });

        }

        /// <summary>
        /// Updates the histogram chart with new data. Clears any existing series
        /// and adds a new RectangleBarSeries based on the provided counts.
        /// </summary>
        /// <param name="histogramData">An array of 256 integers representing the pixel counts for each intensity level (0-255). If null or invalid length, the chart is cleared.</param>
        public void UpdateHistogram(int[]? histogramData)
        {
            // Ensure updates happen on the UI thread if necessary, although PlotModel
            // updates might be thread-safe for data; invalidation should be safe.
            // Consider Dispatcher if issues arise.

            // Always clear previous data series.
            HistogramPlotModel.Series.Clear();

            // Only add a new series if valid histogram data is provided.
            if (histogramData != null && histogramData.Length == 256)
            {
                var rectBarSeries = new RectangleBarSeries
                {
                    Title = "Count", // Used for legends, if enabled
                    StrokeThickness = 1, // Add a thin border to bars
                    FillColor = OxyColors.SteelBlue // Set the bar color
                };

                for (int i = 0; i < histogramData.Length; i++)
                {
                    double x0 = i - 0.5; double x1 = i + 0.5;
                    double y0 = 0; double y1 = histogramData[i];
                    rectBarSeries.Items.Add(new RectangleBarItem(x0, y0, x1, y1));
                }
                HistogramPlotModel.Series.Add(rectBarSeries);
            }

            // Refresh the plot view to display the changes.
            HistogramPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Clears all data series from the histogram plot, effectively resetting it.
        /// </summary>
        public void ClearHistogram()
        {
            HistogramPlotModel.Series.Clear();
            // Refresh the plot view to show the cleared state.
            HistogramPlotModel.InvalidatePlot(true);
        }
    }
}
