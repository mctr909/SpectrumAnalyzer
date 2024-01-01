namespace SpectrumAnalyzer.Forms {
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
			this.GrbDisplaySettings = new System.Windows.Forms.GroupBox();
			this.TrkDispMax = new System.Windows.Forms.TrackBar();
			this.RbNormGain = new System.Windows.Forms.RadioButton();
			this.RbGainNone = new System.Windows.Forms.RadioButton();
			this.RbAutoGain = new System.Windows.Forms.RadioButton();
			this.TrkDispRange = new System.Windows.Forms.TrackBar();
			this.TrkScrollSpeed = new System.Windows.Forms.TrackBar();
			this.ChkThreshold = new System.Windows.Forms.CheckBox();
			this.ChkFreq = new System.Windows.Forms.CheckBox();
			this.ChkCurve = new System.Windows.Forms.CheckBox();
			this.ChkPeak = new System.Windows.Forms.CheckBox();
			this.ChkScroll = new System.Windows.Forms.CheckBox();
			this.GrbOutput = new System.Windows.Forms.GroupBox();
			this.CmbOutput = new System.Windows.Forms.ComboBox();
			this.GrbInput = new System.Windows.Forms.GroupBox();
			this.CmbInput = new System.Windows.Forms.ComboBox();
			this.GrbDisplay = new System.Windows.Forms.GroupBox();
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
			this.GrbKey.SuspendLayout();
			this.GrbSpeed.SuspendLayout();
			this.GrbDisplaySettings.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkDispMax)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkDispRange)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkScrollSpeed)).BeginInit();
			this.GrbOutput.SuspendLayout();
			this.GrbInput.SuspendLayout();
			this.GrbDisplay.SuspendLayout();
			this.SuspendLayout();
			// 
			// TrkSpeed
			// 
			this.TrkSpeed.AutoSize = false;
			this.TrkSpeed.LargeChange = 6;
			this.TrkSpeed.Location = new System.Drawing.Point(6, 15);
			this.TrkSpeed.Maximum = 12;
			this.TrkSpeed.Minimum = -24;
			this.TrkSpeed.Name = "TrkSpeed";
			this.TrkSpeed.Size = new System.Drawing.Size(337, 24);
			this.TrkSpeed.TabIndex = 1;
			this.TrkSpeed.Scroll += new System.EventHandler(this.TrkSpeed_Scroll);
			// 
			// TrkKey
			// 
			this.TrkKey.AutoSize = false;
			this.TrkKey.LargeChange = 1;
			this.TrkKey.Location = new System.Drawing.Point(7, 15);
			this.TrkKey.Maximum = 12;
			this.TrkKey.Minimum = -12;
			this.TrkKey.Name = "TrkKey";
			this.TrkKey.Size = new System.Drawing.Size(336, 24);
			this.TrkKey.TabIndex = 1;
			this.TrkKey.Scroll += new System.EventHandler(this.TrkKey_Scroll);
			// 
			// GrbKey
			// 
			this.GrbKey.Controls.Add(this.TrkKey);
			this.GrbKey.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbKey.Location = new System.Drawing.Point(5, 0);
			this.GrbKey.Name = "GrbKey";
			this.GrbKey.Size = new System.Drawing.Size(349, 47);
			this.GrbKey.TabIndex = 1;
			this.GrbKey.TabStop = false;
			this.GrbKey.Text = "キー";
			// 
			// GrbSpeed
			// 
			this.GrbSpeed.Controls.Add(this.TrkSpeed);
			this.GrbSpeed.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbSpeed.Location = new System.Drawing.Point(5, 53);
			this.GrbSpeed.Name = "GrbSpeed";
			this.GrbSpeed.Size = new System.Drawing.Size(349, 47);
			this.GrbSpeed.TabIndex = 2;
			this.GrbSpeed.TabStop = false;
			this.GrbSpeed.Text = "速さ";
			// 
			// GrbDisplaySettings
			// 
			this.GrbDisplaySettings.Controls.Add(this.TrkDispMax);
			this.GrbDisplaySettings.Controls.Add(this.RbNormGain);
			this.GrbDisplaySettings.Controls.Add(this.RbGainNone);
			this.GrbDisplaySettings.Controls.Add(this.RbAutoGain);
			this.GrbDisplaySettings.Controls.Add(this.TrkDispRange);
			this.GrbDisplaySettings.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbDisplaySettings.Location = new System.Drawing.Point(5, 106);
			this.GrbDisplaySettings.Name = "GrbDisplaySettings";
			this.GrbDisplaySettings.Size = new System.Drawing.Size(349, 138);
			this.GrbDisplaySettings.TabIndex = 4;
			this.GrbDisplaySettings.TabStop = false;
			this.GrbDisplaySettings.Text = "表示";
			// 
			// TrkDispMax
			// 
			this.TrkDispMax.AutoSize = false;
			this.TrkDispMax.LargeChange = 6;
			this.TrkDispMax.Location = new System.Drawing.Point(6, 63);
			this.TrkDispMax.Maximum = 48;
			this.TrkDispMax.Name = "TrkDispMax";
			this.TrkDispMax.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.TrkDispMax.Size = new System.Drawing.Size(337, 42);
			this.TrkDispMax.TabIndex = 9;
			this.TrkDispMax.TickFrequency = 6;
			this.TrkDispMax.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.TrkDispMax.Scroll += new System.EventHandler(this.TrkDispMax_Scroll);
			// 
			// RbNormGain
			// 
			this.RbNormGain.AutoSize = true;
			this.RbNormGain.Location = new System.Drawing.Point(126, 111);
			this.RbNormGain.Name = "RbNormGain";
			this.RbNormGain.Size = new System.Drawing.Size(61, 19);
			this.RbNormGain.TabIndex = 4;
			this.RbNormGain.TabStop = true;
			this.RbNormGain.Text = "正規化";
			this.RbNormGain.UseVisualStyleBackColor = true;
			this.RbNormGain.CheckedChanged += new System.EventHandler(this.RbNormGain_CheckedChanged);
			// 
			// RbGainNone
			// 
			this.RbGainNone.AutoSize = true;
			this.RbGainNone.Location = new System.Drawing.Point(214, 111);
			this.RbGainNone.Name = "RbGainNone";
			this.RbGainNone.Size = new System.Drawing.Size(128, 19);
			this.RbGainNone.TabIndex = 2;
			this.RbGainNone.TabStop = true;
			this.RbGainNone.Text = "最大値指定(-24db)";
			this.RbGainNone.UseVisualStyleBackColor = true;
			this.RbGainNone.CheckedChanged += new System.EventHandler(this.RbGainNone_CheckedChanged);
			// 
			// RbAutoGain
			// 
			this.RbAutoGain.AutoSize = true;
			this.RbAutoGain.Location = new System.Drawing.Point(14, 111);
			this.RbAutoGain.Name = "RbAutoGain";
			this.RbAutoGain.Size = new System.Drawing.Size(73, 19);
			this.RbAutoGain.TabIndex = 5;
			this.RbAutoGain.TabStop = true;
			this.RbAutoGain.Text = "自動調整";
			this.RbAutoGain.UseVisualStyleBackColor = true;
			this.RbAutoGain.CheckedChanged += new System.EventHandler(this.RbAutoGain_CheckedChanged);
			// 
			// TrkDispRange
			// 
			this.TrkDispRange.AutoSize = false;
			this.TrkDispRange.LargeChange = 6;
			this.TrkDispRange.Location = new System.Drawing.Point(6, 15);
			this.TrkDispRange.Maximum = -6;
			this.TrkDispRange.Minimum = -60;
			this.TrkDispRange.Name = "TrkDispRange";
			this.TrkDispRange.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.TrkDispRange.Size = new System.Drawing.Size(337, 42);
			this.TrkDispRange.TabIndex = 1;
			this.TrkDispRange.TickFrequency = 6;
			this.TrkDispRange.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.TrkDispRange.Value = -36;
			this.TrkDispRange.Scroll += new System.EventHandler(this.TrkDispRange_Scroll);
			// 
			// TrkScrollSpeed
			// 
			this.TrkScrollSpeed.AutoSize = false;
			this.TrkScrollSpeed.LargeChange = 1;
			this.TrkScrollSpeed.Location = new System.Drawing.Point(6, 40);
			this.TrkScrollSpeed.Maximum = 8;
			this.TrkScrollSpeed.Minimum = 1;
			this.TrkScrollSpeed.Name = "TrkScrollSpeed";
			this.TrkScrollSpeed.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.TrkScrollSpeed.Size = new System.Drawing.Size(336, 42);
			this.TrkScrollSpeed.TabIndex = 12;
			this.TrkScrollSpeed.TickStyle = System.Windows.Forms.TickStyle.Both;
			this.TrkScrollSpeed.Value = 1;
			this.TrkScrollSpeed.Scroll += new System.EventHandler(this.TrkScrollSpeed_Scroll);
			// 
			// ChkThreshold
			// 
			this.ChkThreshold.AutoSize = true;
			this.ChkThreshold.BackColor = System.Drawing.Color.Black;
			this.ChkThreshold.ForeColor = System.Drawing.Color.Red;
			this.ChkThreshold.Location = new System.Drawing.Point(123, 18);
			this.ChkThreshold.Name = "ChkThreshold";
			this.ChkThreshold.Size = new System.Drawing.Size(50, 19);
			this.ChkThreshold.TabIndex = 11;
			this.ChkThreshold.Text = "閾値";
			this.ChkThreshold.UseVisualStyleBackColor = false;
			this.ChkThreshold.CheckedChanged += new System.EventHandler(this.ChkThreshold_CheckedChanged);
			// 
			// ChkFreq
			// 
			this.ChkFreq.AutoSize = true;
			this.ChkFreq.Location = new System.Drawing.Point(179, 18);
			this.ChkFreq.Name = "ChkFreq";
			this.ChkFreq.Size = new System.Drawing.Size(86, 19);
			this.ChkFreq.TabIndex = 10;
			this.ChkFreq.Text = "周波数表示";
			this.ChkFreq.UseVisualStyleBackColor = true;
			this.ChkFreq.CheckedChanged += new System.EventHandler(this.ChkFreq_CheckedChanged);
			// 
			// ChkCurve
			// 
			this.ChkCurve.AutoSize = true;
			this.ChkCurve.BackColor = System.Drawing.Color.Black;
			this.ChkCurve.Checked = true;
			this.ChkCurve.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ChkCurve.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
			this.ChkCurve.Location = new System.Drawing.Point(8, 18);
			this.ChkCurve.Name = "ChkCurve";
			this.ChkCurve.Size = new System.Drawing.Size(50, 19);
			this.ChkCurve.TabIndex = 8;
			this.ChkCurve.Text = "曲線";
			this.ChkCurve.UseVisualStyleBackColor = false;
			this.ChkCurve.CheckedChanged += new System.EventHandler(this.ChkCurve_CheckedChanged);
			// 
			// ChkPeak
			// 
			this.ChkPeak.AutoSize = true;
			this.ChkPeak.BackColor = System.Drawing.Color.Black;
			this.ChkPeak.ForeColor = System.Drawing.Color.Cyan;
			this.ChkPeak.Location = new System.Drawing.Point(64, 18);
			this.ChkPeak.Name = "ChkPeak";
			this.ChkPeak.Size = new System.Drawing.Size(53, 19);
			this.ChkPeak.TabIndex = 7;
			this.ChkPeak.Text = "ピーク";
			this.ChkPeak.UseVisualStyleBackColor = false;
			this.ChkPeak.CheckedChanged += new System.EventHandler(this.ChkPeak_CheckedChanged);
			// 
			// ChkScroll
			// 
			this.ChkScroll.AutoSize = true;
			this.ChkScroll.Location = new System.Drawing.Point(271, 18);
			this.ChkScroll.Name = "ChkScroll";
			this.ChkScroll.Size = new System.Drawing.Size(72, 19);
			this.ChkScroll.TabIndex = 6;
			this.ChkScroll.Text = "スクロール";
			this.ChkScroll.UseVisualStyleBackColor = true;
			this.ChkScroll.CheckedChanged += new System.EventHandler(this.ChkScroll_CheckedChanged);
			// 
			// GrbOutput
			// 
			this.GrbOutput.Controls.Add(this.CmbOutput);
			this.GrbOutput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbOutput.Location = new System.Drawing.Point(5, 345);
			this.GrbOutput.Name = "GrbOutput";
			this.GrbOutput.Size = new System.Drawing.Size(349, 47);
			this.GrbOutput.TabIndex = 5;
			this.GrbOutput.TabStop = false;
			this.GrbOutput.Text = "出力デバイス";
			// 
			// CmbOutput
			// 
			this.CmbOutput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.CmbOutput.FormattingEnabled = true;
			this.CmbOutput.Location = new System.Drawing.Point(6, 18);
			this.CmbOutput.Name = "CmbOutput";
			this.CmbOutput.Size = new System.Drawing.Size(337, 23);
			this.CmbOutput.TabIndex = 0;
			// 
			// GrbInput
			// 
			this.GrbInput.Controls.Add(this.CmbInput);
			this.GrbInput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbInput.Location = new System.Drawing.Point(5, 398);
			this.GrbInput.Name = "GrbInput";
			this.GrbInput.Size = new System.Drawing.Size(349, 47);
			this.GrbInput.TabIndex = 6;
			this.GrbInput.TabStop = false;
			this.GrbInput.Text = "入力デバイス";
			// 
			// CmbInput
			// 
			this.CmbInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.CmbInput.FormattingEnabled = true;
			this.CmbInput.Location = new System.Drawing.Point(6, 18);
			this.CmbInput.Name = "CmbInput";
			this.CmbInput.Size = new System.Drawing.Size(337, 23);
			this.CmbInput.TabIndex = 0;
			// 
			// GrbDisplay
			// 
			this.GrbDisplay.Controls.Add(this.TrkScrollSpeed);
			this.GrbDisplay.Controls.Add(this.ChkFreq);
			this.GrbDisplay.Controls.Add(this.ChkThreshold);
			this.GrbDisplay.Controls.Add(this.ChkCurve);
			this.GrbDisplay.Controls.Add(this.ChkPeak);
			this.GrbDisplay.Controls.Add(this.ChkScroll);
			this.GrbDisplay.Font = new System.Drawing.Font("Meiryo UI", 9F);
			this.GrbDisplay.Location = new System.Drawing.Point(5, 250);
			this.GrbDisplay.Name = "GrbDisplay";
			this.GrbDisplay.Size = new System.Drawing.Size(349, 89);
			this.GrbDisplay.TabIndex = 13;
			this.GrbDisplay.TabStop = false;
			this.GrbDisplay.Text = "表示内容";
			// 
			// Settings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(360, 451);
			this.Controls.Add(this.GrbDisplay);
			this.Controls.Add(this.GrbInput);
			this.Controls.Add(this.GrbOutput);
			this.Controls.Add(this.GrbDisplaySettings);
			this.Controls.Add(this.GrbSpeed);
			this.Controls.Add(this.GrbKey);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Settings";
			this.Text = "設定";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).EndInit();
			this.GrbKey.ResumeLayout(false);
			this.GrbSpeed.ResumeLayout(false);
			this.GrbDisplaySettings.ResumeLayout(false);
			this.GrbDisplaySettings.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkDispMax)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkDispRange)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkScrollSpeed)).EndInit();
			this.GrbOutput.ResumeLayout(false);
			this.GrbInput.ResumeLayout(false);
			this.GrbDisplay.ResumeLayout(false);
			this.GrbDisplay.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TrackBar TrkSpeed;
		private System.Windows.Forms.TrackBar TrkKey;
		private System.Windows.Forms.GroupBox GrbKey;
		private System.Windows.Forms.GroupBox GrbSpeed;
		private System.Windows.Forms.GroupBox GrbDisplaySettings;
		private System.Windows.Forms.TrackBar TrkDispRange;
        private System.Windows.Forms.RadioButton RbAutoGain;
        private System.Windows.Forms.RadioButton RbGainNone;
		private System.Windows.Forms.GroupBox GrbOutput;
		private System.Windows.Forms.ComboBox CmbOutput;
		private System.Windows.Forms.GroupBox GrbInput;
		private System.Windows.Forms.ComboBox CmbInput;
        private System.Windows.Forms.RadioButton RbNormGain;
		private System.Windows.Forms.CheckBox ChkScroll;
		private System.Windows.Forms.CheckBox ChkPeak;
		private System.Windows.Forms.CheckBox ChkCurve;
		private System.Windows.Forms.TrackBar TrkDispMax;
		private System.Windows.Forms.CheckBox ChkFreq;
		private System.Windows.Forms.CheckBox ChkThreshold;
		private System.Windows.Forms.TrackBar TrkScrollSpeed;
		private System.Windows.Forms.GroupBox GrbDisplay;
	}
}