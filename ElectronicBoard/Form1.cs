using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using DirectShowLib;
using System.IO.Ports;
using NAudio.Wave;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MicroLibrary;
using System.Drawing.Imaging;
using ElectronicBoard.Classes;
using ElectronicBoard.Classes.Plugins;

namespace ElectronicBoard
{
    public partial class MainForm : Form
    {
        public EventMaster master;
        public PrevWindow pw = new PrevWindow();
        public float M_CollectFPS = 60.0f;
        public bool M_SeekChanged = false;
        public bool P_Fill_Flag = false, P_Erase_Flag = false;
        public MainForm()
        {
            InitializeComponent();
            master = EventMaster.GetInstance();
            pw.Show();
            pw.ChangeSize((int)PanelXNum.Value * 16, (int)PanelYNum.Value * 16);
            foreach (var i in SerialPort.GetPortNames())
            {
                COMList.Items.Add(i);
            }
            Baudrate.SelectedIndex = 0;
            M_Type.SelectedIndex = 0;
            M_CopyAspect.SelectedIndex = 0;
            SoundType.SelectedIndex = 0;
            V_Type.SelectedIndex = 0;
            
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            master.Final();
            pw.Close();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            master.Init(this, pw);
            /****************Event Adding***************/

            master.EventAdd(EventMaster.PlayStyle.Scroll, new Scroll());
            master.EventAdd(EventMaster.PlayStyle.Movie, new Movie());
            master.EventAdd(EventMaster.PlayStyle.Paint, new Paint());
            master.EventAdd(EventMaster.PlayStyle.SoundEffect, new SoundEffect());
            master.EventAdd(EventMaster.PlayStyle.Beat, new Beat());
            master.EventAdd(EventMaster.PlayStyle.Test, new TestPlugin());

            /*******************************************/
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            switch (PluginsTab.SelectedIndex)
            {
                case 0:
                    master.ChangeEvent(EventMaster.PlayStyle.Scroll);
                    break;
                case 1:
                    master.ChangeEvent(EventMaster.PlayStyle.Movie);
                    break;
                case 2:
                    master.ChangeEvent(EventMaster.PlayStyle.Paint);
                    break;
                case 3:
                    master.ChangeEvent(EventMaster.PlayStyle.SoundEffect);
                    break;
                case 4:
                    master.ChangeEvent(EventMaster.PlayStyle.Beat);
                    break;
                case 5:
                    master.ChangeEvent(EventMaster.PlayStyle.Test);
                    break;
                default:
                    master.ChangeEvent(EventMaster.PlayStyle.Null);
                    break;
            }
        }

