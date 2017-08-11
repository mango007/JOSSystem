using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
    class WhileOnEVH : OperationOnEVH
    {
        // nested while loop
        public WhileOnEVH(Party party, ICWhile code, Program program, Operation caller)
            : base(party, code, program, caller, OperationType.While)
        {
            icw = code;
            codes = new List<IntermediateCode>();
            codes.AddRange(code.conditionCodes.GetCodes());
            codes.AddRange(code.codes.GetCodes());
        }
        // out most while loop
        public WhileOnEVH(Party party, ICWhile code, Program program)
            : base(party, code, program, OperationType.While)
        {       
            icw = (ICWhile)Program.TransformIntermediateCode(null, code, program)[0];
            codes = new List<IntermediateCode>();
            codes.AddRange(code.conditionCodes.GetCodes());
            codes.AddRange(code.codes.GetCodes());
        }
        ICWhile icw;
        List<IntermediateCode> codes;
        int iter = 0, nextCheck = ICWhile.firstCheck;
        NumericArray keyCond = new NumericArray();
        Numeric encCond;
        protected override void OnEVH()
        {
            if (step <= codes.Count)
            {
                Evaluate(codes[step - 1]);
            }
            else if (step == codes.Count + 1)
            {
                iter++;
                if (nextCheck == iter)
                {
                    encCond = program.GetValue(icw.condition);
                    // send encrypted condition to KH
                    var toKH = Message.AssembleMessage(icw.index, opType, false, new Numeric[] { encCond });
                    party.sender.SendTo(PartyType.KH, toKH);

                    // receive key of condition from KH
                    party.receiver.ReceiveFrom(PartyType.KH, icw.index, this, keyCond);
                }
                else
                {
                    step = 0;
                    // start a new iteration
                    Run();
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(step == codes.Count + 2);
                // if condition holds, continue the while loop
                if ((encCond ^ keyCond[0]).GetUnsignedBigInteger() != 0)
                {
                    step = 0;
                    nextCheck = (int)(nextCheck * ICWhile.incFactor);
                    // start a new iteration
                    Run();
                }
                else
                {
                    // if the while is nested
                    if(!ReferenceEquals(caller, null))
                    {
                        caller.Run();
                    }
                    else
                    {
                        program.evaluatedIC.AddOrUpdate(icw, new Object(), (k, v) => new Object());
                    }
                }
            }
        }
    }
    class WhileOnKH : OperationOnKH
    {
        public WhileOnKH(Party party, ICWhile code, Program program, Operation caller)
            : base(party, code, program, caller, OperationType.While)
        {
            icw = code;
            codes = new List<IntermediateCode>();
            codes.AddRange(code.conditionCodes.GetCodes());
            codes.AddRange(code.codes.GetCodes());
        }
        // out most while loop
        public WhileOnKH(Party party, ICWhile code, Program program)
            : base(party, code, program, OperationType.While)
        {
            icw = (ICWhile)Program.TransformIntermediateCode(null, code, program)[0];
            codes = new List<IntermediateCode>();
            codes.AddRange(code.conditionCodes.GetCodes());
            codes.AddRange(code.codes.GetCodes());
        }
        ICWhile icw;
        List<IntermediateCode> codes;
        int iter = 0, nextCheck = ICWhile.firstCheck;
        NumericArray encCond = new NumericArray();
        Numeric keyCond;
        protected override void OnKH()
        {
            if (step <= codes.Count)
            {
                Evaluate(codes[step - 1]);
            }
            else if (step == codes.Count + 1)
            {
                iter++;
                if (nextCheck == iter)
                {
                    keyCond = program.GetValue(icw.condition);
                    // send key to EVH
                    var toEVH = Message.AssembleMessage(icw.index, opType, false, new Numeric[] { keyCond });
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    // receive encrypted condition from EVH
                    party.receiver.ReceiveFrom(PartyType.EVH, icw.index, this, encCond);
                }
                else
                {
                    step = 0;
                    // start a new iteration
                    Run();
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(step == codes.Count + 2);
                // if condition holds, continue the while loop
                if ((keyCond ^ encCond[0]).GetUnsignedBigInteger() != 0)
                {
                    step = 0;
                    nextCheck = (int)(nextCheck * ICWhile.incFactor);
                    // start a new iteration
                    Run();
                }
                else
                {
                    // if the while loop is nested
                    if (!ReferenceEquals(caller, null))
                    {
                        caller.Run();
                    }
                    else
                    {
                        program.evaluatedIC.AddOrUpdate(icw, new Object(), (k, v) => new Object());
                    }
                }
            }
        }
    }

}
