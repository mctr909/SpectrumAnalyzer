namespace SpectrumAnalyzerNet.Forms {
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
			grpKey = new GroupBox();
			trbKey = new TrackBar();
			grpSpeed = new GroupBox();
			trbSpeed = new TrackBar();
			groupBox1 = new GroupBox();
			chkScroll = new CheckBox();
			chkPeak = new CheckBox();
			chkThreshold = new CheckBox();
			chkCurve = new CheckBox();
			grpDispRange = new GroupBox();
			trbDispRange = new TrackBar();
			groupBox2 = new GroupBox();
			rbManual = new RadioButton();
			rbNorm = new RadioButton();
			rbAuto = new RadioButton();
			trbGain = new TrackBar();
			grpKey.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)trbKey).BeginInit();
			grpSpeed.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)trbSpeed).BeginInit();
			groupBox1.SuspendLayout();
			grpDispRange.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)trbDispRange).BeginInit();
			groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)trbGain).BeginInit();
			SuspendLayout();
			// 
			// grpKey
			// 
			grpKey.Controls.Add(trbKey);
			grpKey.Font = new Font("Yu Gothic UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
			grpKey.Location = new Point(8, 8);
			grpKey.Name = "grpKey";
			grpKey.Size = new Size(374, 85);
			grpKey.TabIndex = 0;
			grpKey.TabStop = false;
			grpKey.Text = "キー：0半音";
			// 
			// trbKey
			// 
			trbKey.LargeChange = 1;
			trbKey.Location = new Point(6, 26);
			trbKey.Maximum = 12;
			trbKey.Minimum = -12;
			trbKey.Name = "trbKey";
			trbKey.Size = new Size(360, 45);
			trbKey.TabIndex = 0;
			trbKey.TickStyle = TickStyle.Both;
			trbKey.Scroll += trbKey_Scroll;
			// 
			// grpSpeed
			// 
			grpSpeed.Controls.Add(trbSpeed);
			grpSpeed.Font = new Font("Yu Gothic UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
			grpSpeed.Location = new Point(8, 99);
			grpSpeed.Name = "grpSpeed";
			grpSpeed.Size = new Size(374, 85);
			grpSpeed.TabIndex = 1;
			grpSpeed.TabStop = false;
			grpSpeed.Text = "スピード：100%";
			// 
			// trbSpeed
			// 
			trbSpeed.LargeChange = 1;
			trbSpeed.Location = new Point(6, 26);
			trbSpeed.Maximum = 12;
			trbSpeed.Minimum = -12;
			trbSpeed.Name = "trbSpeed";
			trbSpeed.Size = new Size(360, 45);
			trbSpeed.TabIndex = 0;
			trbSpeed.TickStyle = TickStyle.Both;
			trbSpeed.Scroll += trbSpeed_Scroll;
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(chkScroll);
			groupBox1.Controls.Add(chkPeak);
			groupBox1.Controls.Add(chkThreshold);
			groupBox1.Controls.Add(chkCurve);
			groupBox1.Font = new Font("Yu Gothic UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
			groupBox1.Location = new Point(8, 190);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(374, 65);
			groupBox1.TabIndex = 2;
			groupBox1.TabStop = false;
			groupBox1.Text = "表示対象";
			// 
			// chkScroll
			// 
			chkScroll.AutoSize = true;
			chkScroll.Location = new Point(283, 25);
			chkScroll.Name = "chkScroll";
			chkScroll.Size = new Size(83, 24);
			chkScroll.TabIndex = 3;
			chkScroll.Text = "スクロール";
			chkScroll.UseVisualStyleBackColor = true;
			chkScroll.CheckedChanged += DisplayTarget_CheckedChanged;
			// 
			// chkPeak
			// 
			chkPeak.AutoSize = true;
			chkPeak.Location = new Point(103, 25);
			chkPeak.Name = "chkPeak";
			chkPeak.Size = new Size(60, 24);
			chkPeak.TabIndex = 2;
			chkPeak.Text = "ピーク";
			chkPeak.UseVisualStyleBackColor = true;
			chkPeak.CheckedChanged += DisplayTarget_CheckedChanged;
			// 
			// chkThreshold
			// 
			chkThreshold.AutoSize = true;
			chkThreshold.Location = new Point(193, 25);
			chkThreshold.Name = "chkThreshold";
			chkThreshold.Size = new Size(58, 24);
			chkThreshold.TabIndex = 1;
			chkThreshold.Text = "閾値";
			chkThreshold.UseVisualStyleBackColor = true;
			chkThreshold.CheckedChanged += DisplayTarget_CheckedChanged;
			// 
			// chkCurve
			// 
			chkCurve.AutoSize = true;
			chkCurve.Location = new Point(13, 25);
			chkCurve.Name = "chkCurve";
			chkCurve.Size = new Size(58, 24);
			chkCurve.TabIndex = 0;
			chkCurve.Text = "曲線";
			chkCurve.UseVisualStyleBackColor = true;
			chkCurve.CheckedChanged += DisplayTarget_CheckedChanged;
			// 
			// grpDispRange
			// 
			grpDispRange.Controls.Add(trbDispRange);
			grpDispRange.Font = new Font("Yu Gothic UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
			grpDispRange.Location = new Point(8, 261);
			grpDispRange.Name = "grpDispRange";
			grpDispRange.Size = new Size(374, 85);
			grpDispRange.TabIndex = 3;
			grpDispRange.TabStop = false;
			grpDispRange.Text = "表示範囲：24dB";
			// 
			// trbDispRange
			// 
			trbDispRange.LargeChange = 1;
			trbDispRange.Location = new Point(6, 27);
			trbDispRange.Maximum = 48;
			trbDispRange.Minimum = 6;
			trbDispRange.Name = "trbDispRange";
			trbDispRange.Size = new Size(360, 45);
			trbDispRange.TabIndex = 1;
			trbDispRange.TickFrequency = 6;
			trbDispRange.TickStyle = TickStyle.Both;
			trbDispRange.Value = 24;
			trbDispRange.Scroll += trbDispRange_Scroll;
			// 
			// groupBox2
			// 
			groupBox2.Controls.Add(rbManual);
			groupBox2.Controls.Add(rbNorm);
			groupBox2.Controls.Add(rbAuto);
			groupBox2.Controls.Add(trbGain);
			groupBox2.Font = new Font("Yu Gothic UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 128);
			groupBox2.Location = new Point(8, 352);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(374, 111);
			groupBox2.TabIndex = 4;
			groupBox2.TabStop = false;
			groupBox2.Text = "自動ゲイン";
			// 
			// rbManual
			// 
			rbManual.AutoSize = true;
			rbManual.Location = new Point(253, 26);
			rbManual.Name = "rbManual";
			rbManual.Size = new Size(98, 24);
			rbManual.TabIndex = 4;
			rbManual.Text = "手動：6dB";
			rbManual.UseVisualStyleBackColor = true;
			rbManual.CheckedChanged += AutoGain_CheckedChanged;
			// 
			// rbNorm
			// 
			rbNorm.AutoSize = true;
			rbNorm.Location = new Point(133, 26);
			rbNorm.Name = "rbNorm";
			rbNorm.Size = new Size(72, 24);
			rbNorm.TabIndex = 3;
			rbNorm.Text = "正規化";
			rbNorm.UseVisualStyleBackColor = true;
			rbNorm.CheckedChanged += AutoGain_CheckedChanged;
			// 
			// rbAuto
			// 
			rbAuto.AutoSize = true;
			rbAuto.Checked = true;
			rbAuto.Location = new Point(13, 26);
			rbAuto.Name = "rbAuto";
			rbAuto.Size = new Size(87, 24);
			rbAuto.TabIndex = 2;
			rbAuto.TabStop = true;
			rbAuto.Text = "自動追随";
			rbAuto.UseVisualStyleBackColor = true;
			rbAuto.CheckedChanged += AutoGain_CheckedChanged;
			// 
			// trbGain
			// 
			trbGain.Enabled = false;
			trbGain.LargeChange = 3;
			trbGain.Location = new Point(6, 56);
			trbGain.Maximum = 30;
			trbGain.Name = "trbGain";
			trbGain.Size = new Size(360, 45);
			trbGain.TabIndex = 1;
			trbGain.TickFrequency = 6;
			trbGain.TickStyle = TickStyle.Both;
			trbGain.Value = 6;
			trbGain.Scroll += trbGain_Scroll;
			// 
			// Settings
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(390, 471);
			Controls.Add(groupBox2);
			Controls.Add(grpDispRange);
			Controls.Add(groupBox1);
			Controls.Add(grpSpeed);
			Controls.Add(grpKey);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Name = "Settings";
			Text = "Settings";
			grpKey.ResumeLayout(false);
			grpKey.PerformLayout();
			((System.ComponentModel.ISupportInitialize)trbKey).EndInit();
			grpSpeed.ResumeLayout(false);
			grpSpeed.PerformLayout();
			((System.ComponentModel.ISupportInitialize)trbSpeed).EndInit();
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			grpDispRange.ResumeLayout(false);
			grpDispRange.PerformLayout();
			((System.ComponentModel.ISupportInitialize)trbDispRange).EndInit();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)trbGain).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private GroupBox grpKey;
		private TrackBar trbKey;
		private GroupBox grpSpeed;
		private TrackBar trbSpeed;
		private GroupBox groupBox1;
		private CheckBox chkCurve;
		private CheckBox chkThreshold;
		private CheckBox chkPeak;
		private CheckBox chkScroll;
		private GroupBox grpDispRange;
		private TrackBar trbDispRange;
		private GroupBox groupBox2;
		private TrackBar trbGain;
		private RadioButton rbAuto;
		private RadioButton rbNorm;
		private RadioButton rbManual;
	}
}