        private void M_OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "動画ファイル(*.avi;*.mpg;*.mp4;*.wmv)|*.avi;*.mpg;*.mp4;*.wmv|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 0;
            ofd.Title = "読み込む動画ファイル";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                M_Filename.Text = ofd.FileName;
            }
        }

        private void M_Stop_Click(object sender, EventArgs e)
        {
            master.About();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            master.About();
        }

        private void M_SetFPS_Click(object sender, EventArgs e)
        {
            M_CollectFPS = (float)M_FPS.Value;
        }
        protected override void WndProc(ref Message m)
        {
            if (!master.WndProc(ref m))
            {
                try
                {
                    base.WndProc(ref m);
                }
                catch (Exception) { }
            }
        }

        public void PanelAccept_Click(object sender, EventArgs e)
        {
            pw.ChangeSize((int)PanelXNum.Value * 16, (int)PanelYNum.Value * 16);
            M_CopyAuto_CheckedChanged(null, null);
            Event("PanelAccept", new int[] { pw.Width, pw.Height });
        }

        private void M_CopyAuto_CheckedChanged(object sender, EventArgs e)
        {
            M_Left.Enabled = !M_CopyAuto.Checked;
            M_Top.Enabled = !M_CopyAuto.Checked;
            M_Right.Enabled = !M_CopyAuto.Checked;
            M_Bottom.Enabled = !M_CopyAuto.Checked;
            M_CopyAspect.Enabled = M_CopyAuto.Checked;
            if (M_CopyAuto.Checked)
            {
                M_SetAspect();
            }
        }
        private void M_SetAspect()
        {
            if (master.preview == null)
                return;
            float aspect = ((Movie)master.GetEvents(EventMaster.PlayStyle.Movie)).GetAspect();
            //renderSize.Height / renderSize.Width;
            Bitmap b = master.preview.GetImage();
            int Mode = M_CopyAspect.SelectedIndex;
            if(Mode == 3 || Mode == 4)
            {
                // false = Width
                // true = Height

                bool Type = aspect >= 1.0f;
                Mode = ((Type ^ (Mode == 4)) & 1 == 1) ? 1 : 2;
            }
            switch (Mode)
            {
                case 0:
                    M_Left.Value = 0;
                    M_Top.Value = 0;
                    M_Right.Value = b.Width;
                    M_Bottom.Value = b.Height;
                    break;
                case 1:
                    {
                        float Ycenter = b.Height / 2.0f;
                        float YHsize = (b.Width * aspect) / 2.0f;
                        M_Left.Value = 0;
                        M_Right.Value = b.Width;
                        M_Top.Value = (int)(Ycenter - YHsize);
                        M_Bottom.Value = (int)(Ycenter + YHsize);
                    }
                    break;
                case 2:
                    {
                        float Xcenter = b.Width / 2.0f;
                        float XHsize = (b.Height / aspect) / 2.0f;
                        M_Left.Value = (int)(Xcenter - XHsize);
                        M_Right.Value = (int)(Xcenter + XHsize);
                        M_Top.Value = 0;
                        M_Bottom.Value = b.Height;
                    }
                    break;
            }
        }
        private void M_Seek_Scroll(object sender, EventArgs e)
        {
            M_SeekChanged = true;
        }

        private void M_CopyAspect_SelectedIndexChanged(object sender, EventArgs e)
        {
            M_SetAspect();
        }
        private void Connected_CheckedChanged(object sender, EventArgs e)
        {
            if (Connected.Checked)
            {
                Connected.Checked = master.OpenSerial(COMList.Text, int.Parse(Baudrate.Text));
            }
            else
            {
                master.CloseSerial();
            }
        }
        bool P_MouseClicked = false;
        private void P_Erase_Click(object sender, EventArgs e)
        {
            Event("P_Erase", new Paint.MouseEvent());
        }

        private void P_Fill_Click(object sender, EventArgs e)
        {
            Event("P_Fill", new Paint.MouseEvent());
        }
        private void P_Image_MouseMove(object sender, MouseEventArgs e)
        {
            Paint.MouseEvent ent = new Classes.Plugins.Paint.MouseEvent();
            ent.Clicked = P_MouseClicked;
            ent.Position = e.Location;
            Event("P_Mouse_Move", ent);
        }

        private void P_Image_MouseDown(object sender, MouseEventArgs e)
        {
            Paint.MouseEvent ent = new Classes.Plugins.Paint.MouseEvent();
            P_MouseClicked = true;
            ent.Clicked = P_MouseClicked;
            ent.Position = e.Location;
            Event("P_Mouse_Click", ent);
        }

        private void P_Image_MouseUp(object sender, MouseEventArgs e)
        {
            Paint.MouseEvent ent = new Classes.Plugins.Paint.MouseEvent();
            P_MouseClicked = false;
            ent.Clicked = P_MouseClicked;
            ent.Position = e.Location;
            Event("P_Mouse_Click", ent);
        }

        private void SoundType_SelectedIndexChanged(object sender, EventArgs e)
        {
            SoundList.Items.Clear();
            switch(SoundType.SelectedIndex)
            {
                case 0:
                    for (int i = 0; i < WaveIn.DeviceCount; i++)
                    {
                        SoundList.Items.Add((string)WaveIn.GetCapabilities(i).ProductName);
                    }
                    break;
                case 1:
                    if (AsioOut.isSupported())
                    {
                        SoundList.Items.AddRange(AsioOut.GetDriverNames());
                    }
                    break;
                default:
                    break;
            }
            if (SoundList.Items.Count > 0) { SoundList.SelectedIndex = 0; }
        }

        private void V_Time_ValueChanged(object sender, EventArgs e)
        {
            master.Event("V_Time_Changed", V_Time.Value);
        }

        private void Event(string EventName, object arg)
        {
            master.Event(EventName, arg);
        }

        /*
        const int WM_Application = 0x8000;
        const int WM_DirectShow = WM_Application + 1;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest,
             int nXDest, int nYDest, int nWidth, int nHeight,
             IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0xCC0020;
        MicroTimer timer1 = new MicroTimer();
        public float M_CollectFPS = 60.0f;
        public bool M_ChangeSeek = false;
        public bool M_ChangeVolume = false;

        PrevWindow pw = new PrevWindow();
        int Display = 8;
        Font f;
        int ScrollX = 0;
        int Mode = 0;
        bool isPlaying = false;
        bool isVolueing = false;
        int VolumeMode = 0;
        int BeatMode = 0;
        float gain = 10;
        bool Clicking = false, ColorFlag = false;
        Bitmap PaintPic;
        Point OldPoint = new Point();
        byte[, ,] DitherMap = 
                            {
                                 {
                                    {   0, 128,  32, 159 },
                                    { 191,  64, 223,  96 },
                                    {  48, 175,  16, 143 },
                                    { 239, 112, 207,  80 }

                                },
                                 {
                                     { 159,  64,  96, 128 },
                                     { 191,   0,  32, 223 },
                                     { 112, 143, 175,  80 },
                                     {  48, 239, 207,  16 }
                                },
                            };

        IntPtr renderwindow = IntPtr.Zero;
        Size renderSize = new Size(640, 480);

        IFilterGraph graph;
        IMediaControl media;
        IMediaEventEx eventEx;
        IVideoWindow window;
        IBaseFilter render;
        IGraphBuilder igb;
        IMediaPosition imp;
        IBasicAudio iba;

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(
            IntPtr hWnd, ref RECT rect
            );
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int l, int t, int r, int b)
            {
                left = l;
                top = t;
                right = r;
                bottom = b;
            }
        }
        public Bitmap CaptureControl(IntPtr ctrl)
        {
            if (ctrl == IntPtr.Zero)
            {
                return null;
            }
            RECT r = new RECT();
            GetClientRect(ctrl, ref r);
            Graphics g = Graphics.FromHwnd(ctrl);
            Bitmap img = new Bitmap(r.right - r.left, r.bottom - r.top);
            Graphics memg = Graphics.FromImage(img);
            IntPtr dc1 = g.GetHdc();
            IntPtr dc2 = memg.GetHdc();
            BitBlt(dc2, 0, 0, img.Width, img.Height, dc1, 0, 0, SRCCOPY);
            g.ReleaseHdc(dc1);
            memg.ReleaseHdc(dc2);
            memg.Dispose();
            g.Dispose();
            return img;
        }


        public Form1()
        {
            InitializeComponent();
            f = new Font("MS ゴシック", 12);
            //serialPort1.Open();
            pw.Show();
            PanelXNum.Value = Display;
            pw.ChangeSize(Display * 16, 16);
            foreach (var i in SerialPort.GetPortNames())
            {
                COMList.Items.Add(i);
            }
            Baudrate.SelectedIndex = 0;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                SoundList.Items.Add((string)WaveIn.GetCapabilities(i).ProductName);
            }
            if (SoundList.Items.Count > 0) { SoundList.SelectedIndex = 0; }
            PaintPic = new Bitmap(Display * 16, 16);
            Graphics g = Graphics.FromImage(PaintPic);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, Display * 16, 16);
            g.Dispose();
            pictureBox1.Image = PaintPic;
            timer1.MicroTimerElapsed += timer1_Tick;
            M_Type.SelectedIndex = 0;
            //comboBox3.SelectedIndex = 2;
            //comboBox4.SelectedIndex = 1;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (Mode)
            {
                case 0:
                    Scroll();
                    break;
                case 1:
                    Animation();
                    break;
                case 3:
                    UpdateProgressBar();
                    break;
                case 4:
                    Beat();
                    break;
                default:
                    break;
            }
            Sending();
        }
        private void Scroll()
        {
            try
            {
                //timer1.Interval = trackBar1.Value;
                this.Invoke((MethodInvoker)delegate()
                {
                    timer1.Interval = S_Adder.Value * 10000;
                    var result = pw.GetImage();
                    var s = TextRenderer.MeasureText(S_Text.Text, f);
                    Graphics g = Graphics.FromImage(result);
                    //g.DrawRectangle(new Pen(new SolidBrush(Color.Black)), 0, 0, result.Width, result.Height);
                    g.FillRectangle(Brushes.Black, 0, 0, result.Width, result.Height);
                    g.DrawString(S_Text.Text, f, new SolidBrush(Color.White), new PointF(ScrollX, -2));
                    //g.DrawImage(b, new Point(ScrollX, 0));
                    g.Dispose();
                    pw.SetImage(result);
                    ScrollX -= S_Interval.Value;
                    if (ScrollX < (s.Width + 16) * -1 && S_Interval.Value > 0)
                    {
                        if (S_Looped.Checked)
                            ScrollX = Display * 16;
                        else
                            timer1.Stop();
                    }
                    if (ScrollX > (Display * 16) + 1 && S_Interval.Value < 0)
                    {
                        if (S_Looped.Checked)
                            ScrollX = (s.Width + 16) * -1;
                        else
                            timer1.Stop();
                    }
                });
            }
            catch (Exception) { }
        }
        private void Sending()
        {
            if (serialPort1.IsOpen)
            {
                this.Invoke((MethodInvoker)delegate()
                {
                    Bitmap b = (Bitmap)pw.GetImage();
                    var color = Color.Black;
                    for (int i = 0; i < Display; i++)
                    {
                        byte buffer = 0;
                        string send = "!" + i;
                        for (int y = 0; y < 16; y++)
                        {
                            for (int x = 0; x < 8; x++)
                            {
                                color = b.GetPixel(x + (i * 16), y);
                                buffer = (byte)((buffer << 1) | ((color.R + color.G + color.B) / 3 > 128 ? 1 : 0));
                            }
                            send += buffer.ToString("X2");
                            buffer = 0;
                            for (int x = 8; x < 16; x++)
                            {
                                color = b.GetPixel(x + (i * 16), y);
                                buffer = (byte)((buffer << 1) | ((color.R + color.G + color.B) / 3 > 128 ? 1 : 0));
                            }
                            send += buffer.ToString("X2");
                        }

                        serialPort1.Write(send);
                    }
                });
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            serialPort1.Close();
            timer1.Stop();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Display = (int)PanelXNum.Value;
            pw.ChangeSize(Display * 16, 16);
            pictureBox1.Width = Display * 16 * 4;
            PaintPic = new Bitmap(Display * 16, 16);
            Graphics g = Graphics.FromImage(PaintPic);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, Display * 16, 16);
            g.Dispose();
            pictureBox1.Image = PaintPic;
            pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            checkBox3_CheckedChanged(null, null);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //timer1.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "動画ファイル(*.avi;*.mpg;*.mp4;*.wmv)|*.avi;*.mpg;*.mp4;*.wmv|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 0;
            ofd.Title = "読み込む動画ファイル";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                M_Filename.Text = ofd.FileName;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            media.Stop();
            if (eventEx != null)
            {
                eventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                eventEx = null;
            }
            if (window != null)
            {
                window.put_Visible(OABool.False);
                window.put_Owner(IntPtr.Zero);
                window = null;
            }
            render = null;
            media = null;
            graph = null;
            timer1.Stop();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            timer1.Interval = (int)(1000000.0f / (int)M_FPS.Value);
            M_CollectFPS = (float)M_FPS.Value;
        }
        private void Animation()
        {
            try
            {
                this.Invoke((MethodInvoker)delegate()
                {
                    Bitmap ss = CaptureControl(renderwindow);
                    if (ss != null)
                    {
                        pw.SetImage(HalfToneProcess(ss));
                        ss.Dispose();
                    }
                    if (imp != null)
                    {
                        double Position = 0;
                        imp.get_CurrentPosition(out Position);
                        M_Seek.Value = (int)Position;
                    }
                });
            }
            catch (Exception)
            {

            }
        }
        enum HalfType : int
        {
            Binarization = 0,
            BinarizationAuto,
            DitherBayer,
            DitherHalfTone,
            Dither2x2,
            ErrorDiffusionSierraLite
        };
        private Bitmap HalfToneProcess(Bitmap b)
        {
            Bitmap result = pw.GetImage();
            int w = result.Width, h = result.Height;
            Bitmap ssbuff = new Bitmap(w, h);
            Graphics gssbuff = Graphics.FromImage(ssbuff);
            gssbuff.DrawImage(b, (float)M_Top.Value, (float)M_Left.Value, (float)M_Bottom.Value, (float)M_Right.Value);
            BitmapData data = ssbuff.LockBits(
                new Rectangle(0, 0, ssbuff.Width, ssbuff.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData rdata = result.LockBits(
                new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            byte[] buf = new byte[w * h * 4];
            byte[] rbuf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            switch ((HalfType)M_Type.SelectedIndex)
            {
                case HalfType.Binarization:
                    {
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                if ((buf[p] + buf[p + 1] + buf[p + 2]) / 3.0 > 128)
                                {
                                    rbuf[p] = 0xff;
                                    rbuf[p + 1] = 0xff;
                                    rbuf[p + 2] = 0xff;
                                    rbuf[p + 3] = 0xff;
                                }
                                else
                                {
                                    rbuf[p] = 0;
                                    rbuf[p + 1] = 0;
                                    rbuf[p + 2] = 0;
                                    rbuf[p + 3] = 0xff;
                                }
                                return y;
                            }, (i) => { });
                        }
                    }
                    break;
                case HalfType.BinarizationAuto:
                    {
                        double avg = buf.Average((i) => { return (int)i; });
                        byte t = (byte)avg;
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                if ((buf[p] + buf[p + 1] + buf[p + 2]) / 3.0 > t)
                                {
                                    rbuf[p] = 0xff;
                                    rbuf[p + 1] = 0xff;
                                    rbuf[p + 2] = 0xff;
                                    rbuf[p + 3] = 0xff;
                                }
                                else
                                {
                                    rbuf[p] = 0;
                                    rbuf[p + 1] = 0;
                                    rbuf[p + 2] = 0;
                                    rbuf[p + 3] = 0xff;
                                }
                                return y;
                            }, (i) => { });
                        }
                    }
                    break;
                case HalfType.DitherBayer:
                case HalfType.DitherHalfTone:
                    {
                        int map = M_Type.SelectedIndex - (int)HalfType.DitherBayer;
                        for (int iy = 0; iy < h; iy += 4)
                        {
                            Parallel.For<int>(0, w / 4, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + (i * 4)) * 4;
                                int pp = 0;
                                int s = 0;
                                int count = 0;
                                for (int iiy = 0; iiy < 4; iiy++)
                                {
                                    for (int iix = 0; iix < 4; iix++)
                                    {
                                        pp = p + (w * iiy + iix) * 4;
                                        if (buf.Length > p + 4 && (iix + (i * 4)) < w)
                                        {
                                            s += (int)((buf[pp] + buf[pp + 1] + buf[pp + 2]) / 3.0);
                                            count++;
                                        }
                                    }
                                }
                                if (count > 0)
                                {
                                    s /= count;
                                    for (int iiy = 0; iiy < 4; iiy++)
                                    {
                                        for (int iix = 0; iix < 4; iix++)
                                        {
                                            pp = p + (w * iiy + iix) * 4;
                                            if (buf.Length > p + 4 && (iix + (i * 4)) < w)
                                            {
                                                if (DitherMap[map, iiy, iix] < s)
                                                {
                                                    rbuf[pp] = 0xff;
                                                    rbuf[pp + 1] = 0xff;
                                                    rbuf[pp + 2] = 0xff;
                                                    rbuf[pp + 3] = 0xff;
                                                }
                                                else
                                                {
                                                    rbuf[pp] = 0;
                                                    rbuf[pp + 1] = 0;
                                                    rbuf[pp + 2] = 0;
                                                    rbuf[pp + 3] = 0xff;
                                                }
                                            }
                                        }
                                    }
                                }
                                return y;
                            }, (i) => { });
                        }

                    }
                    break;
                case HalfType.Dither2x2:
                    {
                        byte[,] DitherMap = {
                                                {   0, 128 },
                                                { 191,  64 }
                                            };
                        for (int iy = 0; iy < h; iy += 2)
                        {
                            Parallel.For<int>(0, w / 2, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + (i * 2)) * 4;
                                int pp = 0;
                                int s = 0;
                                int count = 0;
                                for (int iiy = 0; iiy < 2; iiy++)
                                {
                                    for (int iix = 0; iix < 2; iix++)
                                    {
                                        pp = p + (w * iiy + iix) * 4;
                                        if (buf.Length > p + 4 && (iix + (i * 2)) < w)
                                        {
                                            s += (int)((buf[pp] + buf[pp + 1] + buf[pp + 2]) / 3.0);
                                            count++;
                                        }
                                    }
                                }
                                if (count > 0)
                                {
                                    s /= count;
                                    for (int iiy = 0; iiy < 2; iiy++)
                                    {
                                        for (int iix = 0; iix < 2; iix++)
                                        {
                                            pp = p + (w * iiy + iix) * 4;
                                            if (buf.Length > p + 4 && (iix + (i * 2)) < w)
                                            {
                                                if (DitherMap[iiy, iix] < s)
                                                {
                                                    rbuf[pp] = 0xff;
                                                    rbuf[pp + 1] = 0xff;
                                                    rbuf[pp + 2] = 0xff;
                                                    rbuf[pp + 3] = 0xff;
                                                }
                                                else
                                                {
                                                    rbuf[pp] = 0;
                                                    rbuf[pp + 1] = 0;
                                                    rbuf[pp + 2] = 0;
                                                    rbuf[pp + 3] = 0xff;
                                                }
                                            }
                                        }
                                    }
                                }
                                return y;
                            }, (i) => { });
                        }
                    }
                    break;
                case HalfType.ErrorDiffusionSierraLite:
                    {
                        int[,] Map = new int[h, w];

                        for (int iy = 0; iy < h; iy++)
                        {
                            for (int ix = 0; ix < w; ix++)
                            {
                                int p = (w * iy + ix) * 4;
                                Map[iy, ix] += (int)((float)((byte)buf[p] + (byte)buf[p + 1] + (byte)buf[p + 2]) / 3.0f);

                                int def = Map[iy, ix];
                                if (Map[iy, ix] > 128)
                                {
                                    rbuf[p] = 0xff;
                                    rbuf[p + 1] = 0xff;
                                    rbuf[p + 2] = 0xff;
                                    rbuf[p + 3] = 0xff;
                                    def -= 255;
                                }
                                else
                                {
                                    rbuf[p] = 0;
                                    rbuf[p + 1] = 0;
                                    rbuf[p + 2] = 0;
                                    rbuf[p + 3] = 0xff;
                                }
                                if (ix + 1 < w) { Map[iy, ix + 1] += (int)((float)def * (2f / 4f)); }
                                if (iy + 1 < h) { Map[iy + 1, ix] += (int)((float)def * (1f / 4f)); }
                                if (iy + 1 < h && ix - 1 >= 0) { Map[iy + 1, ix - 1] += (int)((float)def * (1f / 4f)); }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            Marshal.Copy(rbuf, 0, rdata.Scan0, rbuf.Length);
            ssbuff.UnlockBits(data);
            result.UnlockBits(rdata);
            return result;
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (Connected.Checked)
            {
                try
                {
                    if (serialPort1.IsOpen) { serialPort1.Close(); }
                    serialPort1.PortName = (string)COMList.Text;
                    serialPort1.BaudRate = int.Parse(Baudrate.Text);
                    serialPort1.Open();
                }
                catch (Exception)
                {
                    MessageBox.Show("Don't open SerialPort!!!");
                    Connected.Checked = false;
                }
            }
            else
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
            }
        }

        WaveIn wavein;
        private void button9_Click(object sender, EventArgs e)
        {
        }
        List<float> _Lrecode = new List<float>();
        List<float> _Rrecode = new List<float>();
        void wavein_DataAvailable(object sender, WaveInEventArgs e)
        {
            float L, R;
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                L = ((short)((e.Buffer[i + 1] << 8) | e.Buffer[i]) / 32768f);
                R = ((short)((e.Buffer[i + 3] << 8) | e.Buffer[i + 2]) / 32768f);
                ProcessSample(L, R);
            }
        }
        int Sample = 4096;
        float old = 0;
        private void ProcessSample(float Lsample, float Rsample)
        {
            if (_Lrecode.Count > Sample)
            {
                _Lrecode.RemoveRange(0, _Lrecode.Count - Sample);
            }
            if (_Rrecode.Count > Sample)
            {
                _Rrecode.RemoveRange(0, _Rrecode.Count - Sample);
            }
            _Lrecode.Add(Lsample * gain);
            _Rrecode.Add(Rsample * gain);
            if (Mode == 4 && Sample < _Lrecode.Count)
            {
                BeetSample();
                _Lrecode.Clear();
                _Rrecode.Clear();
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (wavein != null)
            {
                wavein.StopRecording();
            }
            timer1.Stop();
        }
        SolidBrush Black = new SolidBrush(Color.Black);
        SolidBrush White = new SolidBrush(Color.White);
        private void UpdateProgressBar()
        {
            try
            {
                this.Invoke((MethodInvoker)delegate()
                {
                    if (_Lrecode.Count > 0 && _Rrecode.Count > 0)
                    {
                        double L = 0, R = 0;
                        foreach (var i in _Lrecode)
                        {
                            L += Math.Abs(i);
                        }
                        foreach (var i in _Rrecode)
                        {
                            R += Math.Abs(i);
                        }
                        L = L / _Lrecode.Count;
                        R = R / _Rrecode.Count;
                        Bitmap img = pw.GetImage();
                        Graphics g = Graphics.FromImage(img);
                        switch (VolumeMode)
                        {
                            case 0:
                                Sample = 64;
                                g.FillRectangle(Black, 0, 0, img.Width, img.Height);
                                g.FillRectangle(White, 0, 0, (int)((img.Width) * (L)), img.Height / 2);
                                g.FillRectangle(White, 0, img.Height / 2, (int)((img.Width) * (R)), img.Height);
                                break;
                            case 1:
                                {
                                    Sample = 64;
                                    int HW = (int)(img.Width / 2);
                                    int LW = (int)(HW * (L)), RW = (int)(HW * (R));
                                    g.FillRectangle(Black, 0, 0, img.Width, img.Height);

                                    g.FillRectangle(White, HW - LW, 0, LW, img.Height);
                                    g.FillRectangle(White, img.Width / 2, 0, RW, img.Height);
                                }
                                break;
                            case 3:
                                {
                                    Sample = 1600;
                                    List<float> AD = new List<float>(_Lrecode);
                                    AD = AD.Select((v, i) => (v + _Rrecode[i]) / 2).ToList();
                                    var HW = Window.Hamming(AD.Count);
                                    var AW = AD.Select((v, i) => new Complex((v * (float)HW[i]), 0)).ToArray();
                                    Fourier.Forward(AW, FourierOptions.Matlab);
                                    var AF = AW.Take(AW.Length / 2).Select((v) => (double)Math.Sqrt(v.Real * v.Real + v.Imaginary * v.Imaginary)).ToArray();
                                    List<RectangleF> FFT = new List<RectangleF>();
                                    double Max = 2;
                                    float sx = (float)img.Width / (float)AF.Length, y = 0, X = 1.4f;
                                    int counter = 0;
                                    foreach (var i in AF)
                                    {
                                        y = (float)(img.Height * Math.Log10((i / Max)));
                                        FFT.Add(new RectangleF(((float)Math.Log((counter * sx) + 1d, AF.Length) * img.Width * X), img.Height - y, (float)((Math.Log((float)((counter + 6) * sx) / (float)((counter + 5) * sx), AF.Length) * img.Width * X)), y));
                                        //FFT.Add(new RectangleF((float)(sx * counter), img.Height - y, sx, y));
                                        counter++;
                                    }
                                    g.FillRectangle(Black, 0, 0, img.Width, img.Height);
                                    g.FillRectangles(White, FFT.ToArray());
                                }
                                break;
                            case 4:
                                {
                                    Sample = img.Width * 20;
                                    float hh = (img.Height / 2);
                                    Pen p = new Pen(White, 1);
                                    g.FillRectangle(Black, 0, 0, img.Width, img.Height);
                                    float WX = (float)img.Width / (float)_Lrecode.Count;
                                    for (int i = 0; i < _Lrecode.Count - 1; i++)
                                    {
                                        g.DrawLine(p, (float)(i * WX), hh + _Lrecode[i] * hh, (float)((i + 1) * WX), hh + _Lrecode[i + 1] * hh);
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                        g.Dispose();
                        pw.SetImage(img);
                    }
                });
            }
            catch (Exception)
            {

            }
        }
        private void StopAnimation()
        {
            if (isPlaying)
            {
                M_PrevSize.Enabled = true;
                media.Stop();
                if (eventEx != null)
                {
                    eventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                    eventEx = null;
                }
                if (window != null)
                {
                    window.put_Visible(OABool.False);
                    window.put_Owner(IntPtr.Zero);
                    window = null;
                }
                iba = null;
                imp = null;
                igb = null;
                render = null;
                media = null;
                graph = null;
                timer1.Stop();
                isPlaying = false;
            }
        }
        private void button10_Click(object sender, EventArgs e)
        {
            bool start = true;

            timer1.Stop();
            StopAnimation();
            if (isVolueing)
            {
                if (wavein != null)
                {
                    wavein.StopRecording();
                }
                timer1.Stop();
                isVolueing = false;
            }

            Mode = tabControl1.SelectedIndex;
            switch (Mode)
            {
                case 0:
                    timer1.Interval = S_Adder.Value * 1000;
                    pw.ChangeSize(Display * 16, 16);
                    ScrollX = Display * 16;
                    if (S_Text.Text == "")
                    {
                        MessageBox.Show("文字入ってなお(´・ω・`)");
                        start = false;
                    }
                    break;
                case 1:
                    {
                        graph = new FilterGraph() as IFilterGraph;
                        media = graph as IMediaControl;
                        eventEx = media as IMediaEventEx;
                        igb = media as IGraphBuilder;
                        imp = igb as IMediaPosition;
                        media.RenderFile(M_Filename.Text);
                        graph.FindFilterByName("Video Renderer", out render);
                        if (render != null)
                        {
                            window = render as IVideoWindow;
                            window.put_WindowStyle(
                                WindowStyle.Caption
                                );
                            window.put_WindowStyleEx(
                                WindowStyleEx.ToolWindow
                                );
                            int Width, Height;
                            window.get_Width(out Width);
                            window.get_Height(out Height);
                            window.put_Caption("ElectronicBoard - VideoPrev -");
                            renderSize.Width = (int)(Width * (float)(M_PrevSize.Value));
                            renderSize.Height = (int)(Height * (float)(M_PrevSize.Value));
                            window.SetWindowPosition(0, 0, renderSize.Width, renderSize.Height);
                            eventEx = media as IMediaEventEx;
                            eventEx.SetNotifyWindow(this.Handle, WM_DirectShow, IntPtr.Zero);
                            media.Run();
                            foreach (Process p in Process.GetProcesses())
                            {
                                if (p.MainWindowTitle == "ElectronicBoard - VideoPrev -")
                                {
                                    renderwindow = p.MainWindowHandle;
                                    break;
                                }
                            }
                            timer1.Interval = (int)(1000000.0f / (int)M_FPS.Value);
                            timer1.Start();
                            M_PrevSize.Enabled = false;
                            isPlaying = true;
                            double Max = 0;
                            imp.get_Duration(out Max);
                            M_Seek.Maximum = (int)Max;
                            M_Seek.Value = 0;
                            iba = media as IBasicAudio;
                            iba.put_Volume(M_Volume.Value);

                            checkBox3_CheckedChanged(null, null);
                        }
                        else
                            start = false;
                    }
                    break;
                case 2:
                    timer1.Stop();
                    break;
                case 3:
                    try
                    {
                        wavein = new WaveIn()
                        {
                            DeviceNumber = SoundList.SelectedIndex,
                        };
                        wavein.DataAvailable += wavein_DataAvailable;
                        wavein.WaveFormat = new WaveFormat(48000, 16, 2);
                        wavein.StartRecording();
                        timer1.Interval = 10000;
                        timer1.Start();
                        isVolueing = true;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Sound Device Error");
                    }
                    break;
                case 4:
                    try
                    {
                        wavein = new WaveIn()
                        {
                            DeviceNumber = SoundList.SelectedIndex,
                        };
                        Sample = 4000;
                        wavein.DataAvailable += wavein_DataAvailable;
                        wavein.WaveFormat = new WaveFormat(4000, 16, 2);
                        wavein.StartRecording();
                        timer1.Interval = 1000000 / 60;
                        timer1.Start();
                        isVolueing = true;
                        O0 = (float)(2.0 * Math.PI * f0 / Fs);
                        a = (float)(Math.Sin(O0) / Q);
                        b0 = (float)((1.0f - Math.Cos(O0)) / 2.0f);
                        b1 = (float)((1.0f - Math.Cos(O0)));
                        b2 = b0;
                        a0 = (float)(1 + a);
                        a1 = (float)(-2 * Math.Cos(O0));
                        a2 = (float)(1 - a);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Sound Device Error");
                    }
                    break;
                default:
                    break;
            }
            if (start)
                timer1.Start();
        }
        long frame = 0;
        private void Beat()
        {
            var img = pw.GetImage();
            frame++;
            try
            {
                float size = (float)((float)frame / (float)BeetPerFrame);
                size = size > 0 ? size : 0.001f;
                size *= img.Width;
                float size2 = size / 2;
                float cx = img.Width / 2;
                float cy = img.Height / 2;
                Pen p = new Pen(White, 1);
                var s = 0.001;
                var a = ArgBeet.Count > 0 ? (ArgBeet.Average() * s) : 0;
                var b = (BeetCounter * s);
                Graphics g = Graphics.FromImage(img);
                g.FillRectangle(Black, 0, 0, img.Width, img.Height);
                if (LP.Count > 0)
                {
                    float SW = (float)img.Width / (float)LP.Count;
                    float hh = (float)img.Height / 2.0f;
                    for (int i = 0; i < LP.Count - 1; i += 100)
                    {
                        g.DrawLine(p, (float)(i * SW), hh + LP2[i] * hh, (float)((i + 1) * SW), hh + LP2[i + 1] * hh);
                    }
                }
                g.FillRectangle(new SolidBrush(Color.Green), 0, 0, (a > img.Width ? img.Width : (int)a), img.Height / 2);
                g.FillRectangle(new SolidBrush(Color.Red), 0, img.Height / 2, (b > img.Width ? img.Width : (int)b), img.Height);
                g.DrawRectangle(new Pen(Color.Blue), 0.0f, 0.0f, (float)(Fs * 3 * s), (float)img.Height);
                g.FillPie(White, cx - size2, cy - size2, size, size, 0, 360);
                g.Dispose();
                if (BeetPerFrame < frame) { frame = 0; }
                pw.SetImage(img);
            }
            catch (InvalidOperationException) { }
            catch (ArgumentException) { }
        }
        float Fs = 8000.0f;
        float f0 = 500.0f;
        float Q = 5f;
        float a, b0, b1, b2, a0, a1, a2;
        float O0;
        long BeetCounter = 0;
        List<long> ArgBeet = new List<long>();
        long BeetPerFrame = 60;
        int Bcount = 0;
        List<float> LP = new List<float>();
        List<float> LP2 = new List<float>();

        private void BeetSample()
        {
            List<float> AD = new List<float>(_Lrecode);
            var HW = Window.Hann(AD.Count);
            AD = AD.Select((v, i) => ((v + _Rrecode[i])) / 2.0f).ToList();
            LP.Clear();
            LP.AddRange(AD);
            //var LP = new List<float>(AD.Count);

            for (int i = 2; i < AD.Count; i++)
            {
                LP.Add(((b0 + b1 * AD[i - 1] + b2 * AD[i - 2]) / (a0 + a1 * AD[i - 1] + a2 * AD[i - 2])) * AD[i]);
            }
            LP2.Clear();
            for (int i = 5; i < AD.Count - 5; i++)
            {
                float sum = 0;
                for (int ii = i - 5; ii < i + 5; ii++)
                {
                    sum += Math.Abs(LP[ii]);
                }
                LP2.Add(sum / 10.0f);
            }
            int c = 0;
            var diff = 0;
            float old = 0;
            foreach (var i in LP2)
            {
                if (Math.Abs(i - old) > (float)numericUpDown9.Value && BeetCounter > 1000)
                {
                    ArgBeet.Add(BeetCounter);
                    diff = LP.Count - c;
                    BeetCounter = 0;
                    if (ArgBeet.Count > 10)
                    {
                        ArgBeet.RemoveAt(0);
                    }
                }
                else
                {
                    BeetCounter++;
                    if (BeetCounter > Fs * 3)
                    {
                        BeetCounter = 0;
                    }
                }
                old = i;
                c++;
            }
            if (ArgBeet.Count > 0)
            {
                label15.Text = ArgBeet.Average().ToString();
                var gg = (long)(ArgBeet.Average() / Fs * 60.0);
                if (Math.Abs(BeetPerFrame - gg) < 50 || Bcount > 10)
                {
                    BeetPerFrame = gg;
                    Bcount = 0;
                    frame = diff;
                    BeetPerFrame = BeetPerFrame > 0 ? BeetPerFrame : 1;
                }
                else
                {
                    Bcount++;
                }
            }
        }
        private void comboBox4_TextUpdate(object sender, EventArgs e)
        {
        }

        private void comboBox4_SelectionChangeCommitted(object sender, EventArgs e)
        {
            VolumeMode = comboBox4.SelectedIndex;
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DirectShow:
                    DirechShowEvent();
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }

            // 子ウィンドウにも同じイベントを送る
            if (window != null)
                window.NotifyOwnerMessage(m.HWnd, m.Msg, m.WParam, m.LParam);
        }

        private void DirechShowEvent()
        {
            bool isComplete = false;
            EventCode eventCode;
            IntPtr param1, param2;
            int hresult = -1;

            do
            {
                // イベントを取得
                hresult = eventEx.GetEvent(
                    out eventCode,
                    out param1,
                    out param2,
                    0);

                if (hresult == 0)
                {
                    // 再生終了フラグ
                    isComplete = (eventCode == EventCode.Complete);

                    // イベントを削除
                    eventEx.FreeEventParams(eventCode, param1, param2);
                }
            } while (hresult == 0);

            // 再生終了
            if (isComplete)
            {
                timer1.Stop();
                StopAnimation();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                timer1.Stop();
                StopAnimation();
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (Mode == 2)
            {
                int X = (int)Math.Ceiling(e.X / 4d);
                int Y = (int)Math.Ceiling(e.Y / 4d);
                OldPoint.X = X;
                OldPoint.Y = Y;
                var c = PaintPic.GetPixel(X, Y);
                if (c.R < 128) { ColorFlag = true; }
                else { ColorFlag = false; }
                Clicking = true;
                PaintPic.SetPixel(X, Y, ColorFlag ? Color.White : Color.Black);
                pictureBox1.Image = PaintPic;
                pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                pw.SetImage(PaintPic);
                Sending();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (Clicking)
            {
                int X = (int)Math.Ceiling(e.X / 4d);
                int Y = (int)Math.Ceiling(e.Y / 4d);
                if (X >= 0 && Y >= 0 && X < PaintPic.Width && Y < PaintPic.Height)
                {
                    Graphics g = Graphics.FromImage(PaintPic);
                    Pen p = new Pen(ColorFlag ? White : Black);
                    g.DrawLine(p, OldPoint.X, OldPoint.Y, X, Y);
                    OldPoint.X = X;
                    OldPoint.Y = Y;
                    g.Dispose();
                    pictureBox1.Image = PaintPic;
                    pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    pw.SetImage(PaintPic);
                    Sending();
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Clicking = false;
            pw.SetImage(PaintPic);
            Sending();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Graphics g = Graphics.FromImage(PaintPic);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, PaintPic.Width, PaintPic.Height);
            g.Dispose();
            pictureBox1.Image = PaintPic;
            pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            pw.SetImage(PaintPic);
            Sending();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            Graphics g = Graphics.FromImage(PaintPic);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, PaintPic.Width, PaintPic.Height);
            g.Dispose();
            pictureBox1.Image = PaintPic;
            pictureBox1.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            pw.SetImage(PaintPic);
            Sending();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            if (isPlaying)
                M_ChangeSeek = true;//imp.put_CurrentPosition((double)M_Seek.Value);
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            if (isPlaying)
                M_ChangeVolume = true;//iba.put_Volume(M_Volume.Value);
        }

        public void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            M_Top.Enabled = !M_CopyAuto.Checked;
            M_Left.Enabled = !M_CopyAuto.Checked;
            M_Bottom.Enabled = !M_CopyAuto.Checked;
            M_Right.Enabled = !M_CopyAuto.Checked;
            if (M_CopyAuto.Checked)
            {
                Bitmap r = pw.GetImage();
                float zx, zy, zz, cx, cy;
                zx = (float)r.Width / (float)renderSize.Width;
                zy = (float)r.Height / (float)renderSize.Height;
                zz = (zx > zy ? zx : zy);
                cx = (zx > zy ? 0 : (r.Width - renderSize.Width * zz) / 2);
                cy = (zx <= zy ? 0 : (r.Height - renderSize.Height * zz) / 2);
                M_Top.Value = (decimal)cx;
                M_Left.Value = (decimal)cy;
                M_Bottom.Value = (decimal)(renderSize.Width * zz);
                M_Right.Value = (decimal)(renderSize.Height * zz);
            }
        }

        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            gain = (float)Math.Pow(10.0, ((double)numericUpDown10.Value) / 10.0);
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            f0 = (float)numericUpDown8.Value;
            Q = (float)numericUpDown11.Value;
            O0 = (float)(2.0 * Math.PI * f0 / Fs);
            a = (float)(Math.Sin(O0) / Q);
            b0 = (float)((1.0f - Math.Cos(O0)) / 2.0f);
            b1 = (float)((1.0f - Math.Cos(O0)));
            b2 = b0;
            a0 = (float)(1 + a);
            a1 = (float)(-2 * Math.Cos(O0));
            a2 = (float)(1 - a);
        }*/
    }
}
