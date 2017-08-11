using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace JOSPrototype
{
    struct MessageID
    {
        public MessageID(int line, OperationType op)
        {
            this.line = line;
            this.op = op;
        }
        public int line;
        public OperationType op;
    }
}
namespace JOSPrototype.Runtime.Network
{
    internal abstract class Receiver
    {
        protected Party localParty;
        protected Dispatcher dispatcher;
        // message queue for receiving data. Dictionary<party, ConcurrentDictionary<instruction index, received bytes stream>>
        public Dictionary<PartyType, ConcurrentDictionary<MessageID, byte[]>> messageQueue = new Dictionary<PartyType, ConcurrentDictionary<MessageID, byte[]>>();
        // signal receiver to stop listening, and sender to stop scanning the buffer 
        public bool hasReceivedEOP = false;
        // 
        //public Dictionary<PartyType, ConcurrentDictionary<int, AutoResetEvent>> signal = new Dictionary<PartyType, ConcurrentDictionary<int, AutoResetEvent>>();

        public Receiver(Party localParty)
        {
            this.localParty = localParty;
            dispatcher = new Dispatcher(this);
        }

        public abstract void StartReceiver();

        // check if the message for an instruction from a certain party exits in messageQueue
        // only called by client
        public byte[] ReceiveFrom(PartyType from, MessageID id)
        {
            System.Diagnostics.Debug.Assert(localParty.Type == PartyType.Client);
            System.Diagnostics.Debug.Assert(id.line == int.MaxValue);
            System.Diagnostics.Debug.Assert(id.op == OperationType.Return);
            byte[] re;
            // var queue = messageQueue[from].GetOrAdd(index, new ConcurrentQueue<byte[]>());
            while (!messageQueue[from].TryRemove(id, out re)) { Thread.Sleep(1); }
            //var sig = signal[from].GetOrAdd(index, new AutoResetEvent(false));
            //sig.WaitOne();
            //queue.TryDequeue(out re);
            return re;
        }
        // pass a "receive" resuest to dispatcher, 
        // when the message is received, the operation will be called to cuntinue the execution
        public void ReceiveFrom(PartyType from, int index, Operation.Operation op, NumericArray result)
        {
            dispatcher.Add(from, index, op, result);
            
        }

        //public void InsertQueue(int index)
        //{
        //    foreach (var entry in messageQueue)
        //    {
        //        if(entry.Key != PartyType.Client)
        //            entry.Value.AddOrUpdate(index, new ConcurrentQueue<byte[]>(), (k, v)=> new ConcurrentQueue<byte[]>());
        //    }
        //}

        // when one instruction is completed, remove the entry for that instruction from the buffer
        //public void RemoveQueue(int index)
        //{
        //    ConcurrentQueue<byte[]> temp;
        //    foreach (var entry in messageQueue)
        //    {
        //        if (entry.Key != PartyType.Client)
        //            entry.Value.TryRemove(index, out temp);
        //    }

        //    /*Stopwatch watch = new Stopwatch();
        //    watch.Start();
        //    while (watch.ElapsedMilliseconds < GlobalSetting.simulated_networkLatency) { };
        //    watch.Stop();*/
        //}
    }

    internal class ReceiverInternet: Receiver
    {
        // constructor
        public ReceiverInternet(Dictionary<PartyType, IPEndPoint> ipAddressesAndPort, Party localParty)
            : base(localParty)
        {
            foreach (var entry in ipAddressesAndPort)
            {
                partyIPMap.Add(entry.Key, entry.Value);
                if (entry.Key != localParty.Type)
                {
                    messageQueue.Add(entry.Key, new ConcurrentDictionary<MessageID, byte[]>());
                }
            }
        }

        public override void StartReceiver()
        {
            Thread thread = new Thread(StartListening);
            thread.Name = localParty.Type + "_Receiver";
            thread.Start();
        }

        // State object for reading client data asynchronously
        private class ReceiverState
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data stream.
            public List<byte> data = new List<byte>();
        }
        // map PartyType to ip address and port  
        private Dictionary<PartyType, IPEndPoint> partyIPMap = new Dictionary<PartyType, IPEndPoint>();

        // Thread signal.
        private ManualResetEvent allDone = new ManualResetEvent(false);

