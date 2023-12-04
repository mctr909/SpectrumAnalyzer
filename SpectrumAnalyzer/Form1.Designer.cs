namespace SpectrumAnalyzer
{
	partial class Form1
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
			this.BtnFileOpen = new System.Windows.Forms.Button();
			this.BtnPlayStop = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.TrkSeek = new System.Windows.Forms.TrackBar();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.TrkKey = new System.Windows.Forms.TrackBar();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.TrkSpeed = new System.Windows.Forms.TrackBar();
			this.BtnRec = new System.Windows.Forms.Button();
			this.BtnSetting = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.TrkSeek)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
			this.SuspendLayout();
			// 
			// BtnFileOpen
			// 
			this.BtnFileOpen.Location = new System.Drawing.Point(4, 4);
			this.BtnFileOpen.Name = "BtnFileOpen";
			this.BtnFileOpen.Size = new System.Drawing.Size(40, 23);
			this.BtnFileOpen.TabIndex = 0;
			this.BtnFileOpen.Text = "開く";
			this.BtnFileOpen.UseVisualStyleBackColor = true;
			this.BtnFileOpen.Click += new System.EventHandler(this.BtnFileOpen_Click);
			// 
			// BtnPlayStop
			// 
			this.BtnPlayStop.Location = new System.Drawing.Point(4, 28);
			this.BtnPlayStop.Name = "BtnPlayStop";
			this.BtnPlayStop.Size = new System.Drawing.Size(40, 23);
			this.BtnPlayStop.TabIndex = 1;
			this.BtnPlayStop.Text = "再生";
			this.BtnPlayStop.UseVisualStyleBackColor = true;
			this.BtnPlayStop.Click += new System.EventHandler(this.BtnPlayStop_Click);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// TrkSeek
			// 
			this.TrkSeek.AutoSize = false;
			this.TrkSeek.Location = new System.Drawing.Point(93, 4);
			this.TrkSeek.Name = "TrkSeek";
			this.TrkSeek.Size = new System.Drawing.Size(265, 26);
			this.TrkSeek.TabIndex = 2;
			this.TrkSeek.TickFrequency = 15;
			this.TrkSeek.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TrkSeek_MouseDown);
			this.TrkSeek.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TrkSeek_MouseUp);
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// TrkKey
			// 
			this.TrkKey.AutoSize = false;
			this.TrkKey.LargeChange = 10;
			this.TrkKey.Location = new System.Drawing.Point(93, 36);
			this.TrkKey.Maximum = 120;
			this.TrkKey.Minimum = -120;
			this.TrkKey.Name = "TrkKey";
			this.TrkKey.Size = new System.Drawing.Size(125, 26);
			this.TrkKey.TabIndex = 5;
			this.TrkKey.TickFrequency = 10;
			this.TrkKey.Scroll += new System.EventHandler(this.TrkKey_Scroll);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new System.Drawing.Point(12, 68);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(346, 294);
			this.pictureBox1.TabIndex = 6;
			this.pictureBox1.TabStop = false;
			// 
			// TrkSpeed
			// 
			this.TrkSpeed.AutoSize = false;
			this.TrkSpeed.LargeChange = 6;
			this.TrkSpeed.Location = new System.Drawing.Point(224, 36);
			this.TrkSpeed.Maximum = 12;
			this.TrkSpeed.Minimum = -12;
			this.TrkSpeed.Name = "TrkSpeed";
			this.TrkSpeed.Size = new System.Drawing.Size(134, 26);
			this.TrkSpeed.TabIndex = 7;
			this.TrkSpeed.TickFrequency = 3;
			this.TrkSpeed.Scroll += new System.EventHandler(this.TrkSpeed_Scroll);
			// 
			// BtnRec
			// 
			this.BtnRec.Location = new System.Drawing.Point(47, 28);
			this.BtnRec.Name = "BtnRec";
			this.BtnRec.Size = new System.Drawing.Size(40, 23);
			this.BtnRec.TabIndex = 8;
			this.BtnRec.Text = "録音";
			this.BtnRec.UseVisualStyleBackColor = true;
			this.BtnRec.Click += new System.EventHandler(this.BtnRec_Click);
			// 
			// BtnSetting
			// 
			this.BtnSetting.Location = new System.Drawing.Point(47, 4);
			this.BtnSetting.Name = "BtnSetting";
			this.BtnSetting.Size = new System.Drawing.Size(40, 23);
			this.BtnSetting.TabIndex = 9;
			this.BtnSetting.Text = "設定";
			this.BtnSetting.UseVisualStyleBackColor = true;
			this.BtnSetting.Click += new System.EventHandler(this.BtnSetting_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(371, 375);
			this.Controls.Add(this.BtnSetting);
			this.Controls.Add(this.BtnRec);
			this.Controls.Add(this.TrkSpeed);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.BtnPlayStop);
			this.Controls.Add(this.BtnFileOpen);
			this.Controls.Add(this.TrkKey);
			this.Controls.Add(this.TrkSeek);
			this.MinimumSize = new System.Drawing.Size(192, 192);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.Resize += new System.EventHandler(this.Form1_Resize);
			((System.ComponentModel.ISupportInitialize)(this.TrkSeek)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button BtnFileOpen;
		private System.Windows.Forms.Button BtnPlayStop;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.TrackBar TrkSeek;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TrackBar TrkKey;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TrackBar TrkSpeed;
		private System.Windows.Forms.Button BtnRec;
		private System.Windows.Forms.Button BtnSetting;
	}
}

