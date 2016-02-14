using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;

namespace ElectronicBoard.Classes
{
    public class ASIO : Sound
    {
        public override event onDataD onData;
        AsioOut input;
        float[] buffer = null;
        float[] result = null;
        int[][] bytebuffer = null;
        int[][] ASIOBuffer = null;
        List<long> timerlog = new List<long>();
        int ABLenght = 0;
        int writeIndex = 0, count = 0, bwriteIndex = 0, bcount = 0;
        int bbufferMax = 0;
        System.Timers.Timer tick = new System.Timers.Timer(1000f / 60f);
        public override void Init(int DeviceID = 0, int channels = 2, int samplerate = 44100, int bufferMax = (44100 * 3))
        {
            base.Init(DeviceID, channels, samplerate, bufferMax);
            DisposeAsio();
            buffer = new float[bufferMax];
            result = new float[bufferMax];
            bcount = 0;
            bwriteIndex = 0;
            input = new AsioOut(DeviceID);
            //provider = new BufferedWaveProvider(new WaveFormat(samplerate, channels));
            input.InitRecordAndPlayback(null, channels, samplerate);
            //input.ShowControlPanel();
            input.AudioAvailable += Input_AudioAvailable;
            System.Diagnostics.Debug.WriteLine(input.PlaybackLatency + "ms");
            input.Play();
            tick.AutoReset = true;
            tick.Elapsed += Tick_Elapsed;
            tick.Start();
        }

        private void Tick_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (bcount > 0)
            {
                int readindex = bwriteIndex - bcount;
                if (readindex < 0) { readindex = (readindex * -1) % bbufferMax; }
                //int ii = writeIndex - 1;
                for (int i = 0; i < bcount; i += 1)
                {
                    for (int c = 0; c < base.Channels; c++)
                    {
                        buffer[writeIndex] = (float)((int)bytebuffer[c][readindex]) / (float)int.MaxValue;
                        //ii = writeIndex - 1;
                        //ii = ii < 0 ? buffer.Length - 1 : ii;
                        count++;
                        writeIndex++;
                        if (writeIndex >= buffer.Length) { writeIndex = 0; }
                        readindex += 1;
                        if (readindex >= bbufferMax)
                        {
                            //System.Diagnostics.Debug.WriteLine("readIndex Reflesh");
                            readindex = 0;
                        }
                    }
                }
                //System.Diagnostics.Debug.WriteLine(bcount);
                bcount = 0;
                if (onData != null)
                {
                    onData();
                }
                try
                {
                    if (timerlog.Count > 0)
                    {
                        //System.Diagnostics.Debug.WriteLine(timerlog.Count + "count" + timerlog.Average() + "us");
                        timerlog.Clear();
                    }
                }
                catch (Exception)
                { }
            }
        }

        static int ReverseBits(int i)
        {
            uint v = (uint)(i & 0x7fffffff);
            v = ((v & 0xaaaaaaaa) >> 1) | ((v & 0x55555555) << 1);
            v = ((v & 0xcccccccc) >> 2) | ((v & 0x33333333) << 2);
            v = ((v & 0xf0f0f0f0) >> 4) | ((v & 0x0f0f0f0f) << 4);
            v = ((v & 0xff00ff00) >> 8) | ((v & 0x00ff00ff) << 8);
            v = (v >> 16) | (v << 16);
            return (int)(v * (i < 0 ? -1 : 1));
        }
        static float Int32LSBToFloat(int i)
        {
            return (float)i / (float)int.MaxValue;
        }
        static float Int32MSBToFloat(int i)
        {
            return (float)ReverseBits(i) / int.MaxValue;
        }

        int over = 0, first = 0;
        private void createByteBuffer(int sampleperbuffer)
        {
            bytebuffer = null;
            bytebuffer = new int[Channels][];
            bbufferMax = sampleperbuffer * 10;
            for (int c = 0; c < Channels; c++)
            {
                bytebuffer[c] = new int[bbufferMax];
            }
        }
        MicroLibrary.MicroStopwatch timer = new MicroLibrary.MicroStopwatch();
        private void Input_AudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            timer.Restart();
            if (bytebuffer == null)
            {
                createByteBuffer(e.SamplesPerBuffer);
            }
            over = bbufferMax - bwriteIndex - e.SamplesPerBuffer;
            first = e.SamplesPerBuffer + (over < 0 ? over : 0);
            for (int i = 0; i < e.InputBuffers.Length; i++)
            {
                Marshal.Copy(e.InputBuffers[i], bytebuffer[i], bwriteIndex, first);
            }
            bwriteIndex += first;
            if (bwriteIndex >= bbufferMax) { bwriteIndex -= bbufferMax; }
            if (over < 0)
            {
                for (int i = 0; i < e.InputBuffers.Length; i++)
                {
                    Marshal.Copy(e.InputBuffers[i], bytebuffer[i], bwriteIndex, over * -1);
                }
                bwriteIndex -= over;
            }
            bcount += e.SamplesPerBuffer;
            timer.Stop();
            timerlog.Add(timer.ElapsedMicroseconds);
        }

        private void DisposeAsio()
        {
            try
            {
                if (input != null)
                {
                    if (input.PlaybackState != PlaybackState.Stopped)
                    {
                        input.Stop();
                    }
                    input.Dispose();
                    input = null;
                }
            }
            catch (NullReferenceException)
            { }
        }
        public override float[] GetData()
        {
            if (count > buffer.Length) { count = buffer.Length - 1; }
            int readIndex = writeIndex - count;
            if (readIndex < 0) { readIndex += buffer.Length; }
            for (int i = 0; i <= count; i++)
            {
                result[i] = (buffer[readIndex]);
                readIndex++;
                if (readIndex >= buffer.Length) { readIndex = 0; }
            }
            DataLength = count;
            count = 0;
            return result;
        }
        public override void Final()
        {
            DisposeAsio();
            base.Final();
        }
    }
}
