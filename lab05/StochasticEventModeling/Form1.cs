using System;
using System.Drawing;
using System.Windows.Forms;

namespace StochasticEventModeling
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

    public partial class Form1 : Form
    {
        private readonly MultiplicativeGenerator _generator = new MultiplicativeGenerator((ulong)DateTime.Now.Ticks);
        private Label _displayLabel;

        private readonly double _yesProbability = 0.5; 

        private readonly double[] _ballProbabilities = {0.1, 0.2, 0.2, 0.2, 0.15, 0.15}; 
        private readonly string[] _ballAnswers = { 
            "Бесспорно!", 
            "Предрешено", 
            "Спроси позже", 
            "Сконцентрируйся и попробуй снова", 
            "Мой ответ - нет", 
            "Весьма сомнительно" 
        };

        public Form1()
        {
            InitializeUserInterface();
        }

        private void InitializeUserInterface()
        {
            this.Text = "Лабораторная работа №5";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblPart1 = new Label { Text = "Часть 1: Да/Нет", Location = new Point(30, 20), Size = new Size(300, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            Button btnYesNo = new Button { Text = "Узнать ответ", Location = new Point(30, 45), Size = new Size(200, 40) };
            btnYesNo.Click += (s, e) => ExecutePart1();

            Label lblPart2 = new Label { Text = "Часть 2: магический шар предсказаний", Location = new Point(30, 110), Size = new Size(350, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            Button btnMagicBall = new Button { Text = "Встряхнуть шар", Location = new Point(30, 135), Size = new Size(200, 40) };
            btnMagicBall.Click += (s, e) => ExecutePart2();

            _displayLabel = new Label 
            { 
                Text = "Нажмите кнопку, чтобы получить ответ...", 
                Location = new Point(30, 210), 
                Size = new Size(420, 160),
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.WhiteSmoke
            };

            this.Controls.AddRange(new Control[] { lblPart1, btnYesNo, lblPart2, btnMagicBall, _displayLabel });
        }

        private void ExecutePart1()
        {
            double alpha = _generator.NextDouble();
            double tempValue = alpha;

            tempValue -= _yesProbability;

            if (tempValue <= 0)
            {
                _displayLabel.Text = "ДА!";
                _displayLabel.ForeColor = Color.Green;
            }
            else
            {
                _displayLabel.Text = "НЕТ!";
                _displayLabel.ForeColor = Color.Red;
            }

            _displayLabel.Text += $"\n\n(Alpha: {alpha:F6}, Остаток: {tempValue:F6})";
        }

        private void ExecutePart2()
        {
            double alpha = _generator.NextDouble();
            double tempValue = alpha;
            int eventIndex = -1;

            for (int i = 0; i < _ballProbabilities.Length; i++)
            {
                tempValue -= _ballProbabilities[i];
                if (tempValue <= 0)
                {
                    eventIndex = i;
                    break;
                }
            }

            if (eventIndex != -1)
            {
                _displayLabel.ForeColor = Color.DarkBlue;
                _displayLabel.Text = $"ШАР ГОВОРИТ:\n\n{_ballAnswers[eventIndex]}";
                _displayLabel.Text += $"\n\n(Alpha: {alpha:F6}, Вероятность события: {_ballProbabilities[eventIndex]})";
            }
        }
    }
}