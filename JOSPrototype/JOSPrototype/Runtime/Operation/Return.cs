using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
    class ReturnOnEVH: OperationOnEVH
    {
        public ReturnOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, OperationType.Return)
        { }
        protected override void OnEVH()
        {
            var encValReturn = new Numeric[program.vReturn.Count];
            for (int i = 0; i < program.vReturn.Count; ++i)
            {
                encValReturn[i] = (Numeric)program.vTable[program.vReturn[i]];
            }
            var toClient = Message.AssembleMessage(int.MaxValue, opType, false, encValReturn);
            party.sender.SendTo(PartyType.Client, toClient);
            program.evaluatedIC.AddOrUpdate(code, new Object(), (k, v) => new Object());
        }
    }
    class ReturnOnKH: OperationOnKH
    {
        public ReturnOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, OperationType.Return)
        { }
        protected override void OnKH()
        {
            var keyReturn = new Numeric[program.vReturn.Count];
            for (int i = 0; i < program.vReturn.Count; ++i)
            {
                keyReturn[i] = (Numeric)program.vTable[program.vReturn[i]];
            }
            byte[] toClient = Message.AssembleMessage(int.MaxValue, opType, false, keyReturn);
            party.sender.SendTo(PartyType.Client, toClient);
            program.evaluatedIC.AddOrUpdate(code, new Object(), (k, v) => new Object());
        }
    }
}
