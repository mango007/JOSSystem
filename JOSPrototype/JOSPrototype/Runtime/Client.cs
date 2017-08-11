using JOSPrototype.Components;
using JOSPrototype.Runtime.Operation;
using JOSPrototype.Runtime.Network;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Remoting;
namespace JOSPrototype.Runtime
{
    class Client: Party
    {
        public Client(Program program)
            :base(PartyType.Client)
        {
            this.program = program;
        }
        public override void RunParty()
        {
            byte[] fromEVH = receiver.ReceiveFrom(PartyType.EVH, new MessageID(int.MaxValue, OperationType.Return));
            byte[] fromKH = receiver.ReceiveFrom(PartyType.KH, new MessageID(int.MaxValue, OperationType.Return));
            var encVal = Message.DisassembleMessage(fromEVH);
            var key = Message.DisassembleMessage(fromKH);
            for (int i = 0; i < program.vReturn.Count; ++i)
            {
                Numeric result = null;
                if(encVal[i].GetEncType() == EncryptionType.None)
                {
                    System.Diagnostics.Debug.Assert(key[i].GetEncType() == EncryptionType.None);
                    System.Diagnostics.Debug.Assert(key[i].GetUnsignedBigInteger() == encVal[i].GetUnsignedBigInteger());
                    System.Diagnostics.Debug.Assert(key[i].GetScaleBits() == encVal[i].GetScaleBits());
                    result = encVal[i];
                }
                else if(encVal[i].GetEncType() == EncryptionType.AddMod)
                {
                    System.Diagnostics.Debug.Assert(key[i].GetEncType() == EncryptionType.AddMod);
                    result = encVal[i] - key[i];
                }
                else
                {
                    System.Diagnostics.Debug.Assert(encVal[i].GetEncType() == EncryptionType.XOR);
                    System.Diagnostics.Debug.Assert(key[i].GetEncType() == EncryptionType.XOR);
                    result = encVal[i] ^ key[i];
                }
                program.vTable.AddOrUpdate(program.vReturn[i], result, (k, v) => result);
            }
        }

        private Program program;
    }
}
