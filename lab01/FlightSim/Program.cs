using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Linq;

namespace FlightSim
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SimulationForm());
        }
    }

    public class SimulationForm : Form
    {
        private Chart chart1;
        private NumericUpDown edHeight, edAngle, edSpeed, edSize, edWeight, edStep;
        private Button btLaunch, btTable, btClear;

        private System.Windows.Forms.Timer animationTimer;
        private List<PointD>? pointsToAnimate;
        private int currentPointIndex;
        private Series? trailSeries;
        private Series? ballSeries;

        const double g = 9.81;
        const double C = 0.15;
        const double rho = 1.29;

        public SimulationForm()
        {
            this.Text = "Моделирование полета в атмосфере";
            this.Size = new Size(1150, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel chartPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            this.Controls.Add(chartPanel);

            chart1 = new Chart { Dock = DockStyle.Fill };
            chartPanel.Controls.Add(chart1);

            FlowLayoutPanel flowPanel = new FlowLayoutPanel { 
                Dock = DockStyle.Top, 
                Height = 85, 
                Padding = new Padding(10), 
                BackColor = SystemColors.Control 
            };
            this.Controls.Add(flowPanel);

            void AddParam(string txt, out NumericUpDown nud, decimal val)
            {
                Label l = new Label { Text = txt, AutoSize = true, Margin = new Padding(0, 8, 2, 0) };
                nud = new NumericUpDown { Value = val, DecimalPlaces = 2, Width = 65, Margin = new Padding(0, 5, 12, 0), Maximum = 10000 };
                flowPanel.Controls.AddRange(new Control[] { l, nud });
            }

            AddParam("Высота (м):", out edHeight, 0);
            AddParam("Угол (град.):", out edAngle, 45);
            AddParam("Скорость (м/с):", out edSpeed, 15);
            AddParam("Размер (м^2):", out edSize, 0.10m);
            AddParam("Масса (кг):", out edWeight, 1);
            
            Label lStep = new Label { Text = "Шаг dt (с):", AutoSize = true, Margin = new Padding(0, 8, 2, 0) };
            edStep = new NumericUpDown { Value = 0.05m, DecimalPlaces = 4, Width = 75, Increment = 0.01m, Minimum = 0.0001m };
            flowPanel.Controls.AddRange(new Control[] { lStep, edStep });

            Size btnSize = new Size(130, 30);
            btLaunch = new Button { Text = "Запуск", Size = btnSize, Margin = new Padding(10, 3, 5, 0) };
            btLaunch.Click += BtLaunch_Click;
            
            btTable = new Button { Text = "Таблица", Size = btnSize, Margin = new Padding(5, 3, 5, 0) };
            btTable.Click += BtTable_Click;

            btClear = new Button { Text = "Очистить", Size = btnSize, Margin = new Padding(5, 3, 0, 0) };
            btClear.Click += (s, e) => { StopAnimation(); chart1.Series.Clear(); ResetAxes(); };

            flowPanel.Controls.AddRange(new Control[] { btLaunch, btTable, btClear });

            ChartArea area = new ChartArea("MainArea");
            area.AxisX.Title = "Расстояние (м)"; 
            area.AxisY.Title = "Высота (м)";
            area.AxisX.MajorGrid.LineColor = Color.LightGray; 
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisX.Minimum = 0; 
            area.AxisY.Minimum = 0;

            chart1.ChartAreas.Add(area);
            chart1.Legends.Add(new Legend { Docking = Docking.Bottom });
            this.Controls.SetChildIndex(flowPanel, 0);

            animationTimer = new System.Windows.Forms.Timer { Interval = 20 }; 
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void SetCustomScale(double maxX, double maxY)
        {
            var area = chart1.ChartAreas[0];
            double newMaxX = maxX * 1.15; 
            double newMaxY = maxY * 1.25; 

            if (!double.IsNaN(area.AxisX.Maximum)) newMaxX = Math.Max(newMaxX, area.AxisX.Maximum);
            if (!double.IsNaN(area.AxisY.Maximum)) newMaxY = Math.Max(newMaxY, area.AxisY.Maximum);

            area.AxisX.Maximum = Math.Ceiling(newMaxX);
            area.AxisY.Maximum = Math.Ceiling(newMaxY);
        }

        private void ResetAxes()
        {
            chart1.ChartAreas[0].AxisX.Maximum = double.NaN;
            chart1.ChartAreas[0].AxisY.Maximum = double.NaN;
        }

        private void BtLaunch_Click(object? sender, EventArgs e)
        {
            StopAnimation();
            double dt = (double)edStep.Value;
            var res = Calculate(dt);

            SetCustomScale(res.dist, res.maxH);

            string name = $"dt={dt}";
            if (chart1.Series.FindByName(name) != null) chart1.Series.Remove(chart1.Series.FindByName(name));

            trailSeries = new Series(name) { ChartType = SeriesChartType.Line, BorderWidth = 2 };
            chart1.Series.Add(trailSeries);

            ballSeries = new Series("Ball") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Circle, MarkerSize = 10, Color = Color.Red };
            ballSeries.IsVisibleInLegend = false;
            chart1.Series.Add(ballSeries);

            pointsToAnimate = res.path;
            currentPointIndex = 0;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (pointsToAnimate == null || currentPointIndex >= pointsToAnimate.Count || trailSeries == null || ballSeries == null)
            {
                StopAnimation();
                return;
            }
            var p = pointsToAnimate[currentPointIndex];
            trailSeries.Points.AddXY(p.X, p.Y);
            ballSeries.Points.Clear();
            ballSeries.Points.AddXY(p.X, p.Y);

            int skip = Math.Max(1, pointsToAnimate.Count / 100); 
            currentPointIndex += skip;
        }

        private void StopAnimation()
        {
            animationTimer.Stop();
            if (ballSeries != null && chart1.Series.Contains(ballSeries)) 
                chart1.Series.Remove(ballSeries);
        }

        private void BtTable_Click(object? sender, EventArgs e)
        {
            StopAnimation();
            chart1.Series.Clear();
            ResetAxes();

            double[] steps = { 1.0, 0.1, 0.01, 0.001, 0.0001 };
            List<SimulationResult> allResults = new List<SimulationResult>();
            foreach (var dt in steps) allResults.Add(Calculate(dt));

            SetCustomScale(allResults.Max(r => r.dist), allResults.Max(r => r.maxH));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Шаг (dt), с | Дальность, м | Макс. высота, м | Скорость в конце, м/с");
            sb.AppendLine("-----------------------------------------------------------------------");

            for (int i = 0; i < steps.Length; i++)
            {
                var res = allResults[i];
                Series s = new Series($"dt={steps[i]}") { ChartType = SeriesChartType.Line, BorderWidth = 2 };
                foreach (var p in res.path) s.Points.AddXY(p.X, p.Y);
                chart1.Series.Add(s);
                sb.AppendLine($"{steps[i],-11} | {res.dist,-12:F4} | {res.maxH,-15:F4} | {res.endV:F4}");
            }
            MessageBox.Show(sb.ToString(), "Результаты моделирования");
        }

        private SimulationResult Calculate(double dt)
        {
            double x = 0, y = (double)edHeight.Value;
            double vx = (double)edSpeed.Value * Math.Cos((double)edAngle.Value * Math.PI / 180);
            double vy = (double)edSpeed.Value * Math.Sin((double)edAngle.Value * Math.PI / 180);
            double k = 0.5 * C * rho * (double)edSize.Value / (double)edWeight.Value;
            double maxH = y;

            List<PointD> path = new List<PointD> { new PointD(x, y) };

            while (y >= 0)
            {
                double v = Math.Sqrt(vx * vx + vy * vy);
                vx = vx - k * vx * v * dt;
                vy = vy - (g + k * vy * v) * dt;
                x = x + vx * dt;
                y = y + vy * dt;
                if (y >= 0) path.Add(new PointD(x, y));
                if (y > maxH) maxH = y;
                if (x > 10000) break; 
            }
            return new SimulationResult(x, maxH, Math.Sqrt(vx * vx + vy * vy), path);
        }
    }

    public struct PointD { 
        public double X, Y; 
        public PointD(double x, double y) { X = x; Y = y; } 
    }

    public record SimulationResult(double dist, double maxH, double endV, List<PointD> path);
}