using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MarkovWeatherContinuousGUI
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
    public class Form1 : Form 
    {
        private DataGridView gridQ = null!;
        private Button btnStart = null!;
        private RichTextBox txtLog = null!;
        private Chart chartStats = null!;
        private TextBox txtTotalHours = null!;
        private TextBox txtSpeedMs = null!;
        private MultiplicativeGenerator myRnd = null!;

        private readonly string[] WeatherStates = { "Ясно", "Облачно", "Пасмурно" };
        private readonly Color[] StateColors = { Color.DarkOrange, Color.SteelBlue, Color.DimGray };
        private readonly double[] InitialProbabilities = { 0.4, 0.4, 0.2 }; 
        
        private double[,] Q = new double[3, 3];
        private bool isRunning = false;
        private bool isUpdatingTable = false; 

        public Form1()
        {
            InitializeComponentProgrammatically();
            SetupDefaultMatrix();
        }

        private void InitializeComponentProgrammatically()
        {
            this.Text = "Лабораторная работа №7";
            this.Size = new Size(1300, 750); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            Panel panelLeft = new Panel { Dock = DockStyle.Left, Width = 550, Padding = new Padding(10), BackColor = Color.WhiteSmoke };
            
            Label lblMatrix = new Label { 
                Text = "Матрица интенсивностей Q\nЧисло = сколько раз в час погода переходит из строки в столбец\nДиагональ (красная) считается автоматически", 
                Location = new Point(10, 10), Size = new Size(530, 60), Font = new Font("Segoe UI", 9, FontStyle.Bold) 
            };
            
            gridQ = new DataGridView { 
                Location = new Point(10, 75), 
                Width = 520, 
                Height = 135, 
                AllowUserToAddRows = false, 
                RowHeadersVisible = true,
                RowHeadersWidth = 160, 
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.None
            };
            gridQ.ColumnCount = 3;
            gridQ.Columns[0].Name = "В Ясно"; gridQ.Columns[1].Name = "В Облачно"; gridQ.Columns[2].Name = "В Пасмурно";
            gridQ.Rows.Add(3);
            gridQ.Rows[0].HeaderCell.Value = "Из Ясно";
            gridQ.Rows[1].HeaderCell.Value = "Из Облачно";
            gridQ.Rows[2].HeaderCell.Value = "Из Пасмурно";

            Label lblHours = new Label { Text = "Общее время (часы):", AutoSize = true, Location = new Point(10, 230) };
            txtTotalHours = new TextBox { Text = "720", Location = new Point(300, 227), Width = 100 };

            Label lblSpeed = new Label { Text = "Задержка (мс):\n(0 - мгновенно)", AutoSize = true, Location = new Point(10, 260) };
            txtSpeedMs = new TextBox { Text = "0", Location = new Point(300, 265), Width = 100 };

            btnStart = new Button { 
                Text = "ЗАПУСТИТЬ МОДЕЛИРОВАНИЕ", 
                Location = new Point(10, 330), 
                Size = new Size(520, 60), 
                BackColor = Color.LightGreen, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            panelLeft.Controls.AddRange(new Control[] { lblMatrix, gridQ, lblHours, txtTotalHours, lblSpeed, txtSpeedMs, btnStart });

            txtLog = new RichTextBox { Location = new Point(570, 10), Width = 700, Height = 280, ReadOnly = true, Font = new Font("Consolas", 9), BackColor = Color.White };
            
            chartStats = new Chart { Location = new Point(570, 300), Size = new Size(700, 400) };
            ChartArea chartArea = new ChartArea("MainArea");
            
            chartArea.AxisX.IsLabelAutoFit = false;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.Maximum = 1.0; 
            chartArea.AxisY.Title = "Вероятность (0.0 - 1.0)";
            
            chartStats.ChartAreas.Add(chartArea);
            
            Legend legend = new Legend("MainLegend") { Docking = Docking.Right, Alignment = StringAlignment.Center };
            chartStats.Legends.Add(legend);

            Series sEmp = new Series("Эмпирическая") { ChartType = SeriesChartType.Column, Color = Color.SteelBlue, IsValueShownAsLabel = true, LabelFormat = "F3" };
            Series sTheo = new Series("Теоретическая") { ChartType = SeriesChartType.Column, Color = Color.Crimson, IsValueShownAsLabel = true, LabelFormat = "F3" };
            
            chartStats.Series.Add(sEmp);
            chartStats.Series.Add(sTheo);

            this.Controls.Add(panelLeft);
            this.Controls.Add(txtLog);
            this.Controls.Add(chartStats);

            gridQ.CellValueChanged += (s, e) => UpdateDiagonals();
        }

        private void SetupDefaultMatrix()
        {
            isUpdatingTable = true;
            gridQ.Rows[0].Cells[1].Value = "0.2";  gridQ.Rows[0].Cells[2].Value = "0.05";
            gridQ.Rows[1].Cells[0].Value = "0.15"; gridQ.Rows[1].Cells[2].Value = "0.2";
            gridQ.Rows[2].Cells[0].Value = "0.05"; gridQ.Rows[2].Cells[1].Value = "0.3";
            isUpdatingTable = false;
            UpdateDiagonals();
        }

        private void UpdateDiagonals()
        {
            if (isUpdatingTable) return; 
            isUpdatingTable = true;
            for (int i = 0; i < 3; i++)
            {
                double sum = 0;
                for (int j = 0; j < 3; j++)
                {
                    if (i != j && double.TryParse(Convert.ToString(gridQ.Rows[i].Cells[j].Value)?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                        sum += val;
                }
                gridQ.Rows[i].Cells[i].Value = (-sum).ToString("F2", CultureInfo.InvariantCulture);
                gridQ.Rows[i].Cells[i].Style.BackColor = Color.MistyRose;
                gridQ.Rows[i].Cells[i].ReadOnly = true;
                gridQ.Rows[i].Cells[i].Style.ForeColor = Color.DarkRed;
            }
            isUpdatingTable = false;
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (isRunning) return;
            try 
            {
                isRunning = true;
                btnStart.Enabled = false;
                btnStart.Text = "РАБОТАЮ...";
                txtLog.Clear();

                double speedRaw = double.Parse(txtSpeedMs.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                double totalHours = double.Parse(txtTotalHours.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        Q[i, j] = double.Parse(gridQ.Rows[i].Cells[j].Value?.ToString()?.Replace(',', '.') ?? "0", CultureInfo.InvariantCulture);

                double[] theoretical = CalculateTheoreticalDist(Q);
                double[] timeInState = new double[3];
                myRnd = new MultiplicativeGenerator((ulong)DateTime.Now.Ticks);
                double currentTime = 0.0;
                int currentState = GetDiscreteState(myRnd.NextDouble(), InitialProbabilities);

                LogMessage($"СТАРТ. Начало: {WeatherStates[currentState]}", Color.Black);

                while (currentTime < totalHours)
                {
                    double q_ii = Q[currentState, currentState];
                    if (q_ii >= 0) break; 
                    double tau = Math.Log(1.0 - myRnd.NextDouble()) / q_ii;
                    if (currentTime + tau > totalHours) tau = totalHours - currentTime;

                    timeInState[currentState] += tau;
                    LogWeather(currentTime, tau, currentState);

                    if (speedRaw > 0) await Task.Delay((int)(tau * speedRaw));

                    currentTime += tau;
                    if (currentTime >= totalHours) break;

                    double r = myRnd.NextDouble();
                    double cum = 0;
                    for (int j = 0; j < 3; j++) {
                        if (j == currentState) continue;
                        cum += Q[currentState, j] / -q_ii;
                        if (r <= cum) { currentState = j; break; }
                    }
                }

                chartStats.Series["Эмпирическая"].Points.Clear();
                chartStats.Series["Теоретическая"].Points.Clear();
                chartStats.ChartAreas[0].AxisX.CustomLabels.Clear();

                for (int i = 0; i < 3; i++) {
                    double empProb = timeInState[i] / totalHours;
                    chartStats.Series["Эмпирическая"].Points.AddXY(i, empProb);
                    chartStats.Series["Теоретическая"].Points.AddXY(i, theoretical[i]);
                    chartStats.ChartAreas[0].AxisX.CustomLabels.Add(i - 0.5, i + 0.5, WeatherStates[i]);
                }

                SaveToCSV(timeInState, theoretical, totalHours);
                LogMessage("ЗАВЕРШЕНО. Файлы сохранены.", Color.Green);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            finally { isRunning = false; btnStart.Enabled = true; btnStart.Text = "ЗАПУСТИТЬ МОДЕЛИРОВАНИЕ"; }
        }

        private int GetDiscreteState(double r, double[] probs)
        {
            double cum = 0;
            for (int i = 0; i < probs.Length; i++) { cum += probs[i]; if (r <= cum) return i; }
            return probs.Length - 1;
        }

        private void LogWeather(double t, double d, int s)
        {
            TimeSpan tsS = TimeSpan.FromHours(t);
            TimeSpan tsE = TimeSpan.FromHours(t + d);
            TimeSpan tsD = TimeSpan.FromHours(d);
            string msg = $"[{(int)tsS.TotalHours:D2}:{tsS.Minutes:D2} - {(int)tsE.TotalHours:D2}:{tsE.Minutes:D2}] {WeatherStates[s],-9} | Дл: {(int)tsD.TotalHours:D2}ч {tsD.Minutes:D2}м";
            LogMessage(msg, StateColors[s]);
        }

        private void LogMessage(string text, Color color)
        {
            if (txtLog.InvokeRequired) { txtLog.Invoke(new Action(() => LogMessage(text, color))); return; }
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = color;
            txtLog.AppendText(text + Environment.NewLine);
            if (txtSpeedMs.Text != "0") txtLog.ScrollToCaret();
        }

        private double[] CalculateTheoreticalDist(double[,] m)
        {
            double[,] A = {{m[0,0], m[1,0], m[2,0]}, {m[0,1], m[1,1], m[2,1]}, {1, 1, 1}};
            double[] B = {0, 0, 1};
            double det = A[0,0]*(A[1,1]*A[2,2]-A[1,2]*A[2,1]) - A[0,1]*(A[1,0]*A[2,2]-A[1,2]*A[2,0]) + A[0,2]*(A[1,0]*A[2,1]-A[1,1]*A[2,0]);
            if (Math.Abs(det) < 1e-9) return new double[] {0.33, 0.33, 0.34};
            double[] res = new double[3];
            for (int i = 0; i < 3; i++) {
                double[,] Ai = (double[,])A.Clone(); Ai[0,i]=B[0]; Ai[1,i]=B[1]; Ai[2,i]=B[2];
                double detI = Ai[0,0]*(Ai[1,1]*Ai[2,2]-Ai[1,2]*Ai[2,1]) - Ai[0,1]*(Ai[1,0]*Ai[2,2]-Ai[1,2]*Ai[2,0]) + Ai[0,2]*(Ai[1,0]*Ai[2,1]-Ai[1,1]*Ai[2,0]);
                res[i] = detI / det;
            }
            return res;
        }

        private void SaveToCSV(double[] timeSpent, double[] theo, double total)
        {
            try {
                string stats = "State;Empirical;Theoretical;Error\n";
                for (int i = 0; i < 3; i++) {
                    double emp = timeSpent[i] / total;
                    stats += string.Format(CultureInfo.InvariantCulture, "{0};{1:F4};{2:F4};{3:F4}\n", 
                        WeatherStates[i], emp, theo[i], Math.Abs(emp - theo[i]));
                }
                File.WriteAllText("weather_stats.csv", stats);
            } catch { }
        }
    }
    
    static class Program {
        [STAThread] static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}