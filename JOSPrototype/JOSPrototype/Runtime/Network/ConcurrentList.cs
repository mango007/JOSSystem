using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Network
{
    // a buffer for incoming messages. The buffer can be accessed by multiple threads parallelly
    internal class ConcurrentList
    {
        private List<byte[]> msgs = new List<byte[]>();
        private Object listLock = new Object();

        //// if buffer contains a null element, it signals the sender to stop scaning the buffer
        //// party needs to explicitly send "END"
        //public bool IsComplete { get; private set; } = false;
        public int Count { get; private set; }

        public void Add(byte[] msg)
        {
            lock (listLock)
            {
                msgs.Add(msg);
                Count++;
            }
        }

        public List<byte[]> Retrieve()
        {
            List<byte[]> re = new List<byte[]>();
            lock (listLock)
            {
                if (msgs.Count != 0)
                {
                    foreach (var msg in msgs)
                    {
                        //if (msg != null)
                        //{
                            re.Add(msg);
                        //}
                        //else
                        //{
                        //    // send EOP message
                        //    List<byte> eop = new List<byte>();
                        //    eop.AddRange(BitConverter.GetBytes(Int32.MaxValue));
                        //    eop.AddRange(Message.EOPBytes);
                        //    re.Add(eop.ToArray());
                        //}
                    }
                    msgs.Clear();
                    Count = 0;
                }
            }
            return re;
        }
    }
}
