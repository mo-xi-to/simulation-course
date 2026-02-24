using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeatLabWin
{
    public partial class Form1 : Form
    {
        const double rho = 2700, c = 900, lmd = 230;
        
        DataGridView grid;
        PictureBox canvas;
        Button btnRun;
        double[] currentProfile;

        public Form1()
        {
            this.Text = "Теплопроводность: алюминиевая пластина (L=0.1м, T=2с)";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            btnRun = new Button() { 
                Text = "Начать моделирование", 
                Location = new Point(20, 15), Width = 200, Height = 40,
                Font = new Font("Segoe UI", 9)
            };
            btnRun.Click += async (s, e) => await RunSimulation();
            this.Controls.Add(btnRun);

            canvas = new PictureBox() {
                Location = new Point(20, 70), Width = 940, Height = 300,
                BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            canvas.Paint += Canvas_Paint;
            this.Controls.Add(canvas);

            grid = new DataGridView() {
                Location = new Point(20, 390), Width = 940, Height = 250,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false
            };
            grid.Columns.Add("dt", "dt \\ h");
            string[] hSteps = { "0.1", "0.01", "0.001", "0.0001" };
            foreach (var h in hSteps) grid.Columns.Add(h, h);
            this.Controls.Add(grid);
        }

        private async Task RunSimulation()
        {
            double L = 0.1, Tl = 100, Tr = 100, Tinit = 20, totalTime = 2.0;
            double[] dts = { 0.1, 0.01, 0.001, 0.0001 };
            double[] hs = { 0.1, 0.01, 0.001, 0.0001 };

            btnRun.Enabled = false;
            grid.Rows.Clear();

            foreach (double dt in dts)
            {
                int rowIndex = grid.Rows.Add();
                grid.Rows[rowIndex].Cells[0].Value = dt;

                for (int j = 0; j < hs.Length; j++)
                {
                    double[] res = await SolveHeat(Tl, Tr, Tinit, L, hs[j], totalTime, dt, false);
                    grid.Rows[rowIndex].Cells[j + 1].Value = res[res.Length / 2].ToString("F2");
                }
            }

            await SolveHeat(Tl, Tr, Tinit, L, 0.001, totalTime, 0.01, true);
            
            btnRun.Enabled = true;
        }

        private async Task<double[]> SolveHeat(double Tl, double Tr, double Tinit, double L, double h, double totalTime, double dt, bool isAnimated)
        {
            int Nx = Math.Max((int)(L / h), 2);
            int Nt = (int)(totalTime / dt);
            
            double[] T = new double[Nx + 1];
            for (int i = 0; i <= Nx; i++) T[i] = Tinit;
            T[0] = Tl; T[Nx] = Tr;

            double A = lmd / (h * h), C = A, B = 2 * lmd / (h * h) + (rho * c / dt);
            double invDtRhoC = (rho * c / dt);

            double[] alpha = new double[Nx + 1], beta = new double[Nx + 1];
            
            alpha[0] = 0;
            for (int i = 1; i < Nx; i++)
                alpha[i] = A / (B - C * alpha[i - 1]);

            for (int t = 0; t < Nt; t++)
            {
                beta[0] = Tl;
                for (int i = 1; i < Nx; i++)
                    beta[i] = (C * beta[i - 1] + invDtRhoC * T[i]) / (B - C * alpha[i - 1]);

                double[] nextT = new double[Nx + 1];
                nextT[0] = Tl; nextT[Nx] = Tr;
                for (int i = Nx - 1; i > 0; i--)
                    nextT[i] = alpha[i] * nextT[i + 1] + beta[i];

                T = nextT;

                if (isAnimated && t % 2 == 0)
                {
                    currentProfile = T;
                    canvas.Invalidate();
                    await Task.Delay(1);
                }
            }
            return T;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            int margin = 45;
            int w = canvas.Width - 2 * margin;
            int h = canvas.Height - 2 * margin;

            Pen axisPen = new Pen(Color.Black, 2);
            Pen gridPen = new Pen(Color.LightGray, 1);
            Font font = new Font("Arial", 8);

            for (int i = 0; i <= 5; i++)
            {
                float y = (canvas.Height - margin) - (i * 20 * h / 100f);
                g.DrawLine(gridPen, margin, y, margin + w, y);
                g.DrawString((i * 20).ToString(), font, Brushes.Black, margin - 30, y - 7);

                float x = margin + (i * 0.02f * w / 0.1f);
                g.DrawLine(gridPen, x, margin, x, canvas.Height - margin);
                g.DrawString((i * 0.02).ToString("F2"), font, Brushes.Black, x - 10, canvas.Height - margin + 5);
            }
            g.DrawLine(axisPen, margin, canvas.Height - margin, margin + w, canvas.Height - margin);
            g.DrawLine(axisPen, margin, margin, margin, canvas.Height - margin);
            g.DrawString("T, °C", font, Brushes.Black, margin - 40, margin - 20);
            g.DrawString("L, м", font, Brushes.Black, margin + w + 5, canvas.Height - margin - 5);

            if (currentProfile != null)
            {
                Pen graphPen = new Pen(Color.Red, 3);
                float stepX = (float)w / (currentProfile.Length - 1);
                float scaleY = (float)h / 100f;

                for (int i = 0; i < currentProfile.Length - 1; i++)
                {
                    float x1 = margin + i * stepX;
                    float y1 = (canvas.Height - margin) - (float)currentProfile[i] * scaleY;
                    float x2 = margin + (i + 1) * stepX;
                    float y2 = (canvas.Height - margin) - (float)currentProfile[i + 1] * scaleY;
                    g.DrawLine(graphPen, x1, Math.Max(y1, (float)margin), x2, Math.Max(y2, (float)margin));
                }
            }
        }
    }
}