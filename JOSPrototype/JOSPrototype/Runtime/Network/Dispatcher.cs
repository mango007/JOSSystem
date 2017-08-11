using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Network
{
    internal class Dispatcher
    {
        public Dispatcher(Receiver receiver)
        {
            this.receiver = receiver;
            queue = new ConcurrentQueue<QueueEntry>();
        }
        public void Add(PartyType from, int index, Operation.Operation op, NumericArray result)
        {
            queue.Enqueue(new QueueEntry(from, index, op, result));
        }
        public void Run()
        {
            while(!receiver.hasReceivedEOP)
            {
                QueueEntry entry;
                // check each entry in the dispacher queue
                if(queue.TryDequeue(out entry))
                {
                    //var msgq = receiver.messageQueue[entry.from].GetOrAdd(entry.line, new ConcurrentQueue<byte[]>());
                    byte[] msg;
                    // if message has been received
                    if(receiver.messageQueue[entry.from].TryRemove(new MessageID(entry.line, entry.op.opType), out msg))
                    {
                        // store results in specified address
                        entry.result.SetArray(Message.DisassembleMessage(msg));
                        // continue the operation                       
                        entry.op.Run();
                    }
                    else
                    {
                        // enqueue the entry such that it will be checked again later
                        queue.Enqueue(entry);
                    }
                }
            }
        }
        private class QueueEntry
        {
            public QueueEntry(PartyType from, int line, Operation.Operation op, NumericArray result)
            {
                this.from = from;
                this.line = line;
                this.op = op;
                this.result = result;
            }
            public PartyType from;
            public int line;
            public Operation.Operation op;
            public NumericArray result;
        }
        private Receiver receiver;
        private ConcurrentQueue<QueueEntry> queue;
    }
}
