namespace ElectronicBoard
{
    partial class PrevWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new ElectronicBoard.InterpolatedPictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.Overray = new ElectronicBoard.InterpolatedPictureBox();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Overray)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.Default;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(284, 262);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Overray
            // 
            this.Overray.BackColor = System.Drawing.Color.Transparent;
            this.Overray.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.Overray.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Overray.Interpolation = System.Drawing.Drawing2D.InterpolationMode.Default;
            this.Overray.Location = new System.Drawing.Point(0, 0);
            this.Overray.Name = "Overray";
            this.Overray.Size = new System.Drawing.Size(284, 262);
            this.Overray.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.Overray.TabIndex = 2;
            this.Overray.TabStop = false;
            // 
            // timer2
            // 
            this.timer2.Enabled = true;
            this.timer2.Interval = 10;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // PrevWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.Overray);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "PrevWindow";
            this.Text = "ElectronicBoard - PrevWindow -";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PrevWindow_FormClosing);
            this.Load += new System.EventHandler(this.PrevWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Overray)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public InterpolatedPictureBox pictureBox1;
        private System.Windows.Forms.Timer timer1;
        private InterpolatedPictureBox Overray;
        private System.Windows.Forms.Timer timer2;



    }
}