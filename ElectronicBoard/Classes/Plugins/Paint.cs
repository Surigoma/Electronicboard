using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace ElectronicBoard.Classes.Plugins
{
    public class Paint : Eventer
    {
        Bitmap s = null;
        Graphics g = null;
        private bool Mouse_Clicked = false;
        private Point oldPoint = new Point();
        private Pen penb = new Pen(Color.Black);
        private Pen penw = new Pen(Color.White);
        private bool Color_Black = true;

        public struct MouseEvent
        {
            public bool Clicked;
            public Point Position;
        };

        public override void Init()
        {
            Bitmap ps = master.preview.GetImage();
            if(ps == null)
            {
                master.form.Invoke((MethodInvoker)delegate()
                {
                    master.form.PanelAccept_Click(null, null);
                    ps = master.preview.GetImage();
                });
            }
            if (g != null)
            {
                g.Dispose();
                g = null;
            }
            if (s != null)
            {
                s.Dispose();
                s = null;
                master.form.P_Image.Image.Dispose();
                master.form.P_Image.Image = null;
            }
            s = (Bitmap)master.form.P_Image.Image;
            if (s == null)
            {
                s = ps;
                master.form.P_Image.Image = ps;
                master.form.P_Image.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            }
            g = Graphics.FromImage((Image)s);
            if (s.GetPixel(0, 0).A == 0)
            {
                g.FillRectangle(new SolidBrush(Color.Black), 0, 0, s.Width, s.Height);
            }
        }
        public override void Final()
        {
        }
        public override void Update()
        {
            //Bitmap s = master.preview.GetImage();
            if (master.form.IsAccessible)
            {
                master.form.Invoke((MethodInvoker)delegate ()
                {
                    master.form.P_Image.Image = s;
                    master.form.P_Image.Width = s.Width * 5;
                    master.form.P_Image.Height = s.Height * 5;
                });
            }
        }
        public override Bitmap Draw(Bitmap p)
        {
            return s;
        }
        public override bool WindowEvent(ref Message m)
        {
            return base.WindowEvent(ref m);
        }
        public override void Event(string EventName, object arg)
        {
            switch (EventName)
            {
                case "PanelAccept":
                    Init();
                    break;
                case "P_Fill":
                    {
                        MouseEvent e = (MouseEvent)arg;
                        g.FillRectangle(new SolidBrush(Color.White), 0, 0, s.Width, s.Height);
                        master.preview.Updated = true;
                    }
                    break;
                case "P_Erase":
                    {
                        MouseEvent e = (MouseEvent)arg;
                        g.FillRectangle(new SolidBrush(Color.Black), 0, 0, s.Width, s.Height);
                        master.preview.Updated = true;
                    }
                    break;
                case "P_Mouse_Click":
                    {
                        MouseEvent e = (MouseEvent)arg;
                        Mouse_Clicked = e.Clicked;
                        oldPoint = e.Position;
                        float X = e.Position.X / 5.0f;
                        float Y = e.Position.Y / 5.0f;
                        if (X >= 0 && X < s.Width && Y >= 0 && X < s.Height)
                        {
                            Color buff = s.GetPixel((int)Math.Floor(e.Position.X / 5.0f), (int)Math.Floor(e.Position.Y / 5.0f));
                            Color_Black = (buff.R + buff.G + buff.B) / 3 >= 127;
                            master.preview.Updated = true;
                        }
                    }
                    break;
                case "P_Mouse_Move":
                    {
                        MouseEvent e = (MouseEvent)arg;
                        if (e.Clicked)
                        {
                            g.DrawLine(Color_Black ? penb : penw, oldPoint.X / 5, oldPoint.Y / 5, e.Position.X / 5, e.Position.Y / 5);
                            oldPoint = e.Position;
                            master.preview.Updated = true;
                        }
                    }
                    break;
            }
        }
    }
}
