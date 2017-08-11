using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
   
    class ConcealOnEVH : OperationOnEVH
    {
        public ConcealOnEVH(Party party, int line, Operation caller, NumericArray operands, EVariable variable, Program program)
            : base(party, line, caller, operands, null, OperationType.Conceal)
        {
            this.program = program;
            this.variable = variable;
            encType = operands[0].GetEncType();
        }
        EVariable variable;
        EncryptionType encType;
        Numeric encRand1;
        bool reveal;
        NumericArray
            key = new NumericArray(),
            lessZeroRe = new NumericArray(),
            isOutOfRange = new NumericArray(),
            encRand2 = new NumericArray(),
            encRand = new NumericArray(),
            encReveal = new NumericArray(),
            keyReveal = new NumericArray(),
            encValTemp = new NumericArray();

        protected override void OnEVH()
        {
            // if variable does not require DOM/STATDOM, we simply assign the result to the variable 
            if (ReferenceEquals(variable.leftBoundary, null))
            {
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.rightBoundary, null));
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.prob, null));
                program.SetValue(variable, encVal[0]);
                caller.Run();
            }
            // if variable uses STATDOM
            else if(!ReferenceEquals(variable.leftBoundary, null) && !ReferenceEquals(variable.prob, null))
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.rightBoundary, null));
                // if val is not encrypted
                if(encType == EncryptionType.None)
                {
                    // if val is in the DOM, we need to encrypt it
                    if(variable.leftBoundary.GetVal() < encVal[0].GetVal() && encVal[0].GetVal() < variable.rightBoundary.GetVal())
                    {
                        switch(step)
                        {
                            case 1:
                                party.receiver.ReceiveFrom(PartyType.KH, line, this, key);
                                break;
                            case 2:
                                if(Config.DefaultEnc == EncryptionType.AddMod)
                                {
                                    encVal[0] = encVal[0] + key[0];
                                    encVal[0].SetEncType(EncryptionType.AddMod);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Assert(Config.DefaultEnc == EncryptionType.XOR);
                                    encVal[0] = encVal[0] ^ key[0];
                                    encVal[0].SetEncType(EncryptionType.XOR);
                                }
                                program.SetValue(variable, encVal[0]);
                                caller.Run();
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                    else
                    {
                        encVal[0].SetEncType(EncryptionType.None);
                        program.SetValue(variable, encVal[0]);
                        caller.Run();
                    }
                }
                // else val is encrypted
                else
                {
                    switch(step)
                    {
                        case 1:
                            // if val is XOR encrypted, we need to convert it into AddMod encryption
                            if (encType == EncryptionType.XOR)
                            {
                                new XORToAddModOnEVH(party, line, this, encVal, encValTemp).Run();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(encType == EncryptionType.AddMod);
                                Run();
                            }
                            break;
                        case 2:
                            // generate random number
                            Numeric rand1 = Utility.NextUnsignedNumeric(Config.ScaleBits, Config.ScaleBits),
                                    keyRand1 = Utility.NextUnsignedNumeric(Config.ScaleBits);
                            encRand1 = rand1 ^ keyRand1;

                            var toKH1 = Message.AssembleMessage(line, opType, false, new Numeric[] { keyRand1 });
                            party.sender.SendTo(PartyType.KH, toKH1);

                            party.receiver.ReceiveFrom(PartyType.KH, line, this, encRand2);
                            break;
                        case 3:
                            new XORToAddModOnEVH(party, line, this, new NumericArray(encRand1 ^ encRand2[0]), encRand).Run();
                            break;
                        case 4:
                            // encVal[0] and variable.leftBoundary or variable.rightBoundary should have same scale bits
                            Numeric temp1 = new Numeric(encValTemp[0]), temp2 = new Numeric(variable.leftBoundary);
                            Numeric.Scale(temp1, temp2);
                            var valLessLeft = temp1 - temp2;
                            temp1 = new Numeric(variable.rightBoundary);
                            temp2 = new Numeric(encValTemp[0]);
                            Numeric.Scale(temp1, temp2);
                            var valGreaterRight = temp1 - temp2;
                            temp1 = new Numeric(encRand[0]);
                            temp2 = new Numeric(variable.prob);
                            Numeric.Scale(temp1, temp2);
                            var probLess  = temp1 - temp2;
                            // compute rand < prob,  secret < leftBoundary and rightBoundary < secret
                            new LessZeroOnEVH(party, line, this,
                                new NumericArray(valLessLeft, valGreaterRight, probLess),
                                lessZeroRe, Config.KeyBits).Run();
                            break;
                        case 5:
                            // check if secret is out of range, not in DOM
                            new OROnEVH(party, line, this, new NumericArray(lessZeroRe[0], lessZeroRe[1]), isOutOfRange).Run();
                            break;
                        case 6:
                            // check if secret can be revealed
                            new ANDOnEVH(party, line, this, new NumericArray(isOutOfRange[0], lessZeroRe[2]), encReveal).Run();
                            break;
                        case 7:
                            // send encReveal to KH
                            var toKH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { encReveal[0] });
                            party.sender.SendTo(PartyType.KH, toKH2);
                            // receive keyReveal from KH
                            party.receiver.ReceiveFrom(PartyType.KH, line, this, keyReveal);
                            break;
                        case 8:
                            reveal = (encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1;
                            if(reveal)
                            {
                                party.sender.SendTo(
                                    PartyType.KH,
                                    Message.AssembleMessage(line, opType, false, new Numeric[] { encValTemp[0] }));
                                party.receiver.ReceiveFrom(PartyType.KH, line, this, key);
                            }
                            else
                            {
                                Run();
                            }
                            break;
                        case 9:
                            if(reveal)
                            {
                                encVal[0] = encValTemp[0] - key[0];
                                encVal[0].SetEncType(EncryptionType.None);                  
                            }
      
                            program.SetValue(variable, encVal[0]);
                            caller.Run();
                            break;
                        default:
                            throw new Exception();
                    }
                    
                }                               
            }
            // DOM
            else
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.leftBoundary, null));
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.rightBoundary, null));
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.prob, null));
                // if val is not encrypted
                if (encType == EncryptionType.None)
                {
                    // if val is in the DOM, we need to encrypt it
                    if (variable.leftBoundary.GetVal() < encVal[0].GetVal() && encVal[0].GetVal() < variable.rightBoundary.GetVal())
                    {
                        switch (step)
                        {
                            case 1:
                                party.receiver.ReceiveFrom(PartyType.KH, line, this, key);
                                break;
                            case 2:
                                if (Config.DefaultEnc == EncryptionType.AddMod)
                                {
                                    encVal[0] = encVal[0] + key[0];
                                    encVal[0].SetEncType(EncryptionType.AddMod);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Assert(Config.DefaultEnc == EncryptionType.XOR);
                                    encVal[0] = encVal[0] ^ key[0];
                                    encVal[0].SetEncType(EncryptionType.XOR);
                                }
                                program.SetValue(variable, encVal[0]);
                                caller.Run();
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                    else
                    {
                        encVal[0].SetEncType(EncryptionType.None);
                        program.SetValue(variable, encVal[0]);
                        caller.Run();
                    }
                }
                // else val is encrypted
                else
                {
                    switch (step)
                    {
                        case 1:
                            // if val is XOR encrypted, we need to convert it into AddMod encryption
                            if (encType == EncryptionType.XOR)
                            {
                                new XORToAddModOnEVH(party, line, this, encVal, encValTemp).Run();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(encType == EncryptionType.AddMod);
                                Run();
                            }
                            break;
                        case 2:
                            // encVal[0] and variable.leftBoundary or variable.rightBoundary should have same scale bits
                            Numeric temp1 = new Numeric(encValTemp[0]), temp2 = new Numeric(variable.leftBoundary);
                            Numeric.Scale(temp1, temp2);
                            var valLessLeft = temp1 - temp2;
                            temp1 = new Numeric(variable.rightBoundary);
                            temp2 = new Numeric(encValTemp[0]);
                            Numeric.Scale(temp1, temp2);
                            var valGreaterRight = temp1 - temp2;
                            
                            // compute secret < leftBoundary and rightBoundary < secret
                            new LessZeroOnEVH(party, line, this,
                                new NumericArray(valLessLeft, valGreaterRight),
                                lessZeroRe, Config.KeyBits).Run();
                            break;
                        case 3:
                            // check if secret is out of range, not in DOM
                            new OROnEVH(party, line, this, new NumericArray(lessZeroRe[0], lessZeroRe[1]), encReveal).Run();
                            break;
                        case 4:
                            // send encReveal to KH
                            var toKH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { encReveal[0] });
                            party.sender.SendTo(PartyType.KH, toKH2);
                            // receive keyReveal from KH
                            party.receiver.ReceiveFrom(PartyType.KH, line, this, keyReveal);
                            break;
                        case 5:
                            reveal = (encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1;
                            if (reveal)
                            {
                                party.sender.SendTo(
                                    PartyType.KH,
                                    Message.AssembleMessage(line, opType, false, new Numeric[] { encValTemp[0] }));
                                party.receiver.ReceiveFrom(PartyType.KH, line, this, key);
                            }
                            else
                            {
                                Run();
                            }
                            break;
                        case 6:
                            if (reveal)
                            {
                                encVal[0] = encValTemp[0] - key[0];
                                encVal[0].SetEncType(EncryptionType.None);
                            }
                            program.SetValue(variable, encVal[0]);
                            caller.Run();
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }
        }
    }

    class ConcealOnKH : OperationOnKH
    {
        public ConcealOnKH(Party party, int line, Operation caller, NumericArray operands, EVariable variable, Program program)
            : base(party, line, caller, operands, null, OperationType.Conceal)
        {
            this.program = program;
            this.variable = variable;
            encType = operands[0].GetEncType();
        }
        EVariable variable;
        EncryptionType encType;
        Numeric keyRand2;
        bool reveal;
        NumericArray
            encVal = new NumericArray(),
            lessZeroRe = new NumericArray(),
            isOutOfRange = new NumericArray(),
            keyRand1 = new NumericArray(),
            keyRand = new NumericArray(),
            encReveal = new NumericArray(),
            keyReveal = new NumericArray(),
            keyTemp = new NumericArray();
        protected override void OnKH()
        {
            // if variable does not require DOM/STATDOM, we simply assign the result to the variable 
            if (ReferenceEquals(variable.leftBoundary, null))
            {
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.rightBoundary, null));
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.prob, null));
                program.SetValue(variable, key[0]);
                caller.Run();
            }
            // if variable uses STATDOM
            else if (!ReferenceEquals(variable.leftBoundary, null) && !ReferenceEquals(variable.prob, null))
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.rightBoundary, null));
                // if val is not encrypted
                if (encType == EncryptionType.None)
                {
                    // if val is in the DOM, we need to encrypt it
                    if (variable.leftBoundary.GetVal() < key[0].GetVal() && key[0].GetVal() < variable.rightBoundary.GetVal())
                    {
                        key[0] = Utility.NextUnsignedNumeric(key[0].GetScaleBits());
                        key[0].SetEncType(Config.DefaultEnc);
                        party.sender.SendTo(
                            PartyType.EVH, 
                            Message.AssembleMessage(line, opType, false, new Numeric[] { key[0] }));
                        program.SetValue(variable, key[0]);
                        caller.Run();
                    }
                    else
                    {
                        key[0].SetEncType(EncryptionType.None);
                        program.SetValue(variable, key[0]);
                        caller.Run();
                    }
                }
                // else val is encrypted
                else
                {
                    switch (step)
                    {
                        case 1:
                            // if val is XOR encrypted, we need to convert it into AddMod encryption
                            if (encType == EncryptionType.XOR)
                            {
                                new XORToAddModOnKH(party, line, this, key, keyTemp).Run();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(encType == EncryptionType.AddMod);
                                Run();
                            }
                            break;
                        case 2:
                            // generate random number
                            Numeric rand2 = Utility.NextUnsignedNumeric(Config.ScaleBits, Config.ScaleBits);
                            keyRand2 = Utility.NextUnsignedNumeric(Config.ScaleBits);
                            var encRand2 = rand2 ^ keyRand2;

                            var toEVH1 = Message.AssembleMessage(line, opType, false, new Numeric[] { encRand2 });
                            party.sender.SendTo(PartyType.EVH, toEVH1);

                            party.receiver.ReceiveFrom(PartyType.EVH, line, this, keyRand1);
                            break;
                        case 3:
                            new XORToAddModOnKH(party, line, this, new NumericArray(keyRand2 ^ keyRand1[0]), keyRand).Run();
                            break;
                        case 4:
                            // compute rand < prob,  secret < leftBoundary and rightBoundary < secret
                            new LessZeroOnKH(party, line, this,
                                new NumericArray(keyTemp[0], new Numeric(0, keyTemp[0].GetScaleBits()) - keyTemp[0], keyRand[0]),
                                lessZeroRe, Config.KeyBits).Run();
                            break;
                        case 5:
                            // check if secret is out of range, not in DOM
                            new OROnKH(party, line, this, new NumericArray(lessZeroRe[0], lessZeroRe[1]), isOutOfRange).Run();
                            break;
                        case 6:
                            // check if secret can be revealed
                            new ANDOnKH(party, line, this, new NumericArray(isOutOfRange[0], lessZeroRe[2]), keyReveal).Run();
                            break;
                        case 7:
                            // send keyReveal to EVH
                            var toEVH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { keyReveal[0] });
                            party.sender.SendTo(PartyType.EVH, toEVH2);
                            // receive encReveal from EVH
                            party.receiver.ReceiveFrom(PartyType.EVH, line, this, encReveal);
                            break;
                        case 8:
                            reveal = (encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1;
                            if (reveal)
                            {
                                party.sender.SendTo(
                                    PartyType.EVH,
                                    Message.AssembleMessage(line, opType, false, new Numeric[] { keyTemp[0] }));
                                party.receiver.ReceiveFrom(PartyType.EVH, line, this, encVal);
                            }
                            else
                            {
                                Run();
                            }
                            break;
                        case 9:
                            if (reveal)
                            {
                                key[0] = encVal[0] - keyTemp[0];
                                key[0].SetEncType(EncryptionType.None);
                            }
                            program.SetValue(variable, key[0]);
                            caller.Run();
                            break;
                        default:
                            throw new Exception();
                    }

                }
            }
            // DOM
            else
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.leftBoundary, null));
                System.Diagnostics.Debug.Assert(!ReferenceEquals(variable.rightBoundary, null));
                System.Diagnostics.Debug.Assert(ReferenceEquals(variable.prob, null));
                // if val is not encrypted
                if (encType == EncryptionType.None)
                {
                    // if val is in the DOM, we need to encrypt it
                    if (variable.leftBoundary.GetVal() < key[0].GetVal() && key[0].GetVal() < variable.rightBoundary.GetVal())
                    {
                        key[0] = Utility.NextUnsignedNumeric(key[0].GetScaleBits());
                        key[0].SetEncType(Config.DefaultEnc);
                        party.sender.SendTo(
                            PartyType.EVH,
                            Message.AssembleMessage(line, opType, false, new Numeric[] { key[0] }));
                        program.SetValue(variable, key[0]);
                        caller.Run();
                    }
                    else
                    {
                        key[0].SetEncType(EncryptionType.None);
                        program.SetValue(variable, key[0]);
                        caller.Run();
                    }
                }
                // else val is encrypted
                else
                {
                    switch (step)
                    {
                        case 1:
                            // if val is XOR encrypted, we need to convert it into AddMod encryption
                            if (encType == EncryptionType.XOR)
                            {
                                new XORToAddModOnKH(party, line, this, key, keyTemp).Run();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(encType == EncryptionType.AddMod);
                                Run();
                            }
                            break;
                        case 2:
                            // compute secret < leftBoundary and rightBoundary < secret
                            new LessZeroOnKH(party, line, this,
                                new NumericArray(keyTemp[0], new Numeric(0, keyTemp[0].GetScaleBits()) - keyTemp[0]),
                                lessZeroRe, Config.KeyBits).Run();
                            break;
                        case 3:
                            // check if secret is out of range, not in DOM
                            new OROnKH(party, line, this, new NumericArray(lessZeroRe[0], lessZeroRe[1]), keyReveal).Run();
                            break;
                        case 4:
                            // send keyReveal to EVH
                            var toEVH2 = Message.AssembleMessage(line, opType, false, new Numeric[] { keyReveal[0] });
                            party.sender.SendTo(PartyType.EVH, toEVH2);
                            // receive encReveal from KH
                            party.receiver.ReceiveFrom(PartyType.EVH, line, this, encReveal);
                            break;
                        case 5:
                            reveal = (encReveal[0] ^ keyReveal[0]).GetUnsignedBigInteger() == 1;
                            if (reveal)
                            {
                                party.sender.SendTo(
                                    PartyType.EVH,
                                    Message.AssembleMessage(line, opType, false, new Numeric[] { keyTemp[0] }));
                                party.receiver.ReceiveFrom(PartyType.EVH, line, this, encVal);
                            }
                            else
                            {
                                Run();
                            }
                            break;
                        case 6:
                            if (reveal)
                            {
                                key[0] = encVal[0] - keyTemp[0];
                                key[0].SetEncType(EncryptionType.None);
                            }
                            program.SetValue(variable, key[0]);
                            caller.Run();
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }
        }
    }
}
