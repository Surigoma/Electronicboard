using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicBoard.Classes
{
    public class Sound
    {
        public delegate void onDataD();
        public virtual event onDataD onData;
        protected int DeviceID, Channels, SampleRate, bufferMax;
        public int DataLength = 0;
        public virtual void Init(int DeviceID=0,int channels = 2,int samplerate = 44100, int bufferMax = (44100*3))
        {
            this.DeviceID = DeviceID;
            Channels = channels;
            SampleRate = samplerate;
            this.bufferMax = bufferMax;
        }
        public virtual void Final()
        {

        }
        public virtual float[] GetData()
        {
            return null;
        }
    }
}
