using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace ElectronicBoard.Classes.Plugins
{
    public class Scroll : Eventer
    {
        int X = 0;
        Font font = new Font("MS ゴシック", 11);
        SolidBrush white = new SolidBrush(Color.White);
        SolidBrush black = new SolidBrush(Color.Black);
        public override void Init()
        {
            X = master.preview.GetImage().Width;
            base.style = EventMaster.PlayStyle.Scroll;
        }
        public override void Final()
        {
        }
        public override void About()
        {
        }
        public override bool WindowEvent(ref Message m)
        {
            return base.WindowEvent(ref m);
        }
        public override void Update()
        {
            MainForm f = master.form;
            int Adder = 0;
            Size renderSize = new Size();
            int width = 0;
            bool loop = false;
            //if (master.form.IsAccessible)
            {
                master.form.Invoke((MethodInvoker)delegate ()
                {
                    span = 1000000 / (uint)f.S_Interval.Value;
                    Adder = (int)f.S_Adder.Value;
                    renderSize = TextRenderer.MeasureText(f.S_Text.Text, font);
                    width = master.preview.GetImage().Width;
                    loop = master.form.S_Looped.Checked;
                    if (font.Size != (float)master.form.S_Size.Value)
                    {
                        font = new Font("MS ゴシック", (float)master.form.S_Size.Value);
                    }
                });
                master.preview.Updated = true;
            }
            if (loop)
            {
                if (Adder > 0 && X > width + 16)
                {
                    X = renderSize.Width * -1;
                }
                if (Adder <= 0 && X < -1 * (renderSize.Width + 16))
                {
                    X = width + 16;
                }
            }
            else if ((Adder > 0 && X > width + 16) || (Adder <= 0 && X < -1 * (renderSize.Width + 16)))
            {
                master.Stop();
            }
            /*if (X < -1 * renderSize.Width + 16 || X > width)
            {
                X = (Adder > 0 ? X = renderSize.Width * -1 : X = width + 16);
            }*/
            X += Adder;
            /*
            try
            {
                //timer1.Interval = trackBar1.Value;
                this.Invoke((MethodInvoker)delegate()
                {
                    timer1.Interval = trackBar2.Value * 10000;
                    var result = pw.GetImage();
                    var s = TextRenderer.MeasureText(textBox1.Text, f);
                    Graphics g = Graphics.FromImage(result);
                    //g.DrawRectangle(new Pen(new SolidBrush(Color.Black)), 0, 0, result.Width, result.Height);
                    g.FillRectangle(Brushes.Black, 0, 0, result.Width, result.Height);
                    g.DrawString(textBox1.Text, f, new SolidBrush(Color.White), new PointF(ScrollX, -2));
                    //g.DrawImage(b, new Point(ScrollX, 0));
                    g.Dispose();
                    pw.SetImage(result);
                    ScrollX -= trackBar1.Value;
                    if (ScrollX < (s.Width + 16) * -1 && trackBar1.Value > 0)
                    {
                        if (checkBox1.Checked)
                            ScrollX = Display * 16;
                        else
                            timer1.Stop();
                    }
                    if (ScrollX > (Display * 16) + 1 && trackBar1.Value < 0)
                    {
                        if (checkBox1.Checked)
                            ScrollX = (s.Width + 16) * -1;
                        else
                            timer1.Stop();
                    }
                });
            }
            catch (Exception) { }
             * */
        }
        public override Bitmap Draw(Bitmap p)
        {
            MainForm f = master.form;
            string text = "";
            float Y = 0;
            master.form.Invoke((MethodInvoker)delegate()
            {
                text = f.S_Text.Text;
                Y = (float)f.S_Y.Value;
            });
            Graphics g = Graphics.FromImage(p);
            g.FillRectangle(black, 0, 0, p.Width, p.Height);
            g.DrawString(text, font, white, X, Y);
            g.Dispose();
            return p;
        }
        public override void Event(string EventName, object arg)
        {
            base.Event(EventName, arg);
        }
    }
}
