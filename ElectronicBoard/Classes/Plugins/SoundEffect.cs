using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Windows.Forms;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace ElectronicBoard.Classes.Plugins
{

    public class SoundEffect : Eventer
    {
        Sound input;
        const int rate = 44100;
        const int BuffMax = rate * 1;
        //float[] L = new float[BuffMax];
        //float[] R = new float[BuffMax];
        Collections.CircularBuffer<float> L = new Collections.CircularBuffer<float>(BuffMax);
        Collections.CircularBuffer<float> R = new Collections.CircularBuffer<float>(BuffMax);
        int writeIndex = 0;
        Pen p_white = new Pen(Color.White);
        SolidBrush b_black = new SolidBrush(Color.Black);
        SolidBrush b_white = new SolidBrush(Color.White);

        int sample;
        Complex[] datas = null;
        double[] window = null;

        private enum Types
        {
            Left,
            Center,
            FFT,
            Effect1
        };

        public override void Init()
        {
            base.Init();
            if(input!=null)
            {
                input.Final();
                input = null;
            }
            switch (master.form.SoundType.SelectedIndex)
            {
                case 0:
                    input = new WinAPI();
                    break;
                case 1:
                    input = new ASIO();
                    break;
                default:
                    break;
            }
            writeIndex = 0;
            input.Init(master.form.SoundList.SelectedIndex,2,rate,BuffMax * 2);
            input.onData += Input_onData;
            sample = (int)(rate * time);
            datas = new Complex[sample];
            window = MathNet.Numerics.Window.Hamming(sample);
        }
        bool ArrayChanged = false;
        public override void Event(string EventName, object arg)
        {
            switch(EventName)
            {
                case "V_Time_Changed":
                    ArrayChanged = true;
                    sample = (int)(rate * time);
                    window = MathNet.Numerics.Window.Hamming(sample);
                    datas = new Complex[sample];
                    break;
                default:
                    break;
            }
        }
        List<float> List = new List<float>();
        int def = 0;
        private void Input_onData()
        {
            float[] result = input.GetData();
            for (int i = 0; i < input.DataLength; i += 2)
            {
                writeIndex++;
                L[writeIndex] = result[i];
                R[writeIndex] = result[i];
                /*
                writeIndex++;
                if (writeIndex >= BuffMax) { writeIndex = 0; }
                L[writeIndex] = result[i];
                R[writeIndex] = result[i + 1];
                */
            }
            def += input.DataLength;
        }

        public override void Final()
        {
            if(input != null)
                input.Final();
        }
        public override void About()
        {
            base.About();
        }
        float gain = (float)Math.Pow(10.0f, 1.5f);
        float time = 0.025f;
        int mode = 0;

        public override Bitmap Draw(Bitmap p)
        {
            Graphics g = Graphics.FromImage(p);
            g.FillRectangle(b_black, 0, 0, p.Width, p.Height);
            def = 0;
            switch ((Types)mode)
            {
                case Types.Left:
                    {
                        int sample = (int)(rate * time);
                        int readIndex = writeIndex;
                        double sum_l = 0, sum_r = 0;
                        float h = p.Height / 2.0f;
                        for (int count = 0; count < sample; count++)
                        {
                            sum_l += Math.Abs(L[readIndex - def]);
                            sum_r += Math.Abs(R[readIndex - def]);
                            readIndex--;
                            //if (readIndex < 0) { readIndex = BuffMax - 1; }
                        }
                        sum_l = sum_l / sample;
                        sum_r = sum_r / sample;
                        g.FillRectangle(b_white, 0, 0, (float)sum_l * gain * p.Width, h);
                        g.FillRectangle(b_white, 0, h, (float)sum_r * gain * p.Width, p.Height);
                    }
                    break;
                case Types.Center:
                    {
                        int sample = (int)(rate * time);
                        int readIndex = writeIndex;
                        double sum_l = 0, sum_r = 0;
                        float h = (float)p.Width / 2.0f;
                        for (int count = 0; count < sample; count++)
                        {
                            sum_l += Math.Abs(L[readIndex - def]);
                            sum_r += Math.Abs(R[readIndex - def]);
                            readIndex--;
                            //if (readIndex < 0) { readIndex = BuffMax - 1; }
                        }
                        sum_l = sum_l / sample;
                        sum_r = sum_r / sample;
                        float LWidth = (float)sum_l * gain * h;
                        g.FillRectangle(b_white, h - LWidth, 0, LWidth, p.Height);
                        g.FillRectangle(b_white, h, 0, (float)sum_r * gain * h, p.Height);
                    }
                    break;
                case Types.FFT:
                    {
                        int readIndex = writeIndex - sample;
                        if (readIndex < 0) { readIndex = readIndex + (BuffMax - 1); }
                        for (int count = 0; count < sample; count++)
                        {
                            if (ArrayChanged) { break; }
                            datas[count] = new Complex((float)window[count] * gain * (L[readIndex - def] + R[readIndex - def]) / 2.0f, 0);
                            readIndex++;
                            //if (readIndex >= BuffMax) { readIndex = 0; }
                        }
                        Fourier.Inverse(datas);
                        float x = 0;
                        float width, height;
                        for (int count = 0; count < sample / 2; count++)
                        {
                            if (ArrayChanged) { break; }
                            float d = (float)Math.Log10(((float)(count + 1) * 2.0f / (float)sample) * 0.9f + 0.1f) - (float)Math.Log10(((float)(count) * 2.0f / (float)sample) * 0.9f + 0.1f);
                            //(float)Math.Log10((float)(count+1) * 2.0f / (float)sample) - (float)Math.Log10((float)(count+0.001f) * 2.0f / (float)sample);//(float)Math.Log10(((float)(sample / 2 - count) / (float)sample * 2.0f) * 9.0f + 1.0f) * 1.5f;
                            width = d * (float)p.Width;
                            height = (float)Math.Log10((float)datas[count].Magnitude) * (float)p.Height;
                            height = height > p.Height ? p.Height : height;
                            g.FillRectangle(b_white, x, (float)p.Height - height, width, height);
                            x += width;
                        }
                        ArrayChanged = false;
                        //System.Diagnostics.Debug.WriteLine(x);
                    }
                    break;
                case Types.Effect1:
                    {
                        int max = (int)Math.Floor((float)rate * time);
                        int span = (int)Math.Ceiling((float)max / ((float)p.Width * 10.0f));
                        int count = 0;
                        float Xs = (float)p.Width / (float)max;
                        int index, nextIndex = 0;
                        float H = (float)p.Height / 2.0f;

                        index = writeIndex;
                        for (int readIndex = writeIndex; count < BuffMax && count < max; readIndex--)
                        {
                            //if (readIndex < 0) { readIndex = BuffMax - 1; }
                            if (count % span == 0)
                            {
                                nextIndex = readIndex - 1;
                                g.DrawLine(p_white,
                                    p.Width - (float)count * Xs,
                                    L[index - def] * gain + H,//(((L[index] + R[index]) / 2.0f) * gain) + H,
                                    p.Width - (float)(count + span) * Xs,
                                    L[nextIndex - def] * gain + H//(((L[nextIndex] + R[nextIndex]) / 2.0f) * gain) + H
                                    );
                                index = nextIndex;
                            }
                            count++;
                        }
                    }
                    break;
            }
            master.preview.Updated = true;
            g.Dispose();
            return p;
        }
        public override void Update()
        {
            //if (master.form.IsAccessible)
            {
                master.form.Invoke((MethodInvoker)delegate()
                {
                    gain = (float)Math.Pow(10.0f, ((float)master.form.V_Gain.Value / 10.0f));
                    time = (float)master.form.V_Time.Value;
                    mode = master.form.V_Type.SelectedIndex;
                });
            }
        }
    }
}
