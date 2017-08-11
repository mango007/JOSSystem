using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
    class IfElseOnEVH : OperationOnEVH
    {
        // nested if-else
        public IfElseOnEVH(Party party, ICIfElse code, Program program, Operation caller)
            : base(party, code, program, caller, OperationType.IfElse)
        {
            icie = code;
        }
        // outermost if-else
        public IfElseOnEVH(Party party, ICIfElse code, Program program)
            : base(party, code, program, OperationType.IfElse)
        {
            icie = code;
        }
        ICIfElse icie;
        Numeric encRand1;
        bool condExed = false, isCondEnc = true, isCondTrue;
        NumericArray 
            encRand2 = new NumericArray(), 
            encRand = new NumericArray(),
            encRevealProb = new NumericArray(),
            encReveal = new NumericArray(), 
            keyReveal = new NumericArray();
        Numeric encCanBeRevealed;
        ICSequence codesToExe = new ICSequence();
        protected override void OnEVH()
        {
            // execute codes for condition
            if (!condExed)
            {
                if (step <= icie.conditionCodes.Count)
                {
                    Evaluate(icie.conditionCodes[step - 1]);
                }
                else
                {
                    var cond = program.GetValue(icie.condition);
                    //Console.WriteLine("EVH: condition: " + cond.GetUnsignedBigInteger() + ", outer condition: " + (ReferenceEquals(icie.outerCondition, null) ? "" : program.GetValue(icie.outerCondition).GetUnsignedBigInteger().ToString()));
                    if(cond.GetEncType() == EncryptionType.None)
                    {
                        isCondEnc = false;
                        System.Diagnostics.Debug.Assert(cond.GetUnsignedBigInteger() == 0 || cond.GetUnsignedBigInteger() == 1);
                        isCondTrue = cond.GetUnsignedBigInteger() == 1 ? true : false;
                    }
                    condExed = true;
                    step = 0;
                    Run();
                }
            }
            else
            {
                // if condition is not encrypted
                if(!isCondEnc)
                {
                    if(step == 1)
                    {
                        // execute if branch
                        if(isCondTrue)
                        {
                            if (icie.codesIf.Count != 0)
                            {
                                foreach (var entry in icie.codesIf.GetCodes())
                                {
                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                }
                            }
                        }
                        // execute else branch
                        else
                        {
                            if (icie.codesElse.Count != 0)
                            {
                                foreach (var entry in icie.codesElse.GetCodes())
                                {
                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                }
                            }
                        }
                    }
                    // execute each command
                    if (step - 1 < codesToExe.Count)
                    {
                        Evaluate(codesToExe[step - 1]);
                    }
                    else
                    {
                        // nested if-else statement
                        if (!ReferenceEquals(caller, null))
                        {
                            caller.Run();
                        }
                        // outermost if-else statement
                        else
                        {
                            program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                        }
                    }
                }
                else
                {
                    // do not use SDP
                    if (ReferenceEquals(icie.revealCond, null))
                    {
                        System.Diagnostics.Debug.Assert(ReferenceEquals(icie.prob, null));
                        if (step == 1)
                        {
                            // transform if-else intermediate code to a list of intermediate codes
                            codesToExe.AddRange(Program.TransformICIfElse(icie, program));
                        }
                        if (step <= codesToExe.Count)
                        {
                            Evaluate(codesToExe[step - 1]);
                        }
                        else
                        {
                            // nested if-else statement
                            if (!ReferenceEquals(caller, null))
                            {
                                caller.Run();
                            }
                            // outermost if-else statement
                            else
                            {
                                program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                            }
                        }
                    }
                    else
                    {
                        // not statistic, go directly to round 5
                        if (ReferenceEquals(icie.prob, null) && step == 1)
                        {
                            step = 5;
                            encReveal.SetArray(new Numeric[] { new Numeric(1, 0) - (program.GetValue(icie.condition) ^ icie.revealCond) });
                        }
                        switch (step)
                        {
                            case 1:
                                // generate random number
                                Numeric rand1 = Utility.NextUnsignedNumeric(Config.ScaleBits, Config.ScaleBits),
                                        keyRand1 = Utility.NextUnsignedNumeric(Config.ScaleBits);
                                encRand1 = rand1 ^ keyRand1;

                                var toKH1 = Message.AssembleMessage(line, opType, false, new Numeric[] { keyRand1 });
                                party.sender.SendTo(PartyType.KH, toKH1);

                                party.receiver.ReceiveFrom(PartyType.KH, line, this, encRand2);
                                break;
                            case 2:
                                new XORToAddModOnEVH(party, line, this, new NumericArray(encRand1 ^ encRand2[0]), encRand).Run();
                                break;
                            case 3:
                                // encVal[0] and variable.leftBoundary or variable.rightBoundary should have same scale bits
                                Numeric temp1 = new Numeric(encRand[0]), temp2 = new Numeric(icie.prob);
                                Numeric.Scale(temp1, temp2);
                                var probLess = temp1 - temp2;
                                // compute rand < icie.prob
                                new LessZeroOnEVH(party, line, this, new NumericArray(probLess), encRevealProb, Config.KeyBits).Run();
                                break;
                            case 4:
                                // compute icie.condition == icie.revealCond
                                encCanBeRevealed = new Numeric(1, 0) ^ (program.GetValue(icie.condition) ^ icie.revealCond);
                                // (icie.condition == icie.revealCond) && (rand < icie.prob)
                                new ANDOnEVH(party, line, this, new NumericArray(encRevealProb[0], encCanBeRevealed), encReveal).Run();
                                break;
                            case 5:
                                // send encVal(reveal) to KH
                                var toKH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { encReveal[0] });
                                party.sender.SendTo(PartyType.KH, toKH2);
                                // receive key(reveal) from KH
                                party.receiver.ReceiveFrom(PartyType.KH, line, this, keyReveal);
                                break;
                            default:
                                if (step == 6)
                                {
                                    // if condition can be revealed
                                    if ((encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1)
                                    {
                                        // if revealed condition is true, execute if branch
                                        if (icie.revealCond.GetUnsignedBigInteger() == 1)
                                        {
                                            if (icie.codesIf.Count != 0)
                                            {
                                                foreach (var entry in icie.codesIf.GetCodes())
                                                {
                                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                                }
                                            }
                                        }
                                        // else, execute else branch
                                        else
                                        {
                                            if (icie.codesElse.Count != 0)
                                            {
                                                foreach (var entry in icie.codesElse.GetCodes())
                                                {
                                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                                }
                                            }
                                        }
                                    }
                                    // if condition can not be revealed, execute both branches
                                    else
                                    {
                                        // transform if-else intermediate code to a list of intermediate codes
                                        codesToExe.AddRange(Program.TransformICIfElse(icie, program));
                                    }
                                }
                                // execute each command
                                if (step - 6 < codesToExe.Count)
                                {
                                    Evaluate(codesToExe[step - 6]);
                                }
                                else
                                {
                                    // nested if-else statement
                                    if (!ReferenceEquals(caller, null))
                                    {
                                        caller.Run();
                                    }
                                    // outermost if-else statement
                                    else
                                    {
                                        program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }         
    }

    class IfElseOnKH : OperationOnKH
    {
        // nested if-else
        public IfElseOnKH(Party party, ICIfElse code, Program program, Operation caller)
            : base(party, code, program, caller, OperationType.IfElse)
        {
            icie = code;
        }
        // out most if-else
        public IfElseOnKH(Party party, ICIfElse code, Program program)
            : base(party, code, program, OperationType.IfElse)
        {
            icie = code;
        }
        ICIfElse icie;
        Numeric keyRand2;
        bool condExed = false, isCondEnc = true, isCondTrue;
        NumericArray
            keyRand1 = new NumericArray(),
            keyRand = new NumericArray(),
            keyRevealProb = new NumericArray(),
            encReveal = new NumericArray(),
            keyReveal = new NumericArray();
        Numeric keyCanBeRevealed;
        ICSequence codesToExe = new ICSequence();
        protected override void OnKH()
        {
            // execute codes for condition
            if (!condExed)
            {
                if (step <= icie.conditionCodes.Count)
                {
                    Evaluate(icie.conditionCodes[step - 1]);
                }
                else
                {
                    var cond = program.GetValue(icie.condition);
                    //Console.WriteLine("KH: condition: " + cond.GetUnsignedBigInteger() + ", outer condition: " + (ReferenceEquals(icie.outerCondition, null) ? "" : program.GetValue(icie.outerCondition).GetUnsignedBigInteger().ToString()));
                    if (cond.GetEncType() == EncryptionType.None)
                    {
                        isCondEnc = false;
                        System.Diagnostics.Debug.Assert(cond.GetUnsignedBigInteger() == 0 || cond.GetUnsignedBigInteger() == 1);
                        isCondTrue = cond.GetUnsignedBigInteger() == 1 ? true : false;
                    }
                    condExed = true;
                    step = 0;
                    Run();
                }
            }
            else
            {
                if(!isCondEnc)
                {
                    if (step == 1)
                    {
                        // execute if branch
                        if (isCondTrue)
                        {
                            if (icie.codesIf.Count != 0)
                            {
                                foreach (var entry in icie.codesIf.GetCodes())
                                {
                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                }
                            }
                        }
                        // execute else branch
                        else
                        {
                            if (icie.codesElse.Count != 0)
                            {
                                foreach (var entry in icie.codesElse.GetCodes())
                                {
                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                }
                            }
                        }
                    }
                    // execute each command
                    if (step - 1 < codesToExe.Count)
                    {
                        Evaluate(codesToExe[step - 1]);
                    }
                    else
                    {
                        // nested if-else statement
                        if (!ReferenceEquals(caller, null))
                        {
                            caller.Run();
                        }
                        // outermost if-else statement
                        else
                        {
                            program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                        }
                    }
                }
                else
                {
                    // do not use SDP
                    if (ReferenceEquals(icie.revealCond, null))
                    {
                        System.Diagnostics.Debug.Assert(ReferenceEquals(icie.prob, null));
                        if (step == 1)
                        {
                            // transform if-else intermediate code to list of intermediate code
                            codesToExe.AddRange(Program.TransformICIfElse(icie, program));
                        }
                        if (step <= codesToExe.Count)
                        {
                            Evaluate(codesToExe[step - 1]);
                        }
                        else
                        {
                            // nested if-else statement
                            if (!ReferenceEquals(caller, null))
                            {
                                caller.Run();
                            }
                            // outermost if-else statement
                            else
                            {
                                program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                            }
                        }
                    }
                    else
                    {
                        // not statistic, go directly to round 5
                        if (ReferenceEquals(icie.prob, null) && step == 1)
                        {
                            step = 5;
                            keyReveal.SetArray(new Numeric[] { program.GetValue(icie.condition) });
                        }
                        switch (step)
                        {
                            case 1:
                                // generate random number
                                Numeric rand2 = Utility.NextUnsignedNumeric(Config.ScaleBits, Config.ScaleBits);
                                keyRand2 = Utility.NextUnsignedNumeric(Config.ScaleBits);
                                var encRand2 = rand2 ^ keyRand2;

                                var toEVH1 = Message.AssembleMessage(line, opType, false, new Numeric[] { encRand2 });
                                party.sender.SendTo(PartyType.EVH, toEVH1);

                                party.receiver.ReceiveFrom(PartyType.EVH, line, this, keyRand1);
                                break;
                            case 2:
                                new XORToAddModOnKH(party, line, this, new NumericArray(keyRand2 ^ keyRand1[0]), keyRand).Run();
                                break;
                            case 3:
                                // compute rand < icie.prob
                                new LessZeroOnKH(party, line, this, new NumericArray(keyRand[0]), keyRevealProb, Config.KeyBits).Run();
                                break;
                            case 4:
                                // compute icie.condition == icie.revealCond
                                keyCanBeRevealed = program.GetValue(icie.condition);
                                // (icie.condition == icie.revealCond) && (rand < icie.prob)
                                new ANDOnKH(party, line, this, new NumericArray(keyRevealProb[0], keyCanBeRevealed), keyReveal).Run();
                                break;
                            case 5:
                                // send key(reveal) to KH
                                var toEVH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { keyReveal[0] });
                                party.sender.SendTo(PartyType.EVH, toEVH2);
                                // receive encVal(reveal) from EVH
                                party.receiver.ReceiveFrom(PartyType.EVH, line, this, encReveal);
                                break;
                            default:
                                if (step == 6)
                                {
                                    // if condition can be revealed
                                    if ((encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1)
                                    {
                                        // if revealed condition is true, execute if branch
                                        if (icie.revealCond.GetUnsignedBigInteger() == 1)
                                        {
                                            if (!ReferenceEquals(icie.codesIf, null))
                                            {
                                                foreach (var entry in icie.codesIf.GetCodes())
                                                {
                                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                                }
                                            }
                                        }
                                        // else, execute else branch
                                        else
                                        {
                                            if (!ReferenceEquals(icie.codesElse, null))
                                            {
                                                foreach (var entry in icie.codesElse.GetCodes())
                                                {
                                                    codesToExe.AddRange(Program.TransformIntermediateCode(icie.outerCondition, entry, program));
                                                }
                                            }
                                        }
                                    }
                                    // if condition can not be revealed, execute both branches
                                    else
                                    {
                                        codesToExe.AddRange(Program.TransformICIfElse(icie, program));
                                    }
                                }
                                // execute each command
                                if (step - 6 < codesToExe.Count)
                                {
                                    Evaluate(codesToExe[step - 6]);
                                }
                                else
                                {
                                    // nested if-else statement
                                    if (!ReferenceEquals(caller, null))
                                    {
                                        caller.Run();
                                    }
                                    // outermost if-else statement
                                    else
                                    {
                                        program.evaluatedIC.AddOrUpdate(icie, new Object(), (k, v) => new Object());
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
    //class IfElseOnHelper : OperationOnHelper
    //{
    //    public IfElseOnHelper(Party party, int line)
    //        : base(party, line)
    //    { }
    //    protected override void OnHelper()
    //    {
    //        Numeric
    //            rand = new Numeric((BigInteger)(Utility.NextDouble() * Math.Pow(2, Config.FractionBitLength)), Config.FractionBitLength),
    //            key = Utility.NextUnsignedNumeric(Config.FractionBitLength),
    //            encVal = rand + key;

    //        var toEVH = Message.AssembleMessage(line, OperationType.IfElse, false, new Numeric[] { encVal });
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var toKH = Message.AssembleMessage(line, OperationType.IfElse, false, new Numeric[] { key });
    //        party.sender.SendTo(PartyType.KH, toKH);
    //    }
    //}
}
