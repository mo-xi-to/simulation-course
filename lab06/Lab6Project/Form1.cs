using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab6Project
{
    public class MultiplicativeGenerator
    {
        private ulong _state;
        private const ulong M = 9223372036854775808UL;
        private const ulong Beta = 4294967299UL;

        public MultiplicativeGenerator(ulong seed)
        {
            _state = seed;
            
            if (_state % 2 == 0) 
            {
                _state += 1; 
            }
        }

        public double NextDouble()
        {
            _state = (Beta * _state) % M;
            return (double)_state / M;
        }
    }
    public partial class Form1 : Form
    {
        private DataGridView dInputGrid, dResultGrid;
        private Chart dChart;
        private TextBox nMuInput, nVarInput;
        private DataGridView nResultGrid;
        private Chart nChart;

        public Form1()
        {
            this.Text = "Лабораторная работа №6";
            this.Size = new Size(1100, 850);
            InitializeApp();
        }

        private void InitializeApp()
        {
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };

            TabPage tab1 = new TabPage("ЛР 6-1: Дискретная СВ");
            SetupDiscreteTab(tab1);

            TabPage tab2 = new TabPage("ЛР 6-2: Нормальная СВ");
            SetupNormalTab(tab2);

            tabs.TabPages.Add(tab1);
            tabs.TabPages.Add(tab2);
            this.Controls.Add(tabs);
        }

        private void SetupDiscreteTab(TabPage page)
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 180 };
            dInputGrid = new DataGridView { Bounds = new Rectangle(10, 10, 300, 130), ColumnCount = 2, AllowUserToAddRows = false };
            dInputGrid.Columns[0].Name = "X"; dInputGrid.Columns[1].Name = "P";
            dInputGrid.Rows.Add("1", "0.1"); dInputGrid.Rows.Add("2", "0.2");
            dInputGrid.Rows.Add("3", "0.4"); dInputGrid.Rows.Add("4", "0.2");
            dInputGrid.Rows.Add("5", "0.1");

            Button btn = new Button { Text = "РАССЧИТАТЬ", Bounds = new Rectangle(320, 10, 120, 40), BackColor = Color.LightBlue };
            btn.Click += (s, e) => RunDiscrete();

            pnl.Controls.Add(dInputGrid); pnl.Controls.Add(btn);

            dResultGrid = CreateResultGrid();
            dResultGrid.Dock = DockStyle.Fill;

            dChart = new Chart { Dock = DockStyle.Bottom, Height = 300 };
            dChart.ChartAreas.Add(new ChartArea());
            dChart.Series.Add(new Series("Эмпирическая") { ChartType = SeriesChartType.Column });

            page.Controls.Add(dResultGrid); page.Controls.Add(pnl); page.Controls.Add(dChart);
        }

        private void SetupNormalTab(TabPage page)
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 100 };
            pnl.Controls.Add(new Label { Text = "Mean (Mu):", Location = new Point(10, 20), AutoSize = true });
            nMuInput = new TextBox { Text = "0", Location = new Point(80, 17), Width = 50 };
            pnl.Controls.Add(new Label { Text = "Var (Sigma^2):", Location = new Point(150, 20), AutoSize = true });
            nVarInput = new TextBox { Text = "1", Location = new Point(230, 17), Width = 50 };

            Button btn = new Button { Text = "МОДЕЛИРОВАТЬ", Bounds = new Rectangle(300, 10, 120, 40), BackColor = Color.LightGreen };
            btn.Click += (s, e) => RunNormal();

            pnl.Controls.Add(nMuInput); pnl.Controls.Add(nVarInput); pnl.Controls.Add(btn);

            nResultGrid = CreateResultGrid();
            nResultGrid.Dock = DockStyle.Fill;

            nChart = new Chart { Dock = DockStyle.Bottom, Height = 350 };
            nChart.ChartAreas.Add(new ChartArea());
            nChart.Series.Add(new Series("Гистограмма") { ChartType = SeriesChartType.Column });
            nChart.Series.Add(new Series("Теория") { ChartType = SeriesChartType.Spline, BorderWidth = 3, Color = Color.Red });

            page.Controls.Add(nResultGrid); page.Controls.Add(pnl); page.Controls.Add(nChart);
        }

        private DataGridView CreateResultGrid()
        {
            DataGridView dg = new DataGridView { ColumnCount = 7, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dg.Columns[0].Name = "N"; dg.Columns[1].Name = "M (эмп)"; dg.Columns[2].Name = "M ош %";
            dg.Columns[3].Name = "D (эмп)"; dg.Columns[4].Name = "D ош %"; dg.Columns[5].Name = "Хи-квадрат";
            dg.Columns[6].Name = "Результат";
            return dg;
        }

        private void RunDiscrete()
        {
            try {
                var xVals = new List<double>(); var pVals = new List<double>();
                foreach (DataGridViewRow r in dInputGrid.Rows) {
                    xVals.Add(double.Parse(r.Cells[0].Value.ToString().Replace(',', '.'), CultureInfo.InvariantCulture));
                    pVals.Add(double.Parse(r.Cells[1].Value.ToString().Replace(',', '.'), CultureInfo.InvariantCulture));
                }
                if (Math.Abs(pVals.Sum() - 1.0) > 0.01) { MessageBox.Show("Сумма P != 1.0"); return; }

                double mT = 0; for (int i = 0; i < xVals.Count; i++) mT += xVals[i] * pVals[i];
                double dT = xVals.Zip(pVals, (x, p) => x * x * p).Sum() - mT * mT;

                dResultGrid.Rows.Clear();
                MultiplicativeGenerator rnd = new MultiplicativeGenerator((ulong)DateTime.Now.Ticks);
                foreach (int N in new[] { 10, 100, 1000, 10000 }) {
                    int[] counts = new int[xVals.Count]; double sum = 0, sumSq = 0;
                    for (int i = 0; i < N; i++) {
                        double A = rnd.NextDouble();
                        for (int j = 0; j < pVals.Count; j++) {
                            A = A - pVals[j];
                            if (A <= 0) {
                                counts[j]++; 
                                sum += xVals[j]; 
                                sumSq += xVals[j] * xVals[j]; 
                                break; 
                            }
                        }
                    }
                    double mE = sum / N, dE = (sumSq / N) - (mE * mE);
                    double chi = 0; for (int i = 0; i < xVals.Count; i++) chi += Math.Pow(counts[i] - pVals[i] * N, 2) / (pVals[i] * N + 1e-10);
                    string res = chi < 9.488 ? "ПРОЙДЕН" : "ОТКЛОНЕН";
                    dResultGrid.Rows.Add(N, mE.ToString("F3"), (Math.Abs(mE - mT) / (Math.Abs(mT) + 1e-10) * 100).ToString("F1") + "%",
                        dE.ToString("F3"), (Math.Abs(dE - dT) / (dT + 1e-10) * 100).ToString("F1") + "%", chi.ToString("F2"), res);
                    if (N == 10000) {
                        dChart.Series[0].Points.Clear();
                        for (int i = 0; i < xVals.Count; i++) dChart.Series[0].Points.AddXY(xVals[i], (double)counts[i] / N);
                    }
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void RunNormal()
        {
            try {
                double mu = double.Parse(nMuInput.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double varT = double.Parse(nVarInput.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double sigma = Math.Sqrt(varT);

                nResultGrid.Rows.Clear(); 
                MultiplicativeGenerator rnd = new MultiplicativeGenerator((ulong)DateTime.Now.Ticks);
                foreach (int N in new[] { 10, 100, 1000, 10000 }) {
                    double[] s = new double[N];
                    for (int i = 0; i < N; i++) {
                        double u1 = 1.0 - rnd.NextDouble(), u2 = 1.0 - rnd.NextDouble();
                        s[i] = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2) * sigma + mu;
                    }
                    double mE = s.Average(), dE = s.Select(v => Math.Pow(v - mE, 2)).Sum() / N;
                    
                    double min = s.Min(), max = s.Max(), step = (max - min) / 10;
                    int[] counts = new int[10];
                    foreach (var v in s) { int b = (int)((v - min) / step); counts[Math.Min(b, 9)]++; }

                    double chi = 0;
                    for (int i = 0; i < 10; i++) {
                        double pT = NormalCDF(min + (i + 1) * step, mu, sigma) - NormalCDF(min + i * step, mu, sigma);
                        chi += Math.Pow(counts[i] - pT * N, 2) / (pT * N + 1e-10);
                    }
                    string res = chi < 16.919 ? "ПРОЙДЕН" : "ОТКЛОНЕН";
                    nResultGrid.Rows.Add(N, mE.ToString("F3"), (Math.Abs(mE - mu) / (Math.Abs(mu) + 1e-10) * 100).ToString("F1") + "%",
                        dE.ToString("F3"), (Math.Abs(dE - varT) / varT * 100).ToString("F1") + "%", chi.ToString("F2"), res);

                    if (N == 10000) {
                        nChart.Series[0].Points.Clear(); nChart.Series[1].Points.Clear();
                        for (int i = 0; i < 10; i++) nChart.Series[0].Points.AddXY(min + i * step + step / 2, (double)counts[i] / (N * step));
                        for (double x = min; x <= max; x += step / 2)
                            nChart.Series[1].Points.AddXY(x, Math.Exp(-Math.Pow(x - mu, 2) / (2 * varT)) / (sigma * Math.Sqrt(2 * Math.PI)));
                    }
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private double NormalCDF(double x, double mu, double sigma) {
            double t = (x - mu) / (sigma * Math.Sqrt(2.0));
            return 0.5 * (1.0 + Erf(t));
        }

        private double Erf(double x) {
            double a1 = 0.254829592, a2 = -0.284496736, a3 = 1.421413741, a4 = -1.453152027, a5 = 1.061405429, p = 0.3275911;
            int s = Math.Sign(x); x = Math.Abs(x);
            double t = 1.0 / (1.0 + p * x);
            return s * (1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x));
        }
    }
}