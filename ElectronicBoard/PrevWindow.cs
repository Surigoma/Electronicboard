using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Drawing2D;


namespace ElectronicBoard
{
    public partial class PrevWindow : Form
    {
        int px = 16, py = 16;
        float alpha = 1.0f;
        List<long> log = new List<long>();
        float SendingFPS = 0.0f;
        bool Sending = false;
        public bool Updated = false;
        Stopwatch sw = new Stopwatch();
        public PrevWindow()
        {
            InitializeComponent();
            sw.Start();
            DrawOverray();
            timer2.Start();
        }
        public void UpdateSendingData(bool flag, float Data)
        {
            Sending = flag;
            SendingFPS = Data;
        }
        public bool GetSendingStatus() { return Sending; }
        public bool IsUpdated()
        {
            bool result = Updated;
            Updated = false;
            return result;
        }
        public void DrawOverray()
        {
            float a = 10.0f;
            int ai = (int)(alpha * 255.0f);
            Bitmap i = new Bitmap((int)(px * a), (int)(py * a));
            Graphics g = Graphics.FromImage(i);
            g.Clear(Color.FromArgb(0, 0, 0, 0));
            if (alpha > 0)
            {
                int x = (int)Math.Floor(px / 16.0);
                int y = (int)Math.Floor(py / 16.0);
                Pen P_Box = new Pen(Color.FromArgb(ai, Color.White), 3);
                Brush P_Num = new SolidBrush(Color.FromArgb(ai, Color.White));
                Font f = new Font("MS ゴシック", 20);
                int count = 0;
                for (int iy = 0; iy < y; iy++)
                {
                    for (int ix = 0; ix < x; ix++)
                    {
                        g.DrawRectangle(P_Box, ix * 16.0f * a, iy * 16.0f * a, (ix + 1) * 16.0f * a, (iy + 1) * 16.0f * a);
                        g.DrawString(count.ToString(), f, P_Num, (ix * 16.0f * a), (iy * 16.0f * a));
                        count++;
                    }
                }
            }
            g.Dispose();
            Overray.Image = i;
            Overray.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            Overray.BackColor = Color.Transparent;
            Overray.Parent = pictureBox1;
        }
        private void PrevWindow_Load(object sender, EventArgs e)
        {
            ChangeSize(16, 16);
        }
        public Bitmap ChangeSize(int x, int y, bool force = false)
        {
            if (px != x || py != y || force)
            {
                var old = pictureBox1.Image;
                this.Invoke((MethodInvoker)delegate
                {
                    pictureBox1.Image = new Bitmap(x, y);
                });
                if (old != null)
                    old.Dispose();
                px = x;
                py = y;
                Size s = this.SizeFromClientSize(new Size(x * 5, y * 5));
                this.Width = s.Width;
                this.Height = s.Height;//y * 5 + SystemInformation.CaptionHeight;
                alpha = 1;
                DrawOverray();
                timer2.Start();
            }
            return (Bitmap)pictureBox1.Image;
        }
        public Bitmap GetImage()
        {
            Bitmap result = null;
            Image img = null;
            this.Invoke((MethodInvoker)delegate ()
            {
                img = pictureBox1.Image;
                if (img == null)
                {
                    img = ChangeSize(px, py, true);
                }
                else if (img.Width != px || img.Height != py)
                {
                    img = ChangeSize(px, py, true);
                }
                result = (Bitmap)img.Clone();
            });
            return (Bitmap)result;
        }
        public void SetImage(Bitmap image)
        {
            if (image != null)
            {
                //Updated = true;
                //if (this.IsAccessible)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        pictureBox1.Image = image;
                        pictureBox1.Interpolation = InterpolationMode.NearestNeighbor;
                    });
                }
                log.Add(sw.ElapsedMilliseconds);
                if (log.Count > 100)
                {
                    log.RemoveRange(0, log.Count - 100);
                }
                sw.Restart();
            }
        }

        private void PrevWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (log.Count > 0)
            {
                double sum = log.Sum();
                //foreach (var i in log) { sum += i; }
                sum = 1000.0d / (sum / log.Count);
                this.Text = "[" + px + " x " + py + "]" + "[" + sum.ToString("F2") + " fps]" + (Sending ? "[" + SendingFPS.ToString("F2") + "fps]" : "");
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            alpha -= 0.01f;
            if (alpha < 0)
            {
                alpha = 0;
                DrawOverray();
                timer2.Stop();
            }
            else
            {
                DrawOverray();
            }
        }
    }

}
