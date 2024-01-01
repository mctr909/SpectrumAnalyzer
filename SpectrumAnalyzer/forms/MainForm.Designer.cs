namespace SpectrumAnalyzer
{
	partial class MainForm
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.TrkSeek = new System.Windows.Forms.TrackBar();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.TsbSetting = new System.Windows.Forms.ToolStripButton();
			this.TsbOpen = new System.Windows.Forms.ToolStripButton();
			this.TsbRec = new System.Windows.Forms.ToolStripButton();
			this.TsbPlay = new System.Windows.Forms.ToolStripButton();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.TrkSeek)).BeginInit();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// TrkSeek
			// 
			this.TrkSeek.AutoSize = false;
			this.TrkSeek.Location = new System.Drawing.Point(106, 28);
			this.TrkSeek.Name = "TrkSeek";
			this.TrkSeek.Size = new System.Drawing.Size(131, 24);
			this.TrkSeek.TabIndex = 2;
			this.TrkSeek.TickFrequency = 15;
			this.TrkSeek.Minimum = 0;
			this.TrkSeek.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TrkSeek_KeyDown);
			this.TrkSeek.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TrkSeek_KeyUp);
			this.TrkSeek.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TrkSeek_MouseDown);
			this.TrkSeek.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TrkSeek_MouseUp);
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TsbSetting,
            this.TsbOpen,
            this.TsbRec,
            this.TsbPlay});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(372, 25);
			this.toolStrip1.TabIndex = 10;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// TsbSetting
			// 
			this.TsbSetting.AutoSize = false;
			this.TsbSetting.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.TsbSetting.Image = global::SpectrumAnalyzer.Properties.Resources.setting;
			this.TsbSetting.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TsbSetting.Name = "TsbSetting";
			this.TsbSetting.Size = new System.Drawing.Size(23, 22);
			this.TsbSetting.Text = "設定";
			this.TsbSetting.Click += new System.EventHandler(this.TsbSetting_Click);
			// 
			// TsbOpen
			// 
			this.TsbOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.TsbOpen.Image = global::SpectrumAnalyzer.Properties.Resources.file;
			this.TsbOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TsbOpen.Name = "TsbOpen";
			this.TsbOpen.Size = new System.Drawing.Size(23, 22);
			this.TsbOpen.Text = "再生ファイルを開く";
			this.TsbOpen.Click += new System.EventHandler(this.TsbOpen_Click);
			// 
			// TsbRec
			// 
			this.TsbRec.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.TsbRec.Image = global::SpectrumAnalyzer.Properties.Resources.rec;
			this.TsbRec.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TsbRec.Name = "TsbRec";
			this.TsbRec.Size = new System.Drawing.Size(23, 22);
			this.TsbRec.Text = "録音";
			this.TsbRec.Click += new System.EventHandler(this.TsbRec_Click);
			// 
			// TsbPlay
			// 
			this.TsbPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.TsbPlay.Image = global::SpectrumAnalyzer.Properties.Resources.play;
			this.TsbPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TsbPlay.Name = "TsbPlay";
			this.TsbPlay.Size = new System.Drawing.Size(23, 22);
			this.TsbPlay.Text = "再生";
			this.TsbPlay.Click += new System.EventHandler(this.TsbPlay_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new System.Drawing.Point(13, 60);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(224, 138);
			this.pictureBox1.TabIndex = 6;
			this.pictureBox1.TabStop = false;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(372, 211);
			this.Controls.Add(this.TrkSeek);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.pictureBox1);
			this.MinimumSize = new System.Drawing.Size(256, 168);
			this.Name = "MainForm";
			this.Text = "SpectrumAnalyzer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.Form1_Load);
			this.Resize += new System.EventHandler(this.Form1_Resize);
			((System.ComponentModel.ISupportInitialize)(this.TrkSeek)).EndInit();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.TrackBar TrkSeek;
		private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton TsbOpen;
		private System.Windows.Forms.ToolStripButton TsbPlay;
		private System.Windows.Forms.ToolStripButton TsbRec;
		private System.Windows.Forms.ToolStripButton TsbSetting;
	}
}

