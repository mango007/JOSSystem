namespace JOSPrototype.Runtime.Network
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Diagnostics;

    internal abstract class Sender
    {
        // the local party
        protected Party localParty;
        
        // Receiver
        protected Receiver receiver;

        public Sender(Party localParty, Receiver receiver)
        {
            this.localParty = localParty;
            this.receiver = receiver;
        }

        public abstract void SendTo(PartyType party, byte[] msg);
        public abstract void StartSender();
        //public abstract bool HasSentAllMessages();
    }

    internal class SenderInternet: Sender
    {
        // constructor
        public SenderInternet(Dictionary<PartyType, IPEndPoint> partyAddress, Party localParty, ReceiverInternet receiver)
            :base(localParty, receiver)
        {
            this.partyIPMap = partyAddress;
            foreach (var party in partyAddress.Keys)
            {
                if (party != localParty.Type)
                {
                    msgBuffer.Add(party, new ConcurrentList());
                    connectDone.Add(party, new ManualResetEvent(false));
                    sendDone.Add(party, new ManualResetEvent(false));
                }
            }
            // client need to send EOP message to itself
            if (localParty.Type == PartyType.Client)
            {
                msgBuffer.Add(PartyType.Client, new ConcurrentList());
                connectDone.Add(PartyType.Client, new ManualResetEvent(false));
                sendDone.Add(PartyType.Client, new ManualResetEvent(false));
            }
        }

        // message sent to a party is buffered first
        public override void SendTo(PartyType party, byte[] msg)
        {
            msgBuffer[party].Add(msg);
        }

        public override void StartSender()
        {
            Thread thread = new Thread(FlushBuffer);
            thread.Name = localParty.Type + "_Sender";
            thread.Start();
        }

        //// check if the buffer is empty
        //public override bool HasSentAllMessages()
        //{
        //    foreach(var val in msgBuffer.Values)
        //    {
        //        if (val.Count != 0)
        //            return false;
        //    }
        //    return true;
        //}

        // state for asynchronize call
        private class SenderState
        {
            public SenderState(Socket socket, PartyType to)
            {
                Socket = socket;
                To = to;
            }
            public Socket Socket { get; private set; }
            public PartyType To { get; private set; }
        }

       
        // The ip address and port number for all parties.
        private Dictionary<PartyType, IPEndPoint> partyIPMap;
        // massage buffer, gather messages sent to one party as many as possible
        private Dictionary<PartyType, ConcurrentList> msgBuffer = new Dictionary<PartyType, ConcurrentList>();
        // ManualResetEvent instances signal completion.
        private Dictionary<PartyType, ManualResetEvent> connectDone = new Dictionary<PartyType, ManualResetEvent>();
        private Dictionary<PartyType, ManualResetEvent> sendDone = new Dictionary<PartyType, ManualResetEvent>();

        // one thread is constantly scan the buffer for all parties
        private void FlushBuffer()
        {
            Dictionary<PartyType, Stopwatch> stopwatch = new Dictionary<PartyType, Stopwatch>();
            foreach (var party in msgBuffer.Keys)
            {
                stopwatch.Add(party, new Stopwatch());
                stopwatch[party].Start();
            }
            while (!receiver.hasReceivedEOP)
            {
                foreach (var party in msgBuffer.Keys)
                {
                    bool reachBufferThreshold = false, reachTimeThreshold = false;
                    switch (party)
                    {
                        //case PartyType.Client:
                        //    reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdClient;
                        //    break;
                        case PartyType.EVH:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdEVH;
                            break;
                        case PartyType.KH:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdKH;
                            break;
                        case PartyType.Helper:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdHelper;
                            break;
                        default:
                            break;
                    }
                    stopwatch[party].Stop();
                    reachTimeThreshold = stopwatch[party].ElapsedMilliseconds >= Config.Runtime_Network_TimeThreshold;
                    stopwatch[party].Start();
                    if (reachBufferThreshold || reachTimeThreshold)
                    {
                        List<byte[]> msgs = msgBuffer[party].Retrieve();

                        //// if buffer already contains a null referance, which means there will be no more messages
                        //if (msgBuffer[party].IsComplete)
                        //{
                        //    // decrement notEmptyBufferCount
                        //    --notEmptyBufferCount;
                        //}

                        // once messages are delivered, resart the stopwatch
                        stopwatch[party].Restart();

                        // if message buffer is not empty, send the message
                        if (msgs.Count != 0)
                        {
                            try
                            {
                                // Create a TCP/IP socket.
                                Socket socket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream, ProtocolType.Tcp);

                                SenderState sender = new SenderState(socket, party);

                                // Connect to the remote endpoint.
                                socket.BeginConnect(partyIPMap[party],
                                    new AsyncCallback(ConnectCallback), sender);
                                connectDone[party].WaitOne();

                                // send data to remote party
                                byte[] data = Message.Pack(localParty.Type, msgs);
                                Send(sender, data);
                                sendDone[party].WaitOne();

                                // Release the socket.
                                socket.Shutdown(SocketShutdown.Both);
                                socket.Close();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                // break the while(true) loop
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                SenderState sender = (SenderState)ar.AsyncState;

                // Complete the connection.
                sender.Socket.EndConnect(ar);

                // Console.WriteLine("" + localParty.Type + " connected to {0}",
               //     sender.Socket.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone[sender.To].Set();
            }
            catch (Exception e)
            {
                // ConnectCallback(ar);
                // Console.WriteLine(e.ToString());
            }
        }

        private void Send(SenderState sender, byte[] data)
        {
            // Begin sending the data to the remote device.
            sender.Socket.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), sender);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                SenderState sender = (SenderState)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = sender.Socket.EndSend(ar);
                Console.WriteLine("" + localParty.Type + " sent {0} bytes to " + sender.To, bytesSent);

                // Signal that all bytes have been sent.
                sendDone[sender.To].Set();
            }
            catch (Exception e)
            {

                //Console.WriteLine(e.ToString());
            }
        }
    }

    internal class SenderLocal : Sender
    {
        public SenderLocal(Dictionary<PartyType, ReceiverLocal> partyReceicerMap, Party localParty, List<Party> parties)
            : base(localParty, partyReceicerMap[localParty.Type])
        {
            foreach (var party in parties)
            {
                partytypePartyMap.Add(party.Type, party);
                if (party.Type != localParty.Type)
                {
                    msgBuffer.Add(party.Type, new ConcurrentList());
                }
            }
            this.partyReceicerMap = partyReceicerMap;
        }

        public override void SendTo(PartyType party, byte[] msg)
        {
            msgBuffer[party].Add(msg);

            //int line = BitConverter.ToInt32(msg, 0);
            //OperationType op = (OperationType)BitConverter.ToInt16(msg, sizeof(int));
            //bool invokeHelper = BitConverter.ToBoolean(msg, sizeof(int) + sizeof(short));
            //byte[] newMsg = msg.SubArray(ByteStreamHandler.HeadLength);

            //var queue = partyReceicerMap[party].messageQueue[localParty.Type].GetOrAdd(line, new ConcurrentQueue<byte[]>());
            //queue.Enqueue(newMsg);

            //var sig = partyReceicerMap[party].signal[localParty.Type].GetOrAdd(line, new AutoResetEvent(true));
            //sig.Set();

            //if (party == PartyType.Helper && invokeHelper)
            //{
            //    Helper helper = (Helper)partytypePartyMap[party];
            //    helper.RunParty(line, op);
            //}
        }

        public override void StartSender()
        {
            // only EVH, KH, Helper will need to send message
            if(localParty.Type != PartyType.Client)
            {
                Thread thread = new Thread(FlushBuffer);
                thread.Name = localParty.Type + "_Sender";
                thread.Start();
            }
        }

        private Dictionary<PartyType, ReceiverLocal>  partyReceicerMap;
        private Dictionary<PartyType, Party> partytypePartyMap = new Dictionary<PartyType, Party>();

        // massage buffer, gather messages sent to one party as many as possible
        private Dictionary<PartyType, ConcurrentList> msgBuffer = new Dictionary<PartyType, ConcurrentList>();

        // one thread is constantly scan the buffer for all parties
        private void FlushBuffer()
        {
            Dictionary<PartyType, Stopwatch> stopwatch = new Dictionary<PartyType, Stopwatch>();
            foreach (var party in msgBuffer.Keys)
            {
                stopwatch.Add(party, new Stopwatch());
                stopwatch[party].Start();
            }
            while (!receiver.hasReceivedEOP)
            {
                Thread.Sleep(Config.Runtime_Network_ScanPeriod);
                foreach (var party in msgBuffer.Keys)
                {
                    bool reachBufferThreshold = false, reachTimeThreshold = false;
                    switch (party)
                    {
                        //case PartyType.Client:
                        //    reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdClient;
                        //    break;
                        case PartyType.EVH:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdEVH;
                            break;
                        case PartyType.KH:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdKH;
                            break;
                        case PartyType.Helper:
                            reachBufferThreshold = msgBuffer[party].Count >= Config.Runtime_Network_BufferMessageThresholdHelper;
                            break;
                        default:
                            break;
                    }
                    stopwatch[party].Stop();
                    reachTimeThreshold = stopwatch[party].ElapsedMilliseconds >= Config.Runtime_Network_TimeThreshold;
                    stopwatch[party].Start();
                    if (reachBufferThreshold || reachTimeThreshold)
                    {
                        List<byte[]> msgs = msgBuffer[party].Retrieve();

                        // once messages are delivered, resart the stopwatch
                        stopwatch[party].Restart();

                        // if message buffer is not empty, send the message
                        if (msgs.Count != 0)
                        {
                            //System.Diagnostics.Debug.WriteLine(msgs.Count + ", from: " + localParty.Type + ", to: " + party);
                            // send data to remote party
                            byte[] data = Message.Pack(localParty.Type, msgs);
                            ThreadPool.QueueUserWorkItem(ReceiverLocal.SimulateNetwork, 
                                new ReceiverLocal.SimulateNetworkState(partyReceicerMap[party], (Helper)partytypePartyMap[PartyType.Helper], data));                               
                        }
                    }
                }
            }
        }
    }
}
