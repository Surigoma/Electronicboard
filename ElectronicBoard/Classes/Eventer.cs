using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace ElectronicBoard.Classes
{
    abstract public class Eventer
    {
        public uint span { get; protected set; }
        protected EventMaster master = EventMaster.GetInstance();
        protected EventMaster.PlayStyle style = EventMaster.PlayStyle.Null;
        public Eventer()
        {
            //Init();
            span = 16667;  // 60fps
            //master.EventAdd(style, this);
        }
        ~Eventer()
        {
            Final();
            master.EventRemove(style);
        }
        virtual public void Init() { }
        virtual public void Final() { }
        virtual public void About() { }
        virtual public void Update() { }
        virtual public bool WindowEvent(ref Message m) { return false; }
        virtual public void Event(string EventName, object arg) { return; }
        virtual public Bitmap Draw(Bitmap p) { return null; }
    }
}
