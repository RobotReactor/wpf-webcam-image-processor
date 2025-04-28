using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace WpfWebcamImageProcessor.App.ViewModels
{
    public class HistogramViewModel : BindableBase
    {
        private PlotModel _histogramPlotModel;
        public PlotModel HistogramPlotModel
        {
            get => _histogramPlotModel;
            private set => SetProperty(ref _histogramPlotModel, value);
        }

        public HistogramViewModel()
        {
            var tempPlotModel = new PlotModel { Title = "Grayscale Histogram" };

            tempPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Intensity",
                Minimum = -0.5,
                Maximum = 255.5,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None
            });
            tempPlotModel.Axes.Add(new LinearAxis 
            {
                Position = AxisPosition.Left,
                Title = "Count",
                Minimum = 0,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0
            });
            HistogramPlotModel = tempPlotModel;
        }

        public void UpdateHistogram(int[]? histogramData)
        {
            HistogramPlotModel.Series.Clear();
            if (histogramData != null)
            {
                var rectBarSeries = new RectangleBarSeries
                {
                    Title = "Count",
                    StrokeThickness = 1,
                    FillColor = OxyColors.SteelBlue 
                };
                for (int i = 0; i < histogramData.Length; i++)
                {
                    double x0 = i - 0.5; double x1 = i + 0.5;
                    double y0 = 0; double y1 = histogramData[i];
                    rectBarSeries.Items.Add(new RectangleBarItem(x0, y0, x1, y1));
                }
                HistogramPlotModel.Series.Add(rectBarSeries);
            }
            HistogramPlotModel.InvalidatePlot(true);
        }

        public void ClearHistogram()
        {
            HistogramPlotModel.Series.Clear();
            HistogramPlotModel.InvalidatePlot(true);
        }
    }
}
