using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Drawing;
using MicroLibrary;
using System.Windows.Forms;
using System.IO.Ports;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace ElectronicBoard.Classes
{
    public class EventMaster
    {
        private static EventMaster master = new EventMaster();
        public PlayStyle Mode { get; private set; }
        public MainForm form;
        public PrevWindow preview;
        private MicroTimer updateTimer = new MicroTimer();
        public SerialPort serialPort = new SerialPort();
        public enum PlayStyle : uint
        {
            Null,
            Scroll,
            Movie,
            Paint,
            SoundEffect,
            Beat,
            Test
        }
        private Dictionary<PlayStyle, Eventer> Events = new Dictionary<PlayStyle, Eventer>();
        public bool Sending = false;
        List<long> log = new List<long>();
        Stopwatch sw = new Stopwatch();
        Thread SendThread;
        Bitmap SendingImage = null;
        bool StopSending = false;

        public Eventer GetEvents(PlayStyle style)
        {
            if (Events.ContainsKey(style))
                return Events[style];
            return Events[PlayStyle.Null];
        }
        private void Send()
        {
            while (!StopSending)
            {
                if (serialPort.IsOpen && Sending)
                {
                    Bitmap p = SendingImage;
                    BitmapData data = p.LockBits(new Rectangle(0, 0, p.Width, p.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    //int X = p.Height, Y = p.Width;
                    Byte[] picdata = new byte[p.Width * p.Height * 4];
                    Marshal.Copy(data.Scan0, picdata, 0, picdata.Length);
                    int PanelCount = 0;
                    String sendData = "";
                    byte buffer;
                    //byte b;
                    for (int py = 0; py < p.Height; py += 16)
                    {
                        for (int px = 0; px < p.Width; px += 16)
                        {
                            sendData = "!" + PanelCount;
                            for (int iy = 0; iy < 16; iy++)
                            {
                                buffer = 0;
                                for (int ix = 0; ix < 8; ix++)
                                {
                                    //b = (byte)(picdata[((py + iy) * p.Height + ix) * 4] > 128 ? 1 : 0);
                                    buffer = (byte)((buffer << 1) | (picdata[(((py + iy) * p.Width) + (ix + px)) * 4] > 128 ? 1 : 0));
                                }
                                sendData += buffer.ToString("X2");
                                buffer = 0;
                                for (int ix = 8; ix < 16; ix++)
                                {
                                    buffer = (byte)((buffer << 1) | (picdata[((py + iy) * p.Width + ix + px) * 4] > 128 ? 1 : 0));
                                }
                                sendData += buffer.ToString("X2");
                            }
                            if (serialPort.IsOpen)
                                serialPort.Write(sendData);
                            PanelCount++;
                        }
                    }
                    /*
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

                        serialPort.Write(send);
                    }
                     */
                    lock (log)
                    {
                        log.Add(sw.ElapsedMilliseconds);
                        sw.Restart();
                        if (log.Count > 100)
                        {
                            log.RemoveRange(0, log.Count - 100);
                        }
                    }
                }
                Sending = false;
                Thread.Sleep(1);
            }
        }
        public bool OpenSerial(string name, int Baudrate)
        {
            try
            {
                if (serialPort.IsOpen) { serialPort.Close(); }
                serialPort.PortName = (string)name;
                serialPort.BaudRate = Baudrate;
                serialPort.Open();
                StopSending = false;
                SendThread = new Thread(new ThreadStart(Send));
                SendThread.Start();
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Don't open SerialPort!!!");
            }
            return false;
        }
        public void CloseSerial()
        {
            if (SendThread != null)
            {
                //SendThread.Abort();
                StopSending = true;
                while (SendThread.ThreadState == System.Threading.ThreadState.Stopped) { }
                SendThread = null;
            }
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        EventMaster()
        {

        }
        ~EventMaster()
        {
            Final();
        }
        public static EventMaster GetInstance() { return master; }
        public void Init(MainForm control, PrevWindow prev)
        {
            form = control;
            preview = prev;
            updateTimer.Interval = 16667;
            updateTimer.MicroTimerElapsed += Tick;
            //Start();
        }
        public void Final()
        {
            StopSending = true;
            updateTimer.Stop();
        }
        public void Start()
        {
            updateTimer.Start();
            sw.Start();
        }
        public void About()
        {
            if (Events.ContainsKey(Mode))
                Events[Mode].About();
        }
        public void Stop()
        {
            updateTimer.Stop();
        }
        public void ChangeEvent(PlayStyle style)
        {
            if (Events.ContainsKey(Mode))
            {
                Events[Mode].Final();
            }
            Mode = style;
            if (Events.ContainsKey(style))
            {
                Events[Mode].Init();
                Start();
            }
        }
        private void Tick(object sender, MicroTimerEventArgs timerEventArgs)
        {
            if (Events.ContainsKey(Mode) && Mode != PlayStyle.Null && !form.IsDisposed)
            {
                Events[Mode].Update();
                if (updateTimer.Interval != Events[Mode].span)
                    updateTimer.Interval = Events[Mode].span;
                Bitmap image = Events[Mode].Draw(preview.GetImage());
                if (image != null)
                    preview.SetImage(image);
                lock (log)
                {
                    if (log.Count > 0)
                        preview.UpdateSendingData(serialPort.IsOpen, serialPort.IsOpen ? (float)(1000.0d / (log.Sum() / log.Count)) : 0);
                    else
                        preview.UpdateSendingData(false, 0);
                }
                /*if (!Sending && serialPort.IsOpen)
                {
                    Sending = true;
                    Thread t = new Thread(new ThreadStart(Send));
                    t.Name = "SendingThread";
                    t.Start();
                }*/
                if (SendThread != null)
                {
                    if (!Sending && preview.IsUpdated())
                    {
                        if (!form.IsDisposed)
                        {
                            form.Invoke((MethodInvoker)delegate ()
                            {
                                SendingImage = (Bitmap)preview.GetImage().Clone();
                            });
                            Sending = true;
                        }
                    }
                }
            }
        }
        public void EventAdd(PlayStyle style, Eventer e)
        {
            if (Events.ContainsKey(style))
            {
                Events[style] = e;
            }
            else
            {
                Events.Add(style, e);
            }
        }
        public void EventRemove(PlayStyle style)
        {
            if (Events.ContainsKey(style))
            {
                Events.Remove(style);
            }
        }
        public bool WndProc(ref Message m)
        {
            if (Mode != PlayStyle.Null)
                return Events[Mode].WindowEvent(ref m);
            return false;
        }
        public void Event(string EventName, object arg)
        {
            if (Events.ContainsKey(Mode))
                Events[Mode].Event(EventName, arg);
        }
    }
}
