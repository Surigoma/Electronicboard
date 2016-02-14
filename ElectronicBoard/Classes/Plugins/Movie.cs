using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DirectShowLib;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;

namespace ElectronicBoard.Classes.Plugins
{
    public class Movie : Eventer
    {
        const int WM_Application = 0x8000;
        const int WM_DirectShow = WM_Application + 1;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest,
             int nXDest, int nYDest, int nWidth, int nHeight,
             IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0xCC0020;
        IntPtr renderwindow = IntPtr.Zero;
        Size renderSize = new Size(640, 480);
        //renderSize.Height / renderSize.Width;
        float Aspect = 1;
        bool isPlaying = false;
        ParallelOptions Option = new ParallelOptions() { MaxDegreeOfParallelism = Process.GetProcesses().Length > 2 ? Process.GetProcesses().Length - 1 : 1 };
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
        public float GetAspect() { return Aspect; }
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
        enum HalfType : int
        {
            Binarization = 0,
            Binarization_R,
            Binarization_G,
            Binarization_B,
            BinarizationAuto,
            BinarizationPlusEdge,
            DitherBayer,
            DitherHalfTone,
            Dither2x2,
            ErrorDiffusionSierraLite
        };
        private Bitmap HalfToneProcess(Bitmap prev, Bitmap ss, HalfType type, PointF S, PointF E)
        {
            List<byte> argHalf = new List<byte>(10);
            Bitmap result = prev;
            int w = result.Width, h = result.Height;
            Bitmap ssbuff = new Bitmap(w, h);
            Graphics gssbuff = Graphics.FromImage(ssbuff);
            gssbuff.DrawImage(ss, S.X, S.Y, E.X - S.X, E.Y - S.Y);
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
            switch (type)
            {
                case HalfType.Binarization:
                    {
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
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
                case HalfType.Binarization_R:
                    {
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                if (buf[p] > 128)
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
                case HalfType.Binarization_G:
                    {
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                if (buf[p + 1] > 128)
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
                case HalfType.Binarization_B:
                    {
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                if (buf[p + 2] > 128)
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
                        if (buf.Count((i) => { return (Math.Max(i,t) - Math.Min(i,t) < 10); }) > buf.Length * 0.3)
                        {
                            t = argHalf.Count > 0 ? (byte)argHalf.Average((i)=> { return (float)i; }) : (byte)128;
                        }
                        argHalf.Add(t);
                        if(argHalf.Count > 10) { argHalf.RemoveAt(0); }
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
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
                case HalfType.BinarizationPlusEdge:
                    {
                        int[,] map = new int[3, 3];
                        int[, ,] sobel = {
                                            {
                                                { -1,  0,  1 },
                                                { -2,  0,  2 },
                                                { -1,  0,  1 }
                                            },{
                                                { -1, -2, -1 },
                                                {  0,  0,  0 },
                                                {  1,  2,  1 }
                                            }
                                        };
                        for (int iy = 0; iy < h; iy++)
                        {
                            Parallel.For<int>(0, w, Option, () => { return iy; }, (i, state, y) =>
                            {
                                int p = (w * y + i) * 4;
                                byte c = (byte)((buf[p] + buf[p + 1] + buf[p + 2]) / 3.0f);
                                int fx = 0, fy = 0;
                                int myi = (y - 1 >= 0 ? -1 : 0);
                                int mxi = (i - 1 >= 0 ? -1 : 0);
                                int mya = (y + 1 < h ? 1 : 0);
                                int mxa = (i + 1 < w ? 1 : 0);
                                for (int my = myi; my <= mya; my++)
                                {
                                    for (int mx = mxi; mx <= mxa; mx++)
                                    {
                                        p = (w * (y + my) + (i + mx)) * 4;
                                        fx += sobel[0, my + 1, mx + 1] * (int)((buf[p] + buf[p + 1] + buf[p + 2]) / 3.0f);
                                        fy += sobel[1, my + 1, mx + 1] * (int)((buf[p] + buf[p + 1] + buf[p + 2]) / 3.0f);
                                    }
                                }
                                int f = (int)(fx * fx + fy * fy);//(int)Math.Sqrt(Math.Pow(fx, 2) + Math.Pow(fx, 2));
                                if (c > 128 ^ f > 7000)
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
                        int map = (int)type - (int)HalfType.DitherBayer;
                        for (int iy = 0; iy < h; iy += 4)
                        {
                            Parallel.For<int>(0, w / 4, Option, () => { return iy; }, (i, state, y) =>
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
                            Parallel.For<int>(0, w / 2, Option, () => { return iy; }, (i, state, y) =>
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
        /*
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
                        trackBar3.Value = (int)Position;
                    }
                });
            }
            catch (Exception)
            {

            }
        }*/
        /*
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
                            renderSize.Width = (int)(Width * (float)(numericUpDown7.Value));
                            renderSize.Height = (int)(Height * (float)(numericUpDown7.Value));
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
                            numericUpDown7.Enabled = false;
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
         */
        public override bool WindowEvent(ref Message m)
        {
            if (m.Msg == WM_DirectShow)
            {
                DirechShowEvent();
                return true;
            }
            if (window != null)
                window.NotifyOwnerMessage(m.HWnd, m.Msg, m.WParam, m.LParam);
            return false;
        }
        public override void Init()
        {
            if (!isPlaying)
            {
                string Filename = "";
                float size = 0;
                double Max = 0;
                int volume = 0;

                graph = new FilterGraph() as IFilterGraph;
                media = graph as IMediaControl;
                eventEx = media as IMediaEventEx;
                igb = media as IGraphBuilder;
                imp = igb as IMediaPosition;
                master.form.Invoke((MethodInvoker)delegate()
                {
                    Filename = master.form.M_Filename.Text;
                    media.RenderFile(Filename);
                    size = (float)master.form.M_PrevSize.Value;
                    master.form.M_PrevSize.Enabled = false;
                    imp.get_Duration(out Max);
                    master.form.M_Seek.Maximum = (int)(Max);
                    master.form.M_Seek.Value = 0;
                    volume = master.form.M_Volume.Value;
                    span = (uint)(1000000.0f / master.form.M_CollectFPS);
                });
                graph.FindFilterByName("Video Renderer", out render);
                if (render != null)
                {
                    window = render as IVideoWindow;
                    window.put_WindowStyle(
                        WindowStyle.Caption | WindowStyle.Child
                        );
                    window.put_WindowStyleEx(
                        WindowStyleEx.ToolWindow
                        );
                    window.put_Caption("ElectronicBoard - VideoPrev -");

                    int Width, Height, Left, Top;
                    window.get_Width(out Width);
                    window.get_Height(out Height);
                    window.get_Left(out Left);
                    window.get_Top(out Top);
                    renderSize.Width = (int)(Width * size);
                    renderSize.Height = (int)(Height * size);
                    Aspect = (float)renderSize.Height / (float)renderSize.Width;
                    window.SetWindowPosition(Left, Top, renderSize.Width, renderSize.Height);

                    eventEx = media as IMediaEventEx;
                    eventEx.SetNotifyWindow(master.form.Handle, WM_DirectShow, IntPtr.Zero);
                    media.Run();
                    foreach (Process p in Process.GetProcesses())
                    {
                        if (p.MainWindowTitle == "ElectronicBoard - VideoPrev -")
                        {
                            renderwindow = p.MainWindowHandle;
                            break;
                        }
                    }
                    isPlaying = true;
                    iba = media as IBasicAudio;
                    iba.put_Volume(volume);

                    //master.form.checkBox3_CheckedChanged(null, null);
                    master.Start();
                }
            }
        }
        private void StopAnimation()
        {
            if (isPlaying)
            {
                master.Stop();
                if (master.form != null)
                {
                    master.form.Invoke((MethodInvoker)delegate()
                    {
                        master.form.M_PrevSize.Enabled = true;
                    });
                }
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
                master.Stop();
                isPlaying = false;
            }
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
                bool isLoop = false;
                master.form.Invoke((MethodInvoker)delegate()
                {
                    isLoop = master.form.M_isLoop.Checked;
                });
                if (!isLoop)
                {
                    StopAnimation();
                }
                else
                {
                    imp.put_CurrentPosition(0);
                }
            }
        }
        public override void Final()
        {
            StopAnimation();
        }
        public override void About()
        {
            StopAnimation();
        }
        bool pause = false;
        public override void Update()
        {
            if (isPlaying)
            {
                if (master.form.M_Pause.Checked != pause)
                {
                    pause = master.form.M_Pause.Checked;
                    if (pause)
                    {
                        media.Pause();
                    }
                    else
                    {
                        media.Run();
                    }
                }
                bool s = false, v = false;
                int volume = 0;
                double seek = 0;
                iba.get_Volume(out volume);
                imp.get_CurrentPosition(out seek);
                master.form.Invoke((MethodInvoker)delegate()
                {
                    span = (uint)(1000000.0f / master.form.M_CollectFPS);
                    if (master.form.M_SeekChanged)
                    {
                        seek = (double)(master.form.M_Seek.Value);
                        s = true;
                    }
                    else
                    {
                        master.form.M_Seek.Value = (int)(seek);
                    }
                    if (volume != master.form.M_Volume.Value)
                    {
                        volume = (int)master.form.M_Volume.Value;
                        v = true;
                    }
                });
                if (s)
                {
                    imp.put_CurrentPosition(seek);
                    master.form.M_SeekChanged = false;
                }
                if (v)
                    iba.put_Volume(volume);
            }
        }
        public override Bitmap Draw(Bitmap p)
        {
            Bitmap result = p;
            try
            {
                Bitmap ss = CaptureControl(renderwindow);
                HalfType type = HalfType.Binarization;
                PointF S = new PointF(), E = new PointF();
                master.form.Invoke((MethodInvoker)delegate()
                {
                    type = (HalfType)master.form.M_Type.SelectedIndex;
                    S.X = (float)master.form.M_Left.Value;
                    S.Y = (float)master.form.M_Top.Value;
                    E.X = (float)master.form.M_Right.Value;
                    E.Y = (float)master.form.M_Bottom.Value;
                });
                if (ss != null)
                {
                    Bitmap buff = HalfToneProcess(p, ss, type, S, E);
                    if (buff != null)
                        result = buff;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : " + e.Message);
            }
            master.preview.Updated = true;
            return result;
        }
        public override void Event(string EventName, object arg)
        {
            base.Event(EventName, arg);
        }
    }
}
