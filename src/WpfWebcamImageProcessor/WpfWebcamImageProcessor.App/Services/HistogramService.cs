using OxyPlot;
using OxyPlot.Axes; 
using OxyPlot.Series; 
using System;

namespace WpfWebcamImageProcessor.App.Services
{
    /// <summary>
    /// Implements the IHistogramService. This service is responsible for managing
    /// an OxyPlot PlotModel used to display a grayscale histogram. It handles
    /// the creation of the chart structure (axes, title) and updates the chart data.
    /// </summary>
    public class HistogramService : IHistogramService
    {
        /// <summary>
        /// Gets the OxyPlot PlotModel instance managed by this service.
        /// This model contains all the configuration and data for the histogram chart.
        /// ViewModels bind to this property (via the IHistogramService interface)
        /// to display the chart in a PlotView control.
        /// </summary>
        public PlotModel HistogramPlotModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HistogramService class.
        /// Creates the PlotModel and configures its axes upon creation.
        /// </summary>
        public HistogramService()
        {
            // Initialize the PlotModel with a title.
            HistogramPlotModel = new PlotModel { Title = "Grayscale Histogram" };

            // Configure the horizontal (bottom) axis to represent pixel intensity.
            HistogramPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Intensity",
                // Set the range slightly outside 0-255 to ensure bars at the edges are fully visible.
                Minimum = -0.5,
                Maximum = 255.5,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None
            });

            // Configure the vertical (left) axis to represent the count of pixels.
            HistogramPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Count",
                // Ensure the axis starts at zero.
                Minimum = 0,
                // Style the grid lines similarly to the X-axis.
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                // Remove extra padding at the top of the axis. This makes the tallest
                // bar reach the top, maximizing the use of the chart area.
                MaximumPadding = 0
            });

            // Additional appearance settings could be applied here if desired,
            // for example, background color, text colors, etc.
            // HistogramPlotModel.PlotAreaBorderColor = OxyColors.DarkGray;
            // HistogramPlotModel.Background = OxyColors.WhiteSmoke;
        }

        /// <summary>
        /// Updates the histogram chart data. It first clears any existing data series
        /// and then creates and adds a new RectangleBarSeries based on the provided counts.
        /// </summary>
        /// <param name="histogramData">
        /// An array containing 256 integer values, where each index corresponds to a grayscale
        /// intensity level (0-255) and the value at that index is the number of pixels
        /// with that intensity. If the data is null or not the expected length, the chart will be cleared.
        /// </param>
        public void UpdateHistogram(int[]? histogramData)
        {
            // It's generally safe to update OxyPlot models directly, but if threading issues
            // were encountered, updates might need marshalling to the UI thread via Dispatcher.

            // Remove any previously displayed histogram bars.
            HistogramPlotModel.Series.Clear();

            // Proceed only if valid histogram data (an array of 256 integers) is provided.
            if (histogramData != null && histogramData.Length == 256)
            {
                // Use RectangleBarSeries for histograms with linear axes. Each bar represents one intensity bin.
                var rectBarSeries = new RectangleBarSeries
                {
                    Title = "Count", // Title used in legends, tooltips etc.
                    StrokeThickness = 1, // Draw a thin border around each bar.
                    FillColor = OxyColors.SteelBlue // Set the fill color of the bars.
                };

                // Create a rectangular bar item for each intensity level (0-255).
                for (int i = 0; i < histogramData.Length; i++)
                {
                    // Define the coordinates for the rectangle representing the bar.
                    // x0, x1 define the width along the Intensity axis, centered around 'i'.
                    // y0, y1 define the height along the Count axis.
                    double x0 = i - 0.5; // Left edge
                    double x1 = i + 0.5; // Right edge
                    double y0 = 0;       // Bottom edge (base)
                    double y1 = histogramData[i]; // Top edge (pixel count)

                    // Add the defined rectangle to the series.
                    rectBarSeries.Items.Add(new RectangleBarItem(x0, y0, x1, y1));
                }
                // Add the newly created and populated series to the plot model.
                HistogramPlotModel.Series.Add(rectBarSeries);
            }
            // If histogramData was null or invalid, the series collection remains empty.

            // Notify the PlotView control that the model has changed and needs to be redrawn.
            // The 'true' argument indicates that the data ranges (and thus axis scaling) might have changed.
            HistogramPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Clears all data series from the histogram plot model, effectively resetting the chart view.
        /// </summary>
        public void ClearHistogram()
        {
            // Remove all series currently associated with the plot model.
            HistogramPlotModel.Series.Clear();
            // Refresh the associated PlotView control to reflect the cleared state.
            HistogramPlotModel.InvalidatePlot(true);
        }
    }
}
