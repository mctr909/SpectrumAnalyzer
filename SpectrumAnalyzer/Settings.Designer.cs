namespace SpectrumAnalyzer {
	partial class Settings {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.TrkSpeed = new System.Windows.Forms.TrackBar();
			this.TrkKey = new System.Windows.Forms.TrackBar();
			this.GrbKey = new System.Windows.Forms.GroupBox();
			this.GrbSpeed = new System.Windows.Forms.GroupBox();
			this.GrbMinLevel = new System.Windows.Forms.GroupBox();
			this.TrkMinLevel = new System.Windows.Forms.TrackBar();
			this.ChkAutoGain = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
			this.GrbKey.SuspendLayout();
			this.GrbSpeed.SuspendLayout();
			this.GrbMinLevel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkMinLevel)).BeginInit();
			this.SuspendLayout();
			// 
			// TrkSpeed
			// 
			this.TrkSpeed.AutoSize = false;
			this.TrkSpeed.LargeChange = 6;
			this.TrkSpeed.Location = new System.Drawing.Point(6, 15);
			this.TrkSpeed.Maximum = 12;
			this.TrkSpeed.Minimum = -12;
			this.TrkSpeed.Name = "TrkSpeed";
			this.TrkSpeed.Size = new System.Drawing.Size(276, 26);
			this.TrkSpeed.TabIndex = 9;
			this.TrkSpeed.TickFrequency = 3;
			this.TrkSpeed.Scroll += new System.EventHandler(this.TrkSpeed_Scroll);
			// 
			// TrkKey
			// 
			this.TrkKey.AutoSize = false;
			this.TrkKey.LargeChange = 10;
			this.TrkKey.Location = new System.Drawing.Point(6, 15);
			this.TrkKey.Maximum = 120;
			this.TrkKey.Minimum = -120;
			this.TrkKey.Name = "TrkKey";
			this.TrkKey.Size = new System.Drawing.Size(276, 26);
			this.TrkKey.TabIndex = 8;
			this.TrkKey.TickFrequency = 10;
			this.TrkKey.Scroll += new System.EventHandler(this.TrkKey_Scroll);
			// 
			// GrbKey
			// 
			this.GrbKey.Controls.Add(this.TrkKey);
			this.GrbKey.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbKey.Location = new System.Drawing.Point(6, 2);
			this.GrbKey.Name = "GrbKey";
			this.GrbKey.Size = new System.Drawing.Size(288, 47);
			this.GrbKey.TabIndex = 10;
			this.GrbKey.TabStop = false;
			this.GrbKey.Text = "キー";
			// 
			// GrbSpeed
			// 
			this.GrbSpeed.Controls.Add(this.TrkSpeed);
			this.GrbSpeed.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbSpeed.Location = new System.Drawing.Point(6, 55);
			this.GrbSpeed.Name = "GrbSpeed";
			this.GrbSpeed.Size = new System.Drawing.Size(288, 47);
			this.GrbSpeed.TabIndex = 11;
			this.GrbSpeed.TabStop = false;
			this.GrbSpeed.Text = "速さ";
			// 
			// GrbMinLevel
			// 
			this.GrbMinLevel.Controls.Add(this.ChkAutoGain);
			this.GrbMinLevel.Controls.Add(this.TrkMinLevel);
			this.GrbMinLevel.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbMinLevel.Location = new System.Drawing.Point(6, 108);
			this.GrbMinLevel.Name = "GrbMinLevel";
			this.GrbMinLevel.Size = new System.Drawing.Size(288, 68);
			this.GrbMinLevel.TabIndex = 12;
			this.GrbMinLevel.TabStop = false;
			this.GrbMinLevel.Text = "表示範囲";
			// 
			// TrkMinLevel
			// 
			this.TrkMinLevel.AutoSize = false;
			this.TrkMinLevel.LargeChange = 6;
			this.TrkMinLevel.Location = new System.Drawing.Point(6, 15);
			this.TrkMinLevel.Maximum = -30;
			this.TrkMinLevel.Minimum = -90;
			this.TrkMinLevel.Name = "TrkMinLevel";
			this.TrkMinLevel.Size = new System.Drawing.Size(276, 26);
			this.TrkMinLevel.TabIndex = 10;
			this.TrkMinLevel.TickFrequency = 10;
			this.TrkMinLevel.Value = -30;
			this.TrkMinLevel.Scroll += new System.EventHandler(this.TrkMinLevel_Scroll);
			// 
			// ChkAutoGain
			// 
			this.ChkAutoGain.AutoSize = true;
			this.ChkAutoGain.Location = new System.Drawing.Point(13, 47);
			this.ChkAutoGain.Name = "ChkAutoGain";
			this.ChkAutoGain.Size = new System.Drawing.Size(102, 19);
			this.ChkAutoGain.TabIndex = 11;
			this.ChkAutoGain.Text = "ゲイン自動調整";
			this.ChkAutoGain.UseVisualStyleBackColor = true;
			this.ChkAutoGain.CheckedChanged += new System.EventHandler(this.ChkAutoGain_CheckedChanged);
			// 
			// Settings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(301, 188);
			this.Controls.Add(this.GrbMinLevel);
			this.Controls.Add(this.GrbSpeed);
			this.Controls.Add(this.GrbKey);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Settings";
			this.Text = "設定";
			this.Load += new System.EventHandler(this.Settings_Load);
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).EndInit();
			this.GrbKey.ResumeLayout(false);
			this.GrbSpeed.ResumeLayout(false);
			this.GrbMinLevel.ResumeLayout(false);
			this.GrbMinLevel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkMinLevel)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TrackBar TrkSpeed;
		private System.Windows.Forms.TrackBar TrkKey;
		private System.Windows.Forms.GroupBox GrbKey;
		private System.Windows.Forms.GroupBox GrbSpeed;
		private System.Windows.Forms.GroupBox GrbMinLevel;
		private System.Windows.Forms.TrackBar TrkMinLevel;
		private System.Windows.Forms.CheckBox ChkAutoGain;
	}
}