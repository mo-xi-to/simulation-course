using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BaseRandomApp
{
    public class MultiplicativeGenerator
    {
        private ulong _state;
        private const ulong M = 9223372036854775808UL;
        private const ulong Beta = 4294967299UL;

        public MultiplicativeGenerator(ulong seed)
        {
            _state = seed;
        }

        public double NextDouble()
        {
            _state = (Beta * _state) % M;
            return (double)_state / M;
        }
    }

    public class MainForm : Form
    {
        private Button btnStart;
        private DataGridView grid;

        public MainForm()
        {
            this.Text = "Лабораторная работа №4";
            this.Size = new Size(800, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnStart = new Button {
                Text = "Выполнить расчет (N = 100 000)",
                Size = new Size(760, 50),
                Location = new Point(10, 10),
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnStart.Click += CalculateAll;

            grid = new DataGridView {
                Location = new Point(10, 70),
                Size = new Size(760, 250),
                ColumnCount = 4,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            grid.Columns[0].Name = "Показатель";
            grid.Columns[1].Name = "Теория";
            grid.Columns[2].Name = "Мультипликативный генератор";
            grid.Columns[3].Name = "Встроенный генератор";

            this.Controls.Add(btnStart);
            this.Controls.Add(grid);
        }

        private void CalculateAll(object sender, EventArgs e)
        {
            int N = 100000;
            double[] customData = new double[N];
            double[] systemData = new double[N];

            var myGen = new MultiplicativeGenerator(4294967299UL);
            var sysGen = new Random();

            for (int i = 0; i < N; i++)
            {
                customData[i] = myGen.NextDouble();
                systemData[i] = sysGen.NextDouble();
            }

            var (cMean, cVar) = GetStats(customData);
            var (sMean, sVar) = GetStats(systemData);

            grid.Rows.Clear();
            grid.Rows.Add("Выборочное среднее", "0.500000", cMean.ToString("F6"), sMean.ToString("F6"));
            grid.Rows.Add("Выборочная дисперсия", "0.083333", cVar.ToString("F6"), sVar.ToString("F6"));
            
            grid.Rows.Add("Погрешность среднего", "0.000000", Math.Abs(cMean - 0.5).ToString("F6"), Math.Abs(sMean - 0.5).ToString("F6"));
        }

        private (double mean, double var) GetStats(double[] data)
        {
            double mean = data.Average();
            double variance = data.Select(x => Math.Pow(x - mean, 2)).Sum() / data.Length;
            return (mean, variance);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }
}