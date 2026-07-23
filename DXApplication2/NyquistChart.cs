using DevExpress.Utils;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
namespace DXApplication21
{
    public partial class NyquistChart : XtraForm
    {
        private Series nyquistSeries;
        private Series cpSeries;
        private Series rpSeries;

        public NyquistChart()
        {
            InitializeComponent();
        }

        private void ResetChart()
        {
            chartControl1.Series.Clear();
            chartControl1.Titles.Clear();

            XYDiagram diagram =
                chartControl1.Diagram as XYDiagram;

            if (diagram != null)
            {
                // Remove the secondary Rp axis when changing plots.
                diagram.SecondaryAxesY.Clear();

                // Restore automatic ranges.
                diagram.AxisX.WholeRange.Auto = true;
                diagram.AxisX.VisualRange.Auto = true;

                diagram.AxisY.WholeRange.Auto = true;
                diagram.AxisY.VisualRange.Auto = true;

                // Restore linear axes.
                diagram.AxisX.Logarithmic = false;
                diagram.AxisY.Logarithmic = false;
            }

            nyquistSeries = null;
            cpSeries = null;
            rpSeries = null;
        }

        public void ChartConfigNyquist(
            IList<double> zline,
            IList<double> z2line)
        {
            if (zline == null || z2line == null)
            {
                return;
            }

            if (zline.Count == 0 ||
                zline.Count != z2line.Count)
            {
                XtraMessageBox.Show(
                    "Z' and -Z'' lists must have the same size.",
                    "Chart error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            ResetChart();

            nyquistSeries = new Series(
                "Nyquist",
                ViewType.ScatterLine
            );

            nyquistSeries.ArgumentScaleType =
                ScaleType.Numerical;

            nyquistSeries.ValueScaleType =
                ScaleType.Numerical;

            chartControl1.Series.Add(nyquistSeries);

            XYDiagram diagram =
                chartControl1.Diagram as XYDiagram;

            if (diagram == null)
            {
                XtraMessageBox.Show(
                    "The chart diagram could not be initialized.",
                    "Chart error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            // X axis: real impedance.
            diagram.AxisX.Title.Visibility =
                DefaultBoolean.True;

            diagram.AxisX.Title.Text = "Z' (Ω)";
            diagram.AxisX.Logarithmic = false;
            diagram.AxisX.GridLines.Visible = true;
            diagram.AxisX.WholeRange.Auto = true;
            diagram.AxisX.VisualRange.Auto = true;

            // Y axis: negative imaginary impedance.
            diagram.AxisY.Title.Visibility =
                DefaultBoolean.True;

            diagram.AxisY.Title.Text = "-Z'' (Ω)";
            diagram.AxisY.Logarithmic = false;
            diagram.AxisY.GridLines.Visible = true;
            diagram.AxisY.WholeRange.Auto = true;
            diagram.AxisY.VisualRange.Auto = true;

            // Add Nyquist points.
            for (int i = 0; i < zline.Count; i++)
            {
                double zReal = zline[i];
                double zImaginary = z2line[i];

                if (double.IsNaN(zReal) ||
                    double.IsInfinity(zReal) ||
                    double.IsNaN(zImaginary) ||
                    double.IsInfinity(zImaginary))
                {
                    continue;
                }

                nyquistSeries.Points.Add(
                    new SeriesPoint(
                        zReal,
                        zImaginary
                    )
                );
            }

            // Zoom and scrolling.
            diagram.EnableAxisXZooming = true;
            diagram.EnableAxisYZooming = true;
            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisYScrolling = true;

            ScatterLineSeriesView seriesView =
                nyquistSeries.View as ScatterLineSeriesView;

            if (seriesView != null)
            {
                seriesView.MarkerVisibility =
                    DefaultBoolean.True;

                seriesView.LineMarkerOptions.Kind =
                    MarkerKind.Circle;

                seriesView.LineMarkerOptions.Size = 7;
            }

            nyquistSeries.LabelsVisibility =
                DefaultBoolean.False;

            chartControl1.ToolTipEnabled =
                DefaultBoolean.True;

            chartControl1.Legend.Visibility =
                DefaultBoolean.True;

            ChartTitle chartTitle = new ChartTitle();
            chartTitle.Text = "Nyquist Plot";

            chartControl1.Titles.Add(chartTitle);

            Text = "Nyquist Plot";
        }

        public void ChartConfigCpRp(
            IList<double> frequencies,
            IList<double> cpValues,
            IList<double> rpValues)
        {
            // Check the received lists.
            if (frequencies == null ||
                cpValues == null ||
                rpValues == null)
            {
                return;
            }

            if (frequencies.Count == 0)
            {
                return;
            }

            if (frequencies.Count != cpValues.Count ||
                frequencies.Count != rpValues.Count)
            {
                XtraMessageBox.Show(
                    "Frequency, Cp, and Rp lists must have the same size.",
                    "Chart Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            ResetChart();

            // Create the series.
            cpSeries = new Series(
                "Cp",
                ViewType.ScatterLine);

            rpSeries = new Series(
                "Rp",
                ViewType.ScatterLine);

            cpSeries.ArgumentScaleType = ScaleType.Numerical;
            cpSeries.ValueScaleType = ScaleType.Numerical;

            rpSeries.ArgumentScaleType = ScaleType.Numerical;
            rpSeries.ValueScaleType = ScaleType.Numerical;

            chartControl1.Series.Add(cpSeries);
            chartControl1.Series.Add(rpSeries);

            XYDiagram diagram =
                chartControl1.Diagram as XYDiagram;

            if (diagram == null)
            {
                XtraMessageBox.Show(
                    "The chart diagram could not be initialized.",
                    "Chart Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // X axis: frequency.
            diagram.AxisX.Title.Visibility =
                DefaultBoolean.True;

            diagram.AxisX.Title.Text =
                "Frequency (Hz)";

            diagram.AxisX.Logarithmic = true;
            diagram.AxisX.LogarithmicBase = 10;
            diagram.AxisX.GridLines.Visible = true;
            diagram.AxisX.WholeRange.Auto = true;
            diagram.AxisX.VisualRange.Auto = true;

            // Primary Y axis: Cp.
            diagram.AxisY.Title.Visibility =
                DefaultBoolean.True;

            diagram.AxisY.Title.Text =
                "Cp (pF)";

            diagram.AxisY.Logarithmic = false;
            diagram.AxisY.GridLines.Visible = true;
            diagram.AxisY.WholeRange.Auto = true;
            diagram.AxisY.VisualRange.Auto = true;

            // Secondary Y axis: Rp.
            SecondaryAxisY rpAxis =
                new SecondaryAxisY("RpAxis");

            rpAxis.Title.Visibility =
                DefaultBoolean.True;

            rpAxis.Title.Text =
                "Rp (MΩ)";

            rpAxis.Alignment = AxisAlignment.Far;
            rpAxis.GridLines.Visible = false;
            rpAxis.WholeRange.Auto = true;
            rpAxis.VisualRange.Auto = true;

            diagram.SecondaryAxesY.Add(rpAxis);

            XYDiagramSeriesViewBase rpView =
                rpSeries.View as XYDiagramSeriesViewBase;

            if (rpView != null)
            {
                rpView.AxisY = rpAxis;
            }

            ConfigureScatterSeries(cpSeries);
            ConfigureScatterSeries(rpSeries);

            cpSeries.LabelsVisibility =
                DefaultBoolean.False;

            rpSeries.LabelsVisibility =
                DefaultBoolean.False;

            // Add the mean Cp and Rp values from the CSV.
            for (int i = 0; i < frequencies.Count; i++)
            {
                double frequency = frequencies[i];
                double cp = cpValues[i];
                double rp = rpValues[i];

                if (frequency <= 0 ||
                    double.IsNaN(frequency) ||
                    double.IsInfinity(frequency) ||
                    double.IsNaN(cp) ||
                    double.IsInfinity(cp) ||
                    double.IsNaN(rp) ||
                    double.IsInfinity(rp))
                {
                    continue;
                }

                cpSeries.Points.Add(
                    new SeriesPoint(
                        frequency,
                        cp));

                rpSeries.Points.Add(
                    new SeriesPoint(
                        frequency,
                        rp));
            }

            // Chart interaction.
            diagram.EnableAxisXZooming = true;
            diagram.EnableAxisYZooming = true;
            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisYScrolling = true;

            chartControl1.ToolTipEnabled =
                DefaultBoolean.True;

            chartControl1.Legend.Visibility =
                DefaultBoolean.True;

            // Chart title.
            ChartTitle chartTitle =
                new ChartTitle();

            chartTitle.Text =
                "Cp and Rp over Frequency";

            chartControl1.Titles.Add(chartTitle);

            Text = "Cp and Rp over Frequency";
        }

        public void ChartConfigNyquistMulti(
            IList<string> modes,
            IList<List<double>> zRealMulti,
            IList<List<double>> zImagMulti)
        {

            // Check the received lists.
            if (modes == null ||
                zRealMulti == null ||
                zImagMulti == null)
            {
                return;
            }

            chartControl1.Series.Clear();

            for (int modeIndex = 0; modeIndex < modes.Count; modeIndex++)
            {
                Series nyquistSeries = new Series(
                    modes[modeIndex],
                    ViewType.ScatterLine);

                ScatterLineSeriesView seriesView =
                    nyquistSeries.View as ScatterLineSeriesView;

                if (seriesView != null)
                {
                    seriesView.MarkerVisibility =
                        DevExpress.Utils.DefaultBoolean.True;
                }

                for (int pointIndex = 0;
                     pointIndex < zRealMulti[modeIndex].Count;
                     pointIndex++)
                {
                    nyquistSeries.Points.Add(
                        new SeriesPoint(
                            zRealMulti[modeIndex][pointIndex],
                            zImagMulti[modeIndex][pointIndex]));
                }

                chartControl1.Series.Add(nyquistSeries);
            }

            // The diagram exists after adding the series
            XYDiagram diagram =
                chartControl1.Diagram as XYDiagram;

            if (diagram == null)
            {
                return;
            }

            diagram.AxisX.Title.Visibility =
                DevExpress.Utils.DefaultBoolean.True;

            diagram.AxisX.Title.Text =
                "Z' (Ω)";

            diagram.AxisY.Title.Visibility =
                DevExpress.Utils.DefaultBoolean.True;

            diagram.AxisY.Title.Text =
                "-Z'' (Ω)";

            chartControl1.Legend.Visibility =
                DevExpress.Utils.DefaultBoolean.True;
        }

        private void ConfigureScatterSeries(Series series)
        {
            ScatterLineSeriesView seriesView =
                series.View as ScatterLineSeriesView;

            if (seriesView == null)
            {
                return;
            }

            seriesView.MarkerVisibility =
                DefaultBoolean.True;

            seriesView.LineMarkerOptions.Kind =
                MarkerKind.Circle;

            seriesView.LineMarkerOptions.Size = 7;
        }
    }
}