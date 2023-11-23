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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.TrkSeek = new System.Windows.Forms.TrackBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.TrkKey = new System.Windows.Forms.TrackBar();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.TrkSpeed = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.TrkSeek)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "開く";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(4, 33);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "再生";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // TrkSeek
            // 
            this.TrkSeek.AutoSize = false;
            this.TrkSeek.Location = new System.Drawing.Point(93, 12);
            this.TrkSeek.Name = "TrkSeek";
            this.TrkSeek.Size = new System.Drawing.Size(265, 30);
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
            this.TrkKey.LargeChange = 1;
            this.TrkKey.Location = new System.Drawing.Point(93, 41);
            this.TrkKey.Maximum = 12;
            this.TrkKey.Minimum = -12;
            this.TrkKey.Name = "TrkKey";
            this.TrkKey.Size = new System.Drawing.Size(117, 30);
            this.TrkKey.TabIndex = 5;
            this.TrkKey.Scroll += new System.EventHandler(this.TrkKey_Scroll);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(12, 92);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(346, 294);
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // TrkSpeed
            // 
            this.TrkSpeed.AutoSize = false;
            this.TrkSpeed.LargeChange = 6;
            this.TrkSpeed.Location = new System.Drawing.Point(241, 41);
            this.TrkSpeed.Maximum = 12;
            this.TrkSpeed.Minimum = -12;
            this.TrkSpeed.Name = "TrkSpeed";
            this.TrkSpeed.Size = new System.Drawing.Size(117, 30);
            this.TrkSpeed.TabIndex = 7;
            this.TrkSpeed.TickFrequency = 3;
            this.TrkSpeed.Scroll += new System.EventHandler(this.TrkSpeed_Scroll);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(390, 403);
            this.Controls.Add(this.TrkSpeed);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.TrkKey);
            this.Controls.Add(this.TrkSeek);
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

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.TrackBar TrkSeek;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TrackBar TrkKey;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TrackBar TrkSpeed;
    }
}

