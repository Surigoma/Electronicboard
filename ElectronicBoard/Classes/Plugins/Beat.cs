using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Windows.Forms;
using System.Threading;

namespace ElectronicBoard.Classes.Plugins
{
    public class Beat : Eventer
    {
        WaveIn waveIn;
        const int rate = 8000;
        const int BuffMax = rate * 3;
        int writeIndex = 0;
        int Buffering = 0;
        float[] Buffer = new float[BuffMax];
        FilterBuffer Buffer_Low, Buffer_Mid, Buffer_High;
        FilterParam Param_Low, Param_Mid, Param_High;
        bool Beet = false;
        private struct FilterBuffer
        {
            public float[] input;
            public float[] output;
        };
        private struct FilterParam
        {
            public float[] a;
            public float[] b;
        };
        private enum FilterType
        {
            LowPass,
            BandPass,
            HighPass
        };

        public override void Init()
        {
            base.Init();
            writeIndex = 0;
            Buffering = 0;
            waveInDispose();
            waveIn = new WaveIn()
            {
                DeviceNumber = master.form.SoundList.SelectedIndex,
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.BufferMilliseconds = 14;
            waveIn.WaveFormat = new WaveFormat(rate, 16, 2);
            waveIn.StartRecording();
            Buffer_Low = CreateFilterBuffer();
            Buffer_Mid = CreateFilterBuffer();
            Buffer_High = CreateFilterBuffer();
            Param_Low = CreateFilterParam(FilterType.LowPass, 800, 3f);
            Param_Mid = CreateFilterParam(FilterType.BandPass, 2000, 0.1f);
            Param_High = CreateFilterParam(FilterType.HighPass, 8000, 2f);
        }
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                Buffer[writeIndex] = (float)((short)((e.Buffer[i + 1] << 8) | e.Buffer[i]) + (short)((e.Buffer[i + 3] << 8) | e.Buffer[i + 2])) / 16384f;
                writeIndex++;
                Buffering++;
                if (writeIndex >= BuffMax) { writeIndex = 0; }
            }
        }
        public override void Final()
        {
            waveInDispose();
        }
        private void waveInDispose()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
        }
        public override void About()
        {
            base.About();
        }
        public override Bitmap Draw(Bitmap p)
        {
            return base.Draw(p);
        }
        public override void Event(string EventName, object arg)
        {
            base.Event(EventName, arg);
        }
        public override void Update()
        {
            if (Buffering > 0)
            {
                float input = 0;
                float l, m, h;
                float sl = 0, sm = 0, sh = 0;
                int b = Buffering;
                int readIndex = writeIndex - Buffering;
                readIndex = readIndex < 0 ? (BuffMax - 1) + readIndex : readIndex;
                for (int i = 0; i < Buffering; i++)
                {
                    input = Buffer[readIndex];
                    l = BiquadFilter(Param_Low, ref Buffer_Low, input);
                    m = BiquadFilter(Param_Mid, ref Buffer_Mid, input);
                    h = BiquadFilter(Param_High, ref Buffer_High, input);
                    sl += (float)Math.Abs(l);
                    sm += (float)Math.Abs(m);
                    sh += (float)Math.Abs(h);
                    readIndex++;
                    readIndex = readIndex >= BuffMax ? 0 : readIndex;
                    Buffering--;
                }
                master.form.Invoke((MethodInvoker)delegate ()
                {
                    l = (float)Math.Abs(sl / (float)b);
                    m = (float)Math.Abs(sm / (float)b);
                    h = (float)Math.Abs(sh / (float)b);
                    master.form.progressBar1.Value = (int)((l > 1 ? 1.0f : l) * 100.0f);
                    master.form.progressBar2.Value = (int)((m > 1 ? 1.0f : m) * 100.0f);
                    master.form.progressBar3.Value = (int)((h > 1 ? 1.0f : h) * 100.0f);
                });
            }
        }
        private FilterParam CreateFilterParam(FilterType type, float freq, float Q)
        {
            FilterParam result = new FilterParam();
            result.a = new float[3];
            result.b = new float[3];
            float omega = 2.0f * (float)Math.PI * freq / Q;
            float alpha = (float)Math.Sin(omega) / Q;
            float cosOmega = (float)Math.Cos(omega);
            switch (type)
            {
                case FilterType.LowPass:
                    result.b[1] = 1.0f - cosOmega;
                    result.b[0] = result.b[1] / 2.0f;
                    result.b[2] = result.b[0];
                    result.a[0] = 1.0f + alpha;
                    result.a[1] = -2.0f * cosOmega;
                    result.a[2] = 1.0f - alpha;
                    break;
                case FilterType.BandPass:
                    result.b[0] = Q * alpha;
                    result.b[1] = 0;
                    result.b[2] = -1.0f * result.b[0];
                    result.a[0] = 1.0f + alpha;
                    result.a[1] = -2.0f * cosOmega;
                    result.a[2] = 1.0f - alpha;
                    break;
                case FilterType.HighPass:
                    result.b[1] = 1.0f + cosOmega;
                    result.b[0] = result.b[1] / 2.0f;
                    result.b[1] *= -1.0f;
                    result.b[2] = result.b[0];
                    result.a[0] = 1.0f + alpha;
                    result.a[1] = -2.0f * cosOmega;
                    result.a[2] = 1.0f - alpha;
                    break;
            }
            return result;
        }
        private FilterBuffer CreateFilterBuffer()
        {
            return new FilterBuffer() { input = new float[2], output = new float[2] };
        }
        private float BiquadFilter(FilterParam param, ref FilterBuffer buffer, float input)
        {
            float result = (param.b[0] / param.a[0]) * input
                            + (param.b[1] / param.a[0]) * buffer.input[0]
                            + (param.b[2] / param.a[0]) * buffer.input[1]
                            - (param.a[1] / param.a[0]) * buffer.output[0]
                            - (param.a[2] / param.a[0]) * buffer.output[1];
            if (float.IsNaN(result))
            {
                result = 0;
            }
            if(float.IsInfinity(result))
            {
                result = 0;
            }
            buffer.input[1] = buffer.input[0];
            buffer.input[0] = input;
            buffer.output[1] = buffer.output[0];
            buffer.output[0] = result;
            return result;
        }

        private float[] BiquadLowPassFilter(FilterParam param, float[] Source)
        {
            float[] result = new float[Source.Length];
            float in1 = 0, in2 = 0, out1 = 0, out2 = 0;
            for (int i = 0; i < Source.Length - 1; i++)
            {
                result[i] = (param.b[0] / param.a[0]) * Source[i]
                            + (param.b[1] / param.a[0]) * in1
                            + (param.b[2] / param.a[0]) * in2
                            - (param.a[1] / param.a[0]) * out1
                            - (param.a[2] / param.a[0]) * out2;
                in2 = in1;
                in1 = Source[i];
                out2 = out1;
                out1 = result[i];
            }
            return result;
        }
    }
}
