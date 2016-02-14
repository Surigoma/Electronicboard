using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicBoard.Classes.Plugins
{
    public class TestPlugin : Eventer
    {
        Sound input = new ASIO();
        List<float> datas = new List<float>(100000);
        public override void Init()
        {
            base.Init();
            input.Init();
            input.onData += Input_onData;
        }

        private void Input_onData()
        {
            datas.AddRange(input.GetData());
        }

        public override void Final()
        {
            base.Final();
            input.Final();
        }
        public override void Update()
        {
            base.Update();
        }
        Pen p_white = new Pen(Color.White);
        SolidBrush b_black = new SolidBrush(Color.Black);
        float gain = 10.0f;
        public override Bitmap Draw(Bitmap p)
        {
            Graphics g = Graphics.FromImage(p);
            g.FillRectangle(b_black, 0, 0, p.Width, p.Height);
            const float rate = 44100;
            float time = 0.025f;
            int max = (int)Math.Floor((float)rate * time);
            int span = (int)Math.Ceiling((float)max / ((float)p.Width * 10.0f));
            int count = 0;
            float Xs = (float)p.Width / (float)max;
            int index, nextIndex = 0;
            float H = (float)p.Height / 2.0f;

            index = 0;
            for (int readIndex = 0; readIndex + 1 < datas.Count; readIndex++)
            {
                if (count % span == 0)
                {
                    nextIndex ++;
                    g.DrawLine(p_white,
                        p.Width - (float)count * Xs,
                        datas[index] * gain + H,//(((L[index] + R[index]) / 2.0f) * gain) + H,
                        p.Width - (float)(count + span) * Xs,
                        datas[nextIndex] * gain + H//(((L[nextIndex] + R[nextIndex]) / 2.0f) * gain) + H
                        );
                    index = nextIndex;
                }
                count++;
            }
            if(datas.Count > 100000)
            {
                datas.RemoveRange(99999, datas.Count - 100000);
            }
            g.Dispose();
            return p;
        }
    }
}
