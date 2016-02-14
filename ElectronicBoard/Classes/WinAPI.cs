using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Threading;

namespace ElectronicBoard.Classes
{
    public class WinAPI : Sound
    {

        WaveIn waveIn = null;
        Collections.CircularBuffer<float> buffer = null;
        float[] result = null;
        int writeIndex = 0;
        int count = 0;
        public override event onDataD onData;
        private Thread e;
        public override void Init(int DeviceID = 0, int channels = 2, int samplerate = 44100, int bufferMax = (44100 * 3))
        {
            base.Init(DeviceID, channels, samplerate, bufferMax);
            DisposeWaveIn();
            waveIn = new WaveIn() { DeviceNumber = DeviceID };
            waveIn.WaveFormat = new WaveFormat(samplerate, 16, channels);
            waveIn.BufferMilliseconds = 30;
            waveIn.DataAvailable += WaveIn_DataAvailable;
            result = new float[bufferMax];
            buffer = new Collections.CircularBuffer<float>(bufferMax);
            waveIn.StartRecording();
            e = new Thread(new ThreadStart(EventThread));
            Run = true;
            e.Start();
            
        }
        private void DisposeWaveIn()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
                buffer = null;
                writeIndex = 0;
            }
        }
        MicroLibrary.MicroStopwatch s1 = new MicroLibrary.MicroStopwatch();
        MicroLibrary.MicroStopwatch s2 = new MicroLibrary.MicroStopwatch();
        List<long> d1 = new List<long>();
        List<long> d2 = new List<long>();
        bool Run = false;
        bool Event = false;
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            s1.Restart();
            //for (int i = e.Buffer.Length - e.BytesRecorded; i < e.BytesRecorded; i += 2)
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                writeIndex++;
                buffer[writeIndex] = ((float)(((short)(e.Buffer[i + 1] << 8) | (short)e.Buffer[i])) / (float)short.MaxValue);
            }
            count += e.Buffer.Length;
            s1.Stop();
            d1.Add(s1.ElapsedMicroseconds);
            s2.Restart();
            //if (onData != null)
            //    onData();
            Event = true;
            s2.Stop();
            d2.Add(s2.ElapsedMicroseconds);
            if (d1.Count > 100)
            {
                System.Diagnostics.Debug.WriteLine(d1.Average() + "us " + d2.Average() + "us");
                d1.Clear();
                d2.Clear();
            }
        }
        private void EventThread()
        {
            //Run = true;
            while (Run)
            {
                if (Event && onData != null) { onData(); Event = false; }
                Thread.Sleep(1);
            }
        }

        public override void Final()
        {
            Run = false;
            e.Abort();
            DisposeWaveIn();
            base.Final();
        }
        public override float[] GetData()
        {
            int BlockR = count % 2;
            int readIndex = writeIndex - count - BlockR;
            for (int i = 0; i < count; i++)
            {
                result[i] = (buffer[readIndex]);
                readIndex++;
            }
            DataLength = count - BlockR;
            count = BlockR;
            return result;
        }
    }
}
