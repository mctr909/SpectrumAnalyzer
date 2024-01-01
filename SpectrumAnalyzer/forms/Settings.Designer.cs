﻿namespace SpectrumAnalyzer {
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
			this.ChkPeak = new System.Windows.Forms.CheckBox();
			this.ChkScroll = new System.Windows.Forms.CheckBox();
			this.ChkThreshold = new System.Windows.Forms.CheckBox();
			this.RbNormGain = new System.Windows.Forms.RadioButton();
			this.RbGain15 = new System.Windows.Forms.RadioButton();
			this.RbGainNone = new System.Windows.Forms.RadioButton();
			this.RbAutoGain = new System.Windows.Forms.RadioButton();
			this.TrkMinLevel = new System.Windows.Forms.TrackBar();
			this.GrbOutput = new System.Windows.Forms.GroupBox();
			this.CmbOutput = new System.Windows.Forms.ComboBox();
			this.GrbInput = new System.Windows.Forms.GroupBox();
			this.CmbInput = new System.Windows.Forms.ComboBox();
			this.GrbThreshold = new System.Windows.Forms.GroupBox();
			this.TrkThresholdWidth = new System.Windows.Forms.TrackBar();
			this.ChkCurve = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
			this.GrbKey.SuspendLayout();
			this.GrbSpeed.SuspendLayout();
			this.GrbMinLevel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkMinLevel)).BeginInit();
			this.GrbOutput.SuspendLayout();
			this.GrbInput.SuspendLayout();
			this.GrbThreshold.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkThresholdWidth)).BeginInit();
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
			this.TrkSpeed.Size = new System.Drawing.Size(291, 24);
			this.TrkSpeed.TabIndex = 1;
			this.TrkSpeed.Scroll += new System.EventHandler(this.TrkSpeed_Scroll);
			// 
			// TrkKey
			// 
			this.TrkKey.AutoSize = false;
			this.TrkKey.LargeChange = 10;
			this.TrkKey.Location = new System.Drawing.Point(7, 15);
			this.TrkKey.Maximum = 120;
			this.TrkKey.Minimum = -120;
			this.TrkKey.Name = "TrkKey";
			this.TrkKey.Size = new System.Drawing.Size(291, 24);
			this.TrkKey.TabIndex = 1;
			this.TrkKey.TickFrequency = 10;
			this.TrkKey.Scroll += new System.EventHandler(this.TrkKey_Scroll);
			// 
			// GrbKey
			// 
			this.GrbKey.Controls.Add(this.TrkKey);
			this.GrbKey.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbKey.Location = new System.Drawing.Point(5, 0);
			this.GrbKey.Name = "GrbKey";
			this.GrbKey.Size = new System.Drawing.Size(303, 47);
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
			this.GrbSpeed.Size = new System.Drawing.Size(303, 47);
			this.GrbSpeed.TabIndex = 2;
			this.GrbSpeed.TabStop = false;
			this.GrbSpeed.Text = "速さ";
			// 
			// GrbMinLevel
			// 
			this.GrbMinLevel.Controls.Add(this.ChkCurve);
			this.GrbMinLevel.Controls.Add(this.ChkPeak);
			this.GrbMinLevel.Controls.Add(this.ChkScroll);
			this.GrbMinLevel.Controls.Add(this.ChkThreshold);
			this.GrbMinLevel.Controls.Add(this.RbNormGain);
			this.GrbMinLevel.Controls.Add(this.RbGain15);
			this.GrbMinLevel.Controls.Add(this.RbGainNone);
			this.GrbMinLevel.Controls.Add(this.RbAutoGain);
			this.GrbMinLevel.Controls.Add(this.TrkMinLevel);
			this.GrbMinLevel.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbMinLevel.Location = new System.Drawing.Point(5, 159);
			this.GrbMinLevel.Name = "GrbMinLevel";
			this.GrbMinLevel.Size = new System.Drawing.Size(303, 94);
			this.GrbMinLevel.TabIndex = 4;
			this.GrbMinLevel.TabStop = false;
			this.GrbMinLevel.Text = "表示";
			// 
			// ChkPeak
			// 
			this.ChkPeak.AutoSize = true;
			this.ChkPeak.Location = new System.Drawing.Point(67, 69);
			this.ChkPeak.Name = "ChkPeak";
			this.ChkPeak.Size = new System.Drawing.Size(53, 19);
			this.ChkPeak.TabIndex = 7;
			this.ChkPeak.Text = "ピーク";
			this.ChkPeak.UseVisualStyleBackColor = true;
			this.ChkPeak.CheckedChanged += new System.EventHandler(this.ChkPeak_CheckedChanged);
			// 
			// ChkScroll
			// 
			this.ChkScroll.AutoSize = true;
			this.ChkScroll.Location = new System.Drawing.Point(182, 69);
			this.ChkScroll.Name = "ChkScroll";
			this.ChkScroll.Size = new System.Drawing.Size(72, 19);
			this.ChkScroll.TabIndex = 6;
			this.ChkScroll.Text = "スクロール";
			this.ChkScroll.UseVisualStyleBackColor = true;
			this.ChkScroll.CheckedChanged += new System.EventHandler(this.ChkScroll_CheckedChanged);
			// 
			// ChkThreshold
			// 
			this.ChkThreshold.AutoSize = true;
			this.ChkThreshold.Location = new System.Drawing.Point(126, 69);
			this.ChkThreshold.Name = "ChkThreshold";
			this.ChkThreshold.Size = new System.Drawing.Size(50, 19);
			this.ChkThreshold.TabIndex = 3;
			this.ChkThreshold.Text = "閾値";
			this.ChkThreshold.UseVisualStyleBackColor = true;
			this.ChkThreshold.CheckedChanged += new System.EventHandler(this.ChkThreshold_CheckedChanged);
			// 
			// RbNormGain
			// 
			this.RbNormGain.AutoSize = true;
			this.RbNormGain.Location = new System.Drawing.Point(148, 45);
			this.RbNormGain.Name = "RbNormGain";
			this.RbNormGain.Size = new System.Drawing.Size(61, 19);
			this.RbNormGain.TabIndex = 4;
			this.RbNormGain.TabStop = true;
			this.RbNormGain.Text = "正規化";
			this.RbNormGain.UseVisualStyleBackColor = true;
			this.RbNormGain.CheckedChanged += new System.EventHandler(this.RbNormGain_CheckedChanged);
			// 
			// RbGain15
			// 
			this.RbGain15.AutoSize = true;
			this.RbGain15.Location = new System.Drawing.Point(82, 45);
			this.RbGain15.Name = "RbGain15";
			this.RbGain15.Size = new System.Drawing.Size(63, 19);
			this.RbGain15.TabIndex = 3;
			this.RbGain15.TabStop = true;
			this.RbGain15.Text = "+15db";
			this.RbGain15.UseVisualStyleBackColor = true;
			this.RbGain15.CheckedChanged += new System.EventHandler(this.RbGain15_CheckedChanged);
			// 
			// RbGainNone
			// 
			this.RbGainNone.AutoSize = true;
			this.RbGainNone.Location = new System.Drawing.Point(11, 45);
			this.RbGainNone.Name = "RbGainNone";
			this.RbGainNone.Size = new System.Drawing.Size(68, 19);
			this.RbGainNone.TabIndex = 2;
			this.RbGainNone.TabStop = true;
			this.RbGainNone.Text = "調整なし";
			this.RbGainNone.UseVisualStyleBackColor = true;
			// 
			// RbAutoGain
			// 
			this.RbAutoGain.AutoSize = true;
			this.RbAutoGain.Location = new System.Drawing.Point(209, 45);
			this.RbAutoGain.Name = "RbAutoGain";
			this.RbAutoGain.Size = new System.Drawing.Size(73, 19);
			this.RbAutoGain.TabIndex = 5;
			this.RbAutoGain.TabStop = true;
			this.RbAutoGain.Text = "自動調整";
			this.RbAutoGain.UseVisualStyleBackColor = true;
			this.RbAutoGain.CheckedChanged += new System.EventHandler(this.RbAutoGain_CheckedChanged);
			// 
			// TrkMinLevel
			// 
			this.TrkMinLevel.AutoSize = false;
			this.TrkMinLevel.LargeChange = 6;
			this.TrkMinLevel.Location = new System.Drawing.Point(6, 15);
			this.TrkMinLevel.Maximum = -20;
			this.TrkMinLevel.Minimum = -80;
			this.TrkMinLevel.Name = "TrkMinLevel";
			this.TrkMinLevel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.TrkMinLevel.Size = new System.Drawing.Size(291, 24);
			this.TrkMinLevel.TabIndex = 1;
			this.TrkMinLevel.TickFrequency = 10;
			this.TrkMinLevel.Value = -40;
			this.TrkMinLevel.Scroll += new System.EventHandler(this.TrkMinLevel_Scroll);
			// 
			// GrbOutput
			// 
			this.GrbOutput.Controls.Add(this.CmbOutput);
			this.GrbOutput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbOutput.Location = new System.Drawing.Point(5, 259);
			this.GrbOutput.Name = "GrbOutput";
			this.GrbOutput.Size = new System.Drawing.Size(303, 47);
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
			this.CmbOutput.Size = new System.Drawing.Size(291, 23);
			this.CmbOutput.TabIndex = 0;
			this.CmbOutput.SelectedIndexChanged += new System.EventHandler(this.CmbOutput_SelectedIndexChanged);
			// 
			// GrbInput
			// 
			this.GrbInput.Controls.Add(this.CmbInput);
			this.GrbInput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbInput.Location = new System.Drawing.Point(5, 312);
			this.GrbInput.Name = "GrbInput";
			this.GrbInput.Size = new System.Drawing.Size(303, 47);
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
			this.CmbInput.Size = new System.Drawing.Size(291, 23);
			this.CmbInput.TabIndex = 0;
			this.CmbInput.SelectedIndexChanged += new System.EventHandler(this.CmbInput_SelectedIndexChanged);
			// 
			// GrbThreshold
			// 
			this.GrbThreshold.Controls.Add(this.TrkThresholdWidth);
			this.GrbThreshold.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.GrbThreshold.Location = new System.Drawing.Point(5, 106);
			this.GrbThreshold.Name = "GrbThreshold";
			this.GrbThreshold.Size = new System.Drawing.Size(303, 47);
			this.GrbThreshold.TabIndex = 3;
			this.GrbThreshold.TabStop = false;
			this.GrbThreshold.Text = "閾値";
			// 
			// TrkThresholdWidth
			// 
			this.TrkThresholdWidth.AutoSize = false;
			this.TrkThresholdWidth.LargeChange = 1;
			this.TrkThresholdWidth.Location = new System.Drawing.Point(6, 15);
			this.TrkThresholdWidth.Maximum = 11;
			this.TrkThresholdWidth.Minimum = 1;
			this.TrkThresholdWidth.Name = "TrkThresholdWidth";
			this.TrkThresholdWidth.Size = new System.Drawing.Size(291, 24);
			this.TrkThresholdWidth.TabIndex = 1;
			this.TrkThresholdWidth.Value = 1;
			this.TrkThresholdWidth.Scroll += new System.EventHandler(this.TrkThresholdWidth_Scroll);
			// 
			// ChkCurve
			// 
			this.ChkCurve.AutoSize = true;
			this.ChkCurve.Location = new System.Drawing.Point(11, 69);
			this.ChkCurve.Name = "ChkCurve";
			this.ChkCurve.Size = new System.Drawing.Size(50, 19);
			this.ChkCurve.TabIndex = 8;
			this.ChkCurve.Text = "曲線";
			this.ChkCurve.UseVisualStyleBackColor = true;
			this.ChkCurve.Checked = true;
			this.ChkCurve.CheckedChanged += new System.EventHandler(this.ChkCurve_CheckedChanged);
			// 
			// Settings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(315, 366);
			this.Controls.Add(this.GrbThreshold);
			this.Controls.Add(this.GrbInput);
			this.Controls.Add(this.GrbOutput);
			this.Controls.Add(this.GrbMinLevel);
			this.Controls.Add(this.GrbSpeed);
			this.Controls.Add(this.GrbKey);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Settings";
			this.Text = "設定";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
			this.Load += new System.EventHandler(this.Settings_Load);
			((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TrkKey)).EndInit();
			this.GrbKey.ResumeLayout(false);
			this.GrbSpeed.ResumeLayout(false);
			this.GrbMinLevel.ResumeLayout(false);
			this.GrbMinLevel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrkMinLevel)).EndInit();
			this.GrbOutput.ResumeLayout(false);
			this.GrbInput.ResumeLayout(false);
			this.GrbThreshold.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.TrkThresholdWidth)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TrackBar TrkSpeed;
		private System.Windows.Forms.TrackBar TrkKey;
		private System.Windows.Forms.GroupBox GrbKey;
		private System.Windows.Forms.GroupBox GrbSpeed;
		private System.Windows.Forms.GroupBox GrbMinLevel;
		private System.Windows.Forms.TrackBar TrkMinLevel;
        private System.Windows.Forms.RadioButton RbAutoGain;
        private System.Windows.Forms.RadioButton RbGainNone;
		private System.Windows.Forms.GroupBox GrbOutput;
		private System.Windows.Forms.ComboBox CmbOutput;
		private System.Windows.Forms.GroupBox GrbInput;
		private System.Windows.Forms.ComboBox CmbInput;
		private System.Windows.Forms.RadioButton RbGain15;
		private System.Windows.Forms.GroupBox GrbThreshold;
        private System.Windows.Forms.CheckBox ChkThreshold;
        private System.Windows.Forms.TrackBar TrkThresholdWidth;
        private System.Windows.Forms.RadioButton RbNormGain;
		private System.Windows.Forms.CheckBox ChkScroll;
		private System.Windows.Forms.CheckBox ChkPeak;
		private System.Windows.Forms.CheckBox ChkCurve;
	}
}