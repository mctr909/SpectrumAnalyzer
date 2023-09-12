using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpectrumAnalyzer {
    public partial class Form1 : Form {
        private SpectrumAnalyzer mWaveIn;
        private DoubleBufferGraphic mGraph;

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            mGraph = new DoubleBufferGraphic(pictureBox1, null);
            mWaveIn = new SpectrumAnalyzer(32, 44100, 20);

            var list = WinMM.WaveIn.GetList();
            comboBox1.Items.Clear();
            foreach(var str in list) {
                comboBox1.Items.Add(str.Item1);
            }
            comboBox1.SelectedIndex = 0;

            timer1.Interval = 10;
            timer1.Start();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            var deviceNum = 0xFFFFFFFF;
            if (0 < comboBox1.SelectedIndex) {
                deviceNum = (uint)comboBox1.SelectedIndex - 1;
            }
            mWaveIn.Open(deviceNum);
        }

        private void timer1_Tick(object sender, EventArgs e) {
            var graph = mGraph.Graphics;
            var width = pictureBox1.Width;
            var height = pictureBox1.Height;

            var divLevel = 24;
            var divX = (float)width / mWaveIn.Amp.Length;
            var divY = (float)height / divLevel;

            var black = new Pen(Color.FromArgb(11, 31, 31), 1.0f).Brush;
            var green = new Pen(Color.FromArgb(127, 211, 211), 1.0f).Brush;

            for (int x = 0; x < mWaveIn.Amp.Length; x++) {
                var px = x * divX;
                var db = divLevel + (int)Math.Max(-divLevel, mWaveIn.Amp[x] * 0.5 + 9);
                for (int y = 0; y < db; y++) {
                    var py = height - y * divY;
                    graph.FillRectangle(green, px, py, divX - 2, divY - 3);
                }
                for (int y = db; y <= divLevel; y++) {
                    var py = height - y * divY;
                    graph.FillRectangle(black, px, py, divX - 2, divY - 3);
                }
            }

            mGraph.Render();
        }
    }
}