        private void StartListening()
        {
            // Establish the remote endpoint for the socket.
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(partyIPMap[localParty.Type]);
                listener.Listen(100);

                while (!hasReceivedEOP)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    // Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                    // Wait until a connection is made before continuing.
                    
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            //Console.WriteLine("\nPress ENTER to continue...");
            //Console.Read();

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            ReceiverState state = new ReceiverState();
            state.workSocket = handler;

            handler.BeginReceive(state.buffer, 0, ReceiverState.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            ReceiverState state = (ReceiverState)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.data.AddRange(state.buffer.SubArray(0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data. EOF := FFFFFFFF
                if (Message.ContainEOF(state.data))
                {
                    Message.RemoveEOF(state.data);
                    // All the data has been read from the 
                    // client. Display it on the console.
                    // Console.WriteLine("" + localParty.Type + " reads {0} bytes from" + state.workSocket.RemoteEndPoint.ToString(), state.data.Count());

                    // unpack the message
                    byte[] dataByte = new byte[state.data.Count];
                    PartyType from;
                    for (int i = 0; i < state.data.Count; ++i) { dataByte[i] = state.data[i]; }
                    var msgs = Message.Unpack(out from, dataByte);

                    // check if the message is "END"
                    //if (from == PartyType.Client && msgs.Count() == 1 && )
                    //{
                    //    foreach (var entry in msgs)
                    //    {
                    //        if(Message.IsEndOfProgram(entry.Value))
                    //        {
                    //            hasReceivedEOP = true;
                    //            // socket listener is blocked while listening, set allDone to allow while loop to continue
                    //            allDone.Set();
                    //            return;
                    //        }
                    //    }
                    //}

                    foreach (var line in msgs.Keys)
                    {
                        foreach (var msg in msgs[line])
                        {
                            OperationType op = (OperationType)BitConverter.ToInt16(msg, 0);
                            bool invokeHelper = BitConverter.ToBoolean(msg, sizeof(short));
                            byte[] newMsg = msg.SubArray(sizeof(short) + sizeof(bool));

                            var hasBeenAdded = messageQueue[from].TryAdd(new MessageID(line, op), newMsg);
                            System.Diagnostics.Debug.Assert(hasBeenAdded == true);
                            //var queue = messageQueue[from].GetOrAdd(line, new ConcurrentQueue<byte[]>());
                            //queue.Enqueue(newMsg);

                            //var sig = receiver.signal[from].GetOrAdd(line, new AutoResetEvent(true));
                            //sig.Set();

                            if (localParty.Type == PartyType.Helper && invokeHelper)
                            {
                                ((Helper)localParty).RunParty(line, op);
                            }
                        }
                    }
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, ReceiverState.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }
    }

    internal class ReceiverLocal : Receiver
    {
        public ReceiverLocal(List<Party> parties, Party localParty)
            : base(localParty)
        {
            foreach (var party in parties)
            {
                if(party.Type != localParty.Type)
                {
                    messageQueue.Add(party.Type, new ConcurrentDictionary<MessageID, byte[]>());
                    //signal.Add(party.Type, new ConcurrentDictionary<int, AutoResetEvent>());
                }
            }
        }

        public override void StartReceiver()
        {
            if (localParty.Type != PartyType.Client)
            {
                Thread thread = new Thread(dispatcher.Run);
                thread.Name = localParty.Type + "_Receiver";
                thread.Start();
            }
        }

        public class SimulateNetworkState
        {
            public SimulateNetworkState(ReceiverLocal receiver, Helper helper, byte[] data)
            {
                this.receiver = receiver;
                this.helper = helper;
                this.data = data;
            }
            internal ReceiverLocal receiver;
            internal Helper helper;
            internal byte[] data;
        }
        public static void SimulateNetwork(object obj)
        {
            // simulate the network delay
            Thread.Sleep(Config.Simulated_Network_Latency);

            var state = (SimulateNetworkState)obj;
            var receiver = state.receiver;
            var helper = state.helper;
            var data = state.data;       
            // remove end of file symbol
            data = Message.RemoveEOF(data);
            PartyType from;
            var msgs = Message.Unpack(out from, data);
            foreach (var line in msgs.Keys)
            {
                foreach(var msg in msgs[line])
                {
                    OperationType op = (OperationType)BitConverter.ToInt16(msg, 0);
                    bool invokeHelper = BitConverter.ToBoolean(msg, sizeof(short));

                    byte[] newMsg = msg.SubArray(sizeof(short) + sizeof(bool));
                    var hasBeenAdded = receiver.messageQueue[from].TryAdd(new MessageID(line, op), newMsg);
                    System.Diagnostics.Debug.Assert(hasBeenAdded == true);
                    //var queue = receiver.messageQueue[from].GetOrAdd(line, new ConcurrentQueue<byte[]>());
                    //queue.Enqueue(newMsg);
                    //Console.WriteLine("to: " + receiver.localParty.Type + ", from: " + from + ", index: " + line);
                    if (receiver.localParty.Type == PartyType.Helper && invokeHelper)
                    {
                        helper.RunParty(line, op);
                    }
                }
            }
        }
    }
}
