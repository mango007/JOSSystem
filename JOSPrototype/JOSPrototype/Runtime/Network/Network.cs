using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace JOSPrototype.Runtime.Network
{
    internal class Network
    {

        public static void NetworkInitialize(List<Party> parties, bool isLocal = true)
        {
            if (isLocal)
            {
                Dictionary<PartyType, ReceiverLocal> partyReceicerMap = new Dictionary<PartyType, ReceiverLocal>();
                foreach (var party in parties)
                {
                    ReceiverLocal receiver = new ReceiverLocal(parties, party);
                    partyReceicerMap.Add(party.Type, receiver);
                    party.AddReceiver(receiver);
                    receiver.StartReceiver();
                }
                foreach (var party in parties)
                {
                    SenderLocal sender = new SenderLocal(partyReceicerMap, party, parties);
                    party.AddSender(sender);
                    sender.StartSender();
                }
            }
            else
            {
                foreach (var party in parties)
                {
                    ReceiverInternet receiver = new ReceiverInternet(Config.partyAddress, party);
                    SenderInternet sender = new SenderInternet(Config.partyAddress, party, receiver);
                    party.AddReceiver(receiver);
                    party.AddSender(sender);
                    receiver.StartReceiver();
                    sender.StartSender();
                }
            }
        }
        public static void TerminateNetwork(List<Party> parties, bool isLocal = true)
        {
            if(isLocal)
            {
                foreach(var party in parties)
                {
                    party.receiver.hasReceivedEOP = true;
                }
            }
        }
    }
}
