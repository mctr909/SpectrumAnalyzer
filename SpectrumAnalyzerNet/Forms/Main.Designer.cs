namespace SpectrumAnalyzerNet.Forms {
	partial class Main {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			components = new System.ComponentModel.Container();
			toolStrip = new ToolStrip();
			tsbOpen = new ToolStripButton();
			toolStripSeparator1 = new ToolStripSeparator();
			tsbPrev = new ToolStripButton();
			tsbRew = new ToolStripButton();
			tsbPlay = new ToolStripButton();
			tsbNext = new ToolStripButton();
			toolStripSeparator2 = new ToolStripSeparator();
			tsbRec = new ToolStripButton();
			toolStripSeparator3 = new ToolStripSeparator();
			tsbSettings = new ToolStripButton();
			toolStripSeparator4 = new ToolStripSeparator();
			progressBar = new ToolStripProgressBar();
			splitContainer1 = new SplitContainer();
			picChart = new PictureBox();
			picScroll = new PictureBox();
			timer1 = new System.Windows.Forms.Timer(components);
			openFileDialog1 = new OpenFileDialog();
			toolStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)picChart).BeginInit();
			((System.ComponentModel.ISupportInitialize)picScroll).BeginInit();
			SuspendLayout();
			// 
			// toolStrip
			// 
			toolStrip.AutoSize = false;
			toolStrip.CanOverflow = false;
			toolStrip.Items.AddRange(new ToolStripItem[] { tsbOpen, toolStripSeparator1, tsbPrev, tsbRew, tsbPlay, tsbNext, toolStripSeparator2, tsbRec, toolStripSeparator3, tsbSettings, toolStripSeparator4, progressBar });
			toolStrip.Location = new Point(0, 0);
			toolStrip.Name = "toolStrip";
			toolStrip.Size = new Size(367, 25);
			toolStrip.TabIndex = 0;
			// 
			// tsbOpen
			// 
			tsbOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbOpen.Image = Properties.Resources.file;
			tsbOpen.ImageTransparentColor = Color.Magenta;
			tsbOpen.Name = "tsbOpen";
			tsbOpen.Size = new Size(23, 22);
			tsbOpen.ToolTipText = "ファイルを開く";
			tsbOpen.Click += ToolTipClicked;
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(6, 25);
			// 
			// tsbPrev
			// 
			tsbPrev.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbPrev.Enabled = false;
			tsbPrev.Image = Properties.Resources.previous;
			tsbPrev.ImageTransparentColor = Color.Magenta;
			tsbPrev.Name = "tsbPrev";
			tsbPrev.Size = new Size(23, 22);
			tsbPrev.Text = "toolStripButton2";
			tsbPrev.ToolTipText = "前のファイルに移動";
			tsbPrev.Click += ToolTipClicked;
			// 
			// tsbRew
			// 
			tsbRew.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbRew.Enabled = false;
			tsbRew.Image = Properties.Resources.restart;
			tsbRew.ImageTransparentColor = Color.Magenta;
			tsbRew.Name = "tsbRew";
			tsbRew.Size = new Size(23, 22);
			tsbRew.Text = "toolStripButton3";
			tsbRew.ToolTipText = "ファイルの始めへ";
			tsbRew.Click += ToolTipClicked;
			// 
			// tsbPlay
			// 
			tsbPlay.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbPlay.Image = Properties.Resources.play;
			tsbPlay.ImageTransparentColor = Color.Magenta;
			tsbPlay.Name = "tsbPlay";
			tsbPlay.Size = new Size(23, 22);
			tsbPlay.Text = "toolStripButton4";
			tsbPlay.ToolTipText = "再生";
			tsbPlay.Click += ToolTipClicked;
			// 
			// tsbNext
			// 
			tsbNext.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbNext.Enabled = false;
			tsbNext.Image = Properties.Resources.next;
			tsbNext.ImageTransparentColor = Color.Magenta;
			tsbNext.Name = "tsbNext";
			tsbNext.Size = new Size(23, 22);
			tsbNext.Text = "toolStripButton5";
			tsbNext.ToolTipText = "次のファイルへ";
			tsbNext.Click += ToolTipClicked;
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new Size(6, 25);
			// 
			// tsbRec
			// 
			tsbRec.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbRec.Image = Properties.Resources.rec;
			tsbRec.ImageTransparentColor = Color.Magenta;
			tsbRec.Name = "tsbRec";
			tsbRec.Size = new Size(23, 22);
			tsbRec.Text = "toolStripButton6";
			tsbRec.ToolTipText = "録音";
			tsbRec.Click += ToolTipClicked;
			// 
			// toolStripSeparator3
			// 
			toolStripSeparator3.Name = "toolStripSeparator3";
			toolStripSeparator3.Size = new Size(6, 25);
			// 
			// tsbSettings
			// 
			tsbSettings.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbSettings.Image = Properties.Resources.setting;
			tsbSettings.ImageTransparentColor = Color.Magenta;
			tsbSettings.Name = "tsbSettings";
			tsbSettings.Size = new Size(23, 22);
			tsbSettings.Text = "toolStripButton7";
			tsbSettings.ToolTipText = "設定";
			tsbSettings.Click += ToolTipClicked;
			// 
			// toolStripSeparator4
			// 
			toolStripSeparator4.Name = "toolStripSeparator4";
			toolStripSeparator4.Size = new Size(6, 25);
			// 
			// progressBar
			// 
			progressBar.AutoSize = false;
			progressBar.MarqueeAnimationSpeed = 1;
			progressBar.Name = "progressBar";
			progressBar.Size = new Size(100, 22);
			progressBar.MouseDown += progressBar_MouseDown;
			progressBar.MouseMove += progressBar_MouseMove;
			progressBar.MouseUp += progressBar_MouseUp;
			// 
			// splitContainer1
			// 
			splitContainer1.Dock = DockStyle.Fill;
			splitContainer1.Location = new Point(0, 25);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Orientation = Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			splitContainer1.Panel1.Controls.Add(picChart);
			splitContainer1.Panel1.SizeChanged += Panel_SizeChanged;
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.Controls.Add(picScroll);
			splitContainer1.Size = new Size(367, 322);
			splitContainer1.SplitterDistance = 184;
			splitContainer1.TabIndex = 1;
			// 
			// picChart
			// 
			picChart.Location = new Point(12, 19);
			picChart.Name = "picChart";
			picChart.Size = new Size(194, 151);
			picChart.TabIndex = 0;
			picChart.TabStop = false;
			// 
			// picScroll
			// 
			picScroll.Location = new Point(12, 15);
			picScroll.Name = "picScroll";
			picScroll.Size = new Size(194, 107);
			picScroll.TabIndex = 1;
			picScroll.TabStop = false;
			// 
			// timer1
			// 
			timer1.Tick += timer1_Tick;
			// 
			// openFileDialog1
			// 
			openFileDialog1.FileName = "openFileDialog1";
			// 
			// Main
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(367, 347);
			Controls.Add(splitContainer1);
			Controls.Add(toolStrip);
			Name = "Main";
			Text = "SpectrumAnalyzer";
			FormClosing += Main_FormClosing;
			toolStrip.ResumeLayout(false);
			toolStrip.PerformLayout();
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)picChart).EndInit();
			((System.ComponentModel.ISupportInitialize)picScroll).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private ToolStrip toolStrip;
		private ToolStripButton tsbOpen;
		private ToolStripButton tsbPrev;
		private ToolStripButton tsbRew;
		private ToolStripButton tsbPlay;
		private ToolStripButton tsbNext;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripButton tsbRec;
		private ToolStripButton tsbSettings;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripSeparator toolStripSeparator4;
		private SplitContainer splitContainer1;
		private PictureBox picChart;
		private PictureBox picScroll;
		private System.Windows.Forms.Timer timer1;
		private ToolStripProgressBar progressBar;
		private OpenFileDialog openFileDialog1;
	}
}
