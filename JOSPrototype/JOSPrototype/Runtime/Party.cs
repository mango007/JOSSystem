using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace JOSPrototype.Runtime
{   
    abstract class Party
    { 
        public PartyType Type { get; private set; }
        public Party(PartyType type)
        {
            Type = type;
        }

        public void AddSender(Sender sender)
        {
            this.sender = sender;
        }

        public void AddReceiver(Receiver receiver)
        {
            this.receiver = receiver;
        }

        public abstract void RunParty();

        protected internal Sender sender;
        protected internal Receiver receiver;

        /// <summary>
        /// start parties
        /// </summary>
        /// <returns> </returns>
        public static long RunAllParties(Program program, Program[] programEnc)
        {
            List<Party> parties = new List<Party>();
            Client client = new Client(program);
            parties.Add(client);
            EVH evh = new EVH(programEnc[0]);
            parties.Add(evh);
            KH kh = new KH(programEnc[1]);
            parties.Add(kh);
            Helper helper = new Helper();
            parties.Add(helper);

            Network.Network.NetworkInitialize(parties);

            var watch = Stopwatch.StartNew();

            Thread thread = new Thread(() => evh.RunParty());
            thread.Name = "EVH";
            thread.Start();

            thread = new Thread(() => kh.RunParty());
            thread.Name = "KH";
            thread.Start();

            client.RunParty();

            watch.Stop();
            long totalTime = watch.ElapsedMilliseconds;
            //var totalTime = (watch.ElapsedTicks * (1000L * 1000L * 1000L)) / Stopwatch.Frequency;
            Network.Network.TerminateNetwork(parties);
            return totalTime;
        }
    }
}
