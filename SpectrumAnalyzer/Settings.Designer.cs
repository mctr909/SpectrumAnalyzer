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
            this.RbGain15 = new System.Windows.Forms.RadioButton();
            this.RbGainNone = new System.Windows.Forms.RadioButton();
            this.RbAutoGain = new System.Windows.Forms.RadioButton();
            this.TrkMinLevel = new System.Windows.Forms.TrackBar();
            this.GrbOutput = new System.Windows.Forms.GroupBox();
            this.CmbOutput = new System.Windows.Forms.ComboBox();
            this.GrbInput = new System.Windows.Forms.GroupBox();
            this.CmbInput = new System.Windows.Forms.ComboBox();
            this.GrbThresholdHigh = new System.Windows.Forms.GroupBox();
            this.ChkThreshold = new System.Windows.Forms.CheckBox();
            this.TrkThresholdHigh = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.TrkSpeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TrkKey)).BeginInit();
            this.GrbKey.SuspendLayout();
            this.GrbSpeed.SuspendLayout();
            this.GrbMinLevel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TrkMinLevel)).BeginInit();
            this.GrbOutput.SuspendLayout();
            this.GrbInput.SuspendLayout();
            this.GrbThresholdHigh.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TrkThresholdHigh)).BeginInit();
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
            this.TrkSpeed.Size = new System.Drawing.Size(276, 24);
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
            this.TrkKey.Size = new System.Drawing.Size(276, 24);
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
            this.GrbMinLevel.Controls.Add(this.RbGain15);
            this.GrbMinLevel.Controls.Add(this.RbGainNone);
            this.GrbMinLevel.Controls.Add(this.RbAutoGain);
            this.GrbMinLevel.Controls.Add(this.TrkMinLevel);
            this.GrbMinLevel.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.GrbMinLevel.Location = new System.Drawing.Point(6, 182);
            this.GrbMinLevel.Name = "GrbMinLevel";
            this.GrbMinLevel.Size = new System.Drawing.Size(288, 68);
            this.GrbMinLevel.TabIndex = 12;
            this.GrbMinLevel.TabStop = false;
            this.GrbMinLevel.Text = "表示範囲";
            // 
            // RbGain15
            // 
            this.RbGain15.AutoSize = true;
            this.RbGain15.Location = new System.Drawing.Point(85, 45);
            this.RbGain15.Name = "RbGain15";
            this.RbGain15.Size = new System.Drawing.Size(63, 19);
            this.RbGain15.TabIndex = 15;
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
            this.RbGainNone.TabIndex = 14;
            this.RbGainNone.TabStop = true;
            this.RbGainNone.Text = "調整なし";
            this.RbGainNone.UseVisualStyleBackColor = true;
            // 
            // RbAutoGain
            // 
            this.RbAutoGain.AutoSize = true;
            this.RbAutoGain.Location = new System.Drawing.Point(154, 45);
            this.RbAutoGain.Name = "RbAutoGain";
            this.RbAutoGain.Size = new System.Drawing.Size(73, 19);
            this.RbAutoGain.TabIndex = 12;
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
            this.TrkMinLevel.Minimum = -100;
            this.TrkMinLevel.Name = "TrkMinLevel";
            this.TrkMinLevel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.TrkMinLevel.Size = new System.Drawing.Size(276, 24);
            this.TrkMinLevel.TabIndex = 10;
            this.TrkMinLevel.TickFrequency = 10;
            this.TrkMinLevel.Value = -30;
            this.TrkMinLevel.Scroll += new System.EventHandler(this.TrkMinLevel_Scroll);
            // 
            // GrbOutput
            // 
            this.GrbOutput.Controls.Add(this.CmbOutput);
            this.GrbOutput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.GrbOutput.Location = new System.Drawing.Point(6, 256);
            this.GrbOutput.Name = "GrbOutput";
            this.GrbOutput.Size = new System.Drawing.Size(288, 47);
            this.GrbOutput.TabIndex = 13;
            this.GrbOutput.TabStop = false;
            this.GrbOutput.Text = "出力デバイス";
            // 
            // CmbOutput
            // 
            this.CmbOutput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbOutput.FormattingEnabled = true;
            this.CmbOutput.Location = new System.Drawing.Point(6, 18);
            this.CmbOutput.Name = "CmbOutput";
            this.CmbOutput.Size = new System.Drawing.Size(276, 23);
            this.CmbOutput.TabIndex = 0;
            this.CmbOutput.SelectedIndexChanged += new System.EventHandler(this.CmbOutput_SelectedIndexChanged);
            // 
            // GrbInput
            // 
            this.GrbInput.Controls.Add(this.CmbInput);
            this.GrbInput.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.GrbInput.Location = new System.Drawing.Point(6, 309);
            this.GrbInput.Name = "GrbInput";
            this.GrbInput.Size = new System.Drawing.Size(288, 47);
            this.GrbInput.TabIndex = 14;
            this.GrbInput.TabStop = false;
            this.GrbInput.Text = "入力デバイス";
            // 
            // CmbInput
            // 
            this.CmbInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbInput.FormattingEnabled = true;
            this.CmbInput.Location = new System.Drawing.Point(6, 18);
            this.CmbInput.Name = "CmbInput";
            this.CmbInput.Size = new System.Drawing.Size(276, 23);
            this.CmbInput.TabIndex = 0;
            this.CmbInput.SelectedIndexChanged += new System.EventHandler(this.CmbInput_SelectedIndexChanged);
            // 
            // GrbThresholdHigh
            // 
            this.GrbThresholdHigh.Controls.Add(this.ChkThreshold);
            this.GrbThresholdHigh.Controls.Add(this.TrkThresholdHigh);
            this.GrbThresholdHigh.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.GrbThresholdHigh.Location = new System.Drawing.Point(6, 108);
            this.GrbThresholdHigh.Name = "GrbThresholdHigh";
            this.GrbThresholdHigh.Size = new System.Drawing.Size(288, 68);
            this.GrbThresholdHigh.TabIndex = 15;
            this.GrbThresholdHigh.TabStop = false;
            this.GrbThresholdHigh.Text = "閾値";
            // 
            // ChkThreshold
            // 
            this.ChkThreshold.AutoSize = true;
            this.ChkThreshold.Location = new System.Drawing.Point(11, 43);
            this.ChkThreshold.Name = "ChkThreshold";
            this.ChkThreshold.Size = new System.Drawing.Size(102, 19);
            this.ChkThreshold.TabIndex = 10;
            this.ChkThreshold.Text = "閾値を表示する";
            this.ChkThreshold.UseVisualStyleBackColor = true;
            this.ChkThreshold.CheckedChanged += new System.EventHandler(this.ChkThreshold_CheckedChanged);
            // 
            // TrkThresholdHigh
            // 
            this.TrkThresholdHigh.AutoSize = false;
            this.TrkThresholdHigh.LargeChange = 1;
            this.TrkThresholdHigh.Location = new System.Drawing.Point(6, 15);
            this.TrkThresholdHigh.Maximum = 12;
            this.TrkThresholdHigh.Minimum = 2;
            this.TrkThresholdHigh.Name = "TrkThresholdHigh";
            this.TrkThresholdHigh.Size = new System.Drawing.Size(276, 24);
            this.TrkThresholdHigh.TabIndex = 9;
            this.TrkThresholdHigh.Value = 2;
            this.TrkThresholdHigh.Scroll += new System.EventHandler(this.TrkThresholdHigh_Scroll);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(301, 362);
            this.Controls.Add(this.GrbThresholdHigh);
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
            this.GrbThresholdHigh.ResumeLayout(false);
            this.GrbThresholdHigh.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TrkThresholdHigh)).EndInit();
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
		private System.Windows.Forms.GroupBox GrbThresholdHigh;
		private System.Windows.Forms.TrackBar TrkThresholdHigh;
        private System.Windows.Forms.CheckBox ChkThreshold;
    }
}