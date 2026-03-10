using System;
using System.Drawing;
using System.Windows.Forms;

namespace ForestFire
{
    public enum CellState
    {
        Empty = 0,
        Tree = 1,
        Burning = 2,
        Water = 3
    }

    public class Form1 : Form
    {
        private const int GridWidth = 100;
        private const int GridHeight = 80;
        private const int CellSize = 8;

        private CellState[,] grid;
        private CellState[,] nextGrid;
        
        private System.Windows.Forms.Timer timer;
        
        private Random random = new Random();
        private PictureBox pictureBox;
        
        private ComboBox cmbWindDirection;
        private TrackBar trackHumidity;
        private Label lblHumidity;
        private Button btnReset;
        private Button btnStartStop;

        private int windDx = 0; 
        private int windDy = 0; 
        private double humidity = 0.0; 
        private double probGrow = 0.01;
        private double probLightning = 0.0001;

        public Form1()
        {
            InitializeGrid();
            SetupUI();
            DrawGrid();
        }

        private void SetupUI()
        {
            this.Size = new Size(GridWidth * CellSize + 240, GridHeight * CellSize + 60);
            this.Text = "Симуляция лесного пожара";
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            pictureBox = new PictureBox();
            pictureBox.Location = new Point(10, 10);
            pictureBox.Size = new Size(GridWidth * CellSize, GridHeight * CellSize);
            pictureBox.BackColor = Color.Black;
            
            pictureBox.MouseDown += (s, e) => 
            {
                HandleMouseInput(e);
            };
            pictureBox.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                    HandleMouseInput(e);
            };

            this.Controls.Add(pictureBox);

            int uiX = pictureBox.Right + 15;
            
            btnStartStop = new Button { Text = "Старт", Location = new Point(uiX, 10), Width = 160, Height = 30 };
            btnStartStop.Click += (s, e) => {
                if (timer.Enabled) { timer.Stop(); btnStartStop.Text = "Старт"; }
                else { timer.Start(); btnStartStop.Text = "Пауза"; }
            };
            this.Controls.Add(btnStartStop);

            btnReset = new Button { Text = "Регенерация", Location = new Point(uiX, 50), Width = 160, Height = 30 };
            btnReset.Click += (s, e) => { InitializeGrid(); DrawGrid(); };
            this.Controls.Add(btnReset);

            Label lblWind = new Label { Text = "Ветер:", Location = new Point(uiX, 100), AutoSize = true };
            this.Controls.Add(lblWind);

            cmbWindDirection = new ComboBox { Location = new Point(uiX, 120), Width = 160 };
            cmbWindDirection.Items.AddRange(new object[] { "Нет ветра", "Север (Вверх)", "Юг (Вниз)", "Запад (Влево)", "Восток (Вправо)" });
            cmbWindDirection.SelectedIndex = 0;
            cmbWindDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWindDirection.SelectedIndexChanged += (s, e) => UpdateWind();
            this.Controls.Add(cmbWindDirection);

            lblHumidity = new Label { Text = "Влажность: 0%", Location = new Point(uiX, 160), AutoSize = true };
            this.Controls.Add(lblHumidity);

            trackHumidity = new TrackBar { Location = new Point(uiX, 180), Width = 160, Minimum = 0, Maximum = 100, Value = 0 };
            trackHumidity.Scroll += (s, e) => {
                humidity = trackHumidity.Value / 100.0;
                lblHumidity.Text = $"Влажность: {trackHumidity.Value}%";
            };
            this.Controls.Add(trackHumidity);

            Label lblHelp = new Label { 
                Text = "ЛКМ: Вода\nПКМ: Огонь", 
                Location = new Point(uiX, 230), 
                AutoSize = true,
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblHelp);

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50; 
            timer.Tick += (s, e) => {
                UpdateModel();
                DrawGrid();
            };
        }

        private void HandleMouseInput(MouseEventArgs e)
        {
            int cx = e.X / CellSize;
            int cy = e.Y / CellSize;
            if(cx >= 0 && cx < GridWidth && cy >= 0 && cy < GridHeight)
            {
                if (e.Button == MouseButtons.Left) grid[cx, cy] = CellState.Water;
                else if (e.Button == MouseButtons.Right) grid[cx, cy] = CellState.Burning;
                DrawGrid();
            }
        }

        private void UpdateWind()
        {
            windDx = 0; windDy = 0;
            switch (cmbWindDirection.SelectedIndex)
            {
                case 1: windDy = -1; break;
                case 2: windDy = 1; break;
                case 3: windDx = -1; break;
                case 4: windDx = 1; break;
            }
        }

        private void InitializeGrid()
        {
            grid = new CellState[GridWidth, GridHeight];
            nextGrid = new CellState[GridWidth, GridHeight];

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    double r = random.NextDouble();
                    if (r < 0.05) grid[x, y] = CellState.Water; 
                    else if (r < 0.6) grid[x, y] = CellState.Tree; 
                    else grid[x, y] = CellState.Empty;
                }
            }
        }

        private void UpdateModel()
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    CellState current = grid[x, y];
                    
                    if (current == CellState.Burning) nextGrid[x, y] = CellState.Empty;
                    else if (current == CellState.Empty)
                    {
                        if (random.NextDouble() < probGrow) nextGrid[x, y] = CellState.Tree;
                        else nextGrid[x, y] = CellState.Empty;
                    }
                    else if (current == CellState.Tree)
                    {
                        if (ShouldBurn(x, y)) nextGrid[x, y] = CellState.Burning;
                        else nextGrid[x, y] = CellState.Tree;
                    }
                    else if (current == CellState.Water) nextGrid[x, y] = CellState.Water;
                }
            }
            var temp = grid; grid = nextGrid; nextGrid = temp;
        }

        private bool ShouldBurn(int x, int y)
        {
            if (random.NextDouble() < probLightning) return true;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                    {
                        if (grid[nx, ny] == CellState.Burning)
                        {
                            double burnChance = 0.5; 

                            if (windDx == -dx && windDy == -dy) burnChance = 0.95;
                            else if (windDx == dx && windDy == dy) burnChance = 0.05;

                            burnChance -= (humidity * 0.4); 

                            if (random.NextDouble() < burnChance) return true; 
                        }
                    }
                }
            }
            return false;
        }

        private void DrawGrid()
        {
            if (pictureBox.Image == null)
                pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);

            using (Graphics g = Graphics.FromImage(pictureBox.Image))
            {
                g.Clear(Color.Black);

                for (int x = 0; x < GridWidth; x++)
                {
                    for (int y = 0; y < GridHeight; y++)
                    {
                        CellState state = grid[x, y];
                        if (state == CellState.Empty) continue;

                        Brush brush = Brushes.Black;
                        switch (state)
                        {
                            case CellState.Tree: brush = Brushes.ForestGreen; break;
                            case CellState.Burning: brush = Brushes.OrangeRed; break;
                            case CellState.Water: brush = Brushes.DeepSkyBlue; break;
                        }
                        g.FillRectangle(brush, x * CellSize, y * CellSize, CellSize - 1, CellSize - 1);
                    }
                }
            }
            pictureBox.Refresh();
        }
    }
}