using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeAna {
    public partial class Form1 : Form {
        private SpeAna mWaveIn;
        private DoubleBufferGraphic mGraph;

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            mGraph = new DoubleBufferGraphic(pictureBox1, null);
            mWaveIn = new SpeAna(24, 44100, 30);

            var list = WinMM.WaveIn.GetList();
            comboBox1.Items.Clear();
            foreach(var str in list) {
                comboBox1.Items.Add(str.Item1);
            }
            comboBox1.SelectedIndex = 0;

            timer1.Interval = 10;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            var graph = mGraph.Graphics;
            var width = pictureBox1.Width;
            var height = pictureBox1.Height;

            var divLevel = 24;
            var divX = (float)width / mWaveIn.Amp.Length;
            var divY = (float)height / divLevel;

            var black = new Pen(Color.FromArgb(0, 32, 0), 1.0f).Brush;
            var green = new Pen(Color.FromArgb(0, 192, 0), 1.0f).Brush;

            for (int x = 0; x < mWaveIn.Amp.Length; x++) {
                var px = x * divX;
                var db = divLevel + (int)Math.Max(-divLevel, mWaveIn.Amp[x] * 3 / 5 + 15);
                for (int y = 0; y < db; y++) {
                    var py = height - y * divY;
                    graph.FillRectangle(green, px, py, divX - 2, divY - 2);
                }
                for (int y = db; y <= 30; y++) {
                    var py = height - y * divY;
                    graph.FillRectangle(black, px, py, divX - 2, divY - 2);
                }
            }

            mGraph.Render();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboBox1.SelectedIndex == 0) {
                mWaveIn.Open(0xFFFFFFFF);
            } else {
                mWaveIn.Open((uint)comboBox1.SelectedIndex - 1);
            }
        }
    }
}
