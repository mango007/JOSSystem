using System;
using System.Collections.Generic;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System.Numerics;

namespace JOSPrototype.Runtime.Operation
{
    class MultiplicationOnEVH: OperationOnEVH
    {
        public MultiplicationOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Multiplication)
        { }
        public MultiplicationOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Multiplication)
        { }
        public MultiplicationOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Multiplication)
        { }
        int parallelism;
        byte[] scaleBits;
        Numeric[] enckbplusk2b, enckaa, enckfab;
        NumericArray k6 = new NumericArray(), enck7p7iAndK5 = new NumericArray(), enc_newKey_a;
        List<Numeric> er = new List<Numeric>();
        Dictionary<int, int> erIdxMap = new Dictionary<int, int>();
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric
                            enc_ka_a = program.GetValue(code.operand1),
                            enc_kb_b = program.GetValue(code.operand2);                        
                        TransformEncType(enc_ka_a, enc_kb_b);
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    encType = resultEncType;
                    parallelism = encVal.Length / 2;
                    enckfab = new Numeric[parallelism];
                    scaleBits = new byte[parallelism];
                    if (!ReferenceEquals(code, null))
                    {
                        scaleBits[0] = Numeric.Scale(encVal[0], encVal[1]);
                        // both not encrypted
                        // **tested**
                        if (encVal[0].GetEncType() == EncryptionType.None && encVal[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            if (scaleBits[0] == 0)
                            {
                                enckfab[0] = encVal[0] * encVal[1];
                                enckfab[0].SetScaleBits(0);
                            }
                            else
                            {
                                enckfab[0] = new Numeric((encVal[0].GetSignedBigInteger() * encVal[1].GetSignedBigInteger()) >> scaleBits[0], scaleBits[0]);
                            }
                            // jump to round 7
                            step = 6;
                            Run();
                            break;
                        }
                        // one is encrypted, the other is not
                        else if(encVal[0].GetEncType() == EncryptionType.None || encVal[1].GetEncType() == EncryptionType.None)
                        {
                          
                            // **tested**
                            if (scaleBits[0] == 0)
                            {
                                enckfab[0] = encVal[0] * encVal[1];
                                enckfab[0].SetScaleBits(0);
                                // jump to round 7
                                step = 6;
                                Run();
                                break;
                            }
                            // **tested**
                            else
                            {
                                // if both operands are fixed-pointer integers,
                                // we treat the non-encryped operand as if the encrpted value is the original value and the key is 0,
                                // then we use the confidential protocol to computer the result.


                                //if (encVal[0].GetEncType() == EncryptionType.None)
                                //{
                                //    enckfab[0] = new Numeric(encVal[0].GetSignedBigInteger() * encVal[1].GetUnsignedBigInteger(), 0);
                                //}
                                //else
                                //{
                                //    enckfab[0] = new Numeric(encVal[0].GetUnsignedBigInteger() * encVal[1].GetSignedBigInteger(), 0);
                                //}
                                //erIdxMap.Add(0, 0);
                                //er.Add(enckfab[0]);
                                //// jump to round 5
                                //round = 4;
                                //Run();
                                //break;
                            }
                        }
                    }                           
                    enckbplusk2b = new Numeric[parallelism];
                    enckaa = new Numeric[parallelism];
                    Numeric[]
                        enckaaAndK2 = new Numeric[2 * parallelism],               
                        enckbb = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        enckaa[p] = new Numeric(encVal[2 * p]);
                        enckbb[p] = new Numeric(encVal[2 * p + 1]);
                        System.Diagnostics.Debug.Assert(enckaa[p].GetScaleBits() == enckbb[p].GetScaleBits());
                        scaleBits[p] = enckaa[p].GetScaleBits();
                        enckaa[p].ResetScaleBits();
                        enckbb[p].ResetScaleBits();
                        // System.Diagnostics.Debug.Assert(encVal[2 * p + 1].GetScalingFactor() == scalingFactor[p]);            
                        // byte[] k2 = fixedKey2;
                        Numeric
                            k2 = Utility.NextUnsignedNumeric(0);
                        enckbplusk2b[p] = enckbb[p] + k2;
                        enckaaAndK2[2 * p] = enckaa[p];
                        enckaaAndK2[2 * p + 1] = k2;
                    }

                    var toKH = Message.AssembleMessage(line, opType, false, enckbplusk2b);
                    party.sender.SendTo(PartyType.KH, toKH);

                    var toHelper = Message.AssembleMessage(line, opType, true, enckaaAndK2);
                    party.sender.SendTo(PartyType.Helper, toHelper);
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, k6);
                    break;
                case 3:
                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enck7p7iAndK5);
                    break;
                case 4:
                                                
                    int count = 0;
                    for (int p = 0; p < parallelism; ++p)
                    {
                        //System.Diagnostics.Debug.Assert(k6[p].GetScalingFactor() == scalingFactor[p]);
                        //System.Diagnostics.Debug.Assert(enck7p7iAndK5[2 * p].GetScalingFactor() == scalingFactor[p]);
                        //System.Diagnostics.Debug.Assert(enck7p7iAndK5[2 * p + 1].GetScalingFactor() == scalingFactor[p]);
                        Numeric
                            t0 = enckaa[p] * enckbplusk2b[p],
                            enck7p7 = enck7p7iAndK5[2 * p],
                            k5 = enck7p7iAndK5[2 * p + 1];
                        enckfab[p] = t0 + enck7p7 + ((new Numeric(0, 0) - k5) * (new Numeric(0, 0) - k6[p]));
                        if (scaleBits[p] != 0)
                        {
                            erIdxMap.Add(count++, p);
                            //er.Add(new Numeric(enckfab[p].ToUnsignedBigInteger() & Numeric.oneMaskMap[scalingFactor[p]], 0));
                            er.Add(enckfab[p]);
                        }
                    }
                    Run();
                    break;
                case 5:
                    if (er.Count != 0)
                    {
                        enc_newKey_a = new NumericArray();
                        new AddModToAddOnEVH(party, line, this, new NumericArray(er.ToArray()), enc_newKey_a).Run();
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 6:
                    if(er.Count != 0)
                    {
                        for (int i = 0; i < er.Count; ++i)
                        {
                            //System.Diagnostics.Debug.WriteLine(enc_newKey_a[i].GetUnsignedBigInteger());
                            //System.Diagnostics.Debug.WriteLine(enc_newKey_a[i].GetSignedBigInteger());
                            System.Diagnostics.Debug.Assert(enc_newKey_a[i].GetUnsignedBigInteger() == enc_newKey_a[i].GetSignedBigInteger());
                            enckfab[erIdxMap[i]] = new Numeric(enc_newKey_a[i].GetUnsignedBigInteger() >> scaleBits[erIdxMap[i]], scaleBits[erIdxMap[i]]);
                            // enckfab[erIdxMap[i]] = new Numeric((BigInteger)(enc_paddedKey_a[i].GetVal() / Math.Pow(2, scaleBits[erIdxMap[i]])), scaleBits[erIdxMap[i]]);
                        }
                    }
                    Run();
                    break;
                case 7:
                    SetResult(encType, enckfab);
                    break;
                case 8:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class MultiplicationOnKH: OperationOnKH
    {
        public MultiplicationOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Multiplication)
        { }
        public MultiplicationOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Multiplication)
        { }
        public MultiplicationOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Multiplication)
        { }
        int parallelism;
        byte[] scaleBits;
        Numeric[] k6, ka, kf;
        NumericArray enckbplusk2b = new NumericArray(), enck5kbplusk2AndK7 = new NumericArray(), newKey;
        List<Numeric> kr = new List<Numeric>();
        Dictionary<int, int> krIdxMap = new Dictionary<int, int>();
        EncryptionType encType;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric
                            k1 = program.GetValue(code.operand1),
                            k2 = program.GetValue(code.operand2);                       
                        TransformEncType(k1, k2);
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    parallelism = key.Length / 2;
                    scaleBits = new byte[parallelism];
                    kf = new Numeric[parallelism];
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        scaleBits[0] = Numeric.Scale(key[0], key[1]);
                        // both not encrypted                 
                        if (key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            if (scaleBits[0] == 0)
                            {
                                kf[0] = key[0] * key[1];
                                kf[0].SetScaleBits(0);
                            }
                            else
                            {
                                kf[0] = new Numeric((key[0].GetSignedBigInteger() * key[1].GetSignedBigInteger()) >> scaleBits[0], scaleBits[0]);
                            }
                            // jump to round 7
                            step = 6;
                            Run();
                            break;
                        }
                        // one is encrypted, the other is not
                        else if (key[0].GetEncType() == EncryptionType.None || key[1].GetEncType() == EncryptionType.None)
                        {                         
                            if(scaleBits[0] == 0)
                            {
                                kf[0] = key[0] * key[1];
                                kf[0].SetScaleBits(0);
                                // jump to round 7
                                step = 6;
                                Run();
                                break;
                            }
                            else
                            {
                                // if both operands are fixed-pointer integers,
                                // we treat the non-encryped operand as if the encrpted value is the original value and the key is 0,
                                // then we use the confidential protocol to computer the result. 
                                if (key[0].GetEncType() == EncryptionType.None)
                                {
                                    key[0] = new Numeric(0, scaleBits[0]);
                                    //kf[0] = new Numeric(key[0].GetSignedBigInteger() * key[1].GetUnsignedBigInteger(), 0);
                                }
                                else
                                {
                                    key[1] = new Numeric(0, scaleBits[0]);
                                    //kf[0] = new Numeric(key[0].GetUnsignedBigInteger() * key[1].GetSignedBigInteger(), 0);
                                }
                                //krIdxMap.Add(0, 0);
                                //kr.Add(key[0]);
                                //// jump to round 5
                                //round = 4;
                                //Run();
                                //break;
                            }
                        }
                    }
                    
                    k6 = new Numeric[parallelism];
                    ka = new Numeric[parallelism];
                    Numeric[]             
                        kbAndEnck6ka = new Numeric[2 * parallelism],             
                        kb = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        ka[p] = new Numeric(key[2 * p]);
                        kb[p] = new Numeric(key[2 * p + 1]);
                        System.Diagnostics.Debug.Assert(ka[p].GetScaleBits() == kb[p].GetScaleBits());
                        scaleBits[p] = ka[p].GetScaleBits();
                        ka[p].ResetScaleBits();
                        kb[p].ResetScaleBits();
                        k6[p] = Utility.NextUnsignedNumeric(0);
                        Numeric enck6ka = (new Numeric(0, 0) - ka[p]) + k6[p];
                        kbAndEnck6ka[2 * p] = kb[p];
                        kbAndEnck6ka[2 * p + 1] = enck6ka;
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, k6);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    byte[] toHelper = Message.AssembleMessage(line, opType, false, kbAndEnck6ka);
                    party.sender.SendTo(PartyType.Helper, toHelper);

                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enck5kbplusk2AndK7);
                    break;
                case 3:
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, enckbplusk2b);
                    break;
                case 4:
                    int count = 0;
                    for (int p = 0; p < parallelism; ++p)
                    {
                        //System.Diagnostics.Debug.Assert(enck5kbplusk2AndK7[2 * p].GetScalingFactor() == scalingFactor[p]);
                        //System.Diagnostics.Debug.Assert(enck5kbplusk2AndK7[2 * p + 1].GetScalingFactor() == scalingFactor[p]);
                        //System.Diagnostics.Debug.Assert(enckbplusk2b[p].GetScalingFactor() == scalingFactor[p]);
                        Numeric
                            enck5kbplusk2 = enck5kbplusk2AndK7[2 * p],
                            k7 = enck5kbplusk2AndK7[2 * p + 1],
                            t2 = (new Numeric(0, 0) - ka[p]) * enckbplusk2b[p],
                            t3 = (new Numeric(0, 0) - k6[p]) * enck5kbplusk2;
                        kf[p] = k7 - t2 - t3;
                        if (scaleBits[p] != 0)
                        {
                            krIdxMap.Add(count++, p);
                            kr.Add(kf[p]);
                        }
                    }
                    Run();
                    break;
                case 5:

                    if (kr.Count != 0)
                    {
                        newKey = new NumericArray();
                        new AddModToAddOnKH(party, line, this, new NumericArray(kr.ToArray()), newKey).Run();
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 6:
                    if(kr.Count != 0)
                    {
                        for (int i = 0; i < kr.Count; ++i)
                        {
                            System.Diagnostics.Debug.Assert(newKey[i].GetUnsignedBigInteger() == newKey[i].GetSignedBigInteger());
                            kf[krIdxMap[i]] = new Numeric(newKey[i].GetUnsignedBigInteger() >> scaleBits[krIdxMap[i]], scaleBits[krIdxMap[i]]);
                            //kf[krIdxMap[i]] = new Numeric((BigInteger)(newKey[i].GetVal() / Math.Pow(2, scaleBits[krIdxMap[i]])), scaleBits[krIdxMap[i]]);
                            //kf[krIdxMap[i]].SetEncType(EncryptionType.AddMod);
                        }
                    }
                    Run();
                    break;
                case 7:
                    SetResult(encType, kf);
                    break;
                case 8:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class MultiplicationOnHelper: OperationOnHelper
    {
        public MultiplicationOnHelper(Party party, int line)
            : base(party, line, OperationType.Multiplication)
        { }
        NumericArray enckaaAndK2 = new NumericArray(), kbAndEnck6ka = new NumericArray();
        protected override void OnHelper()
        {
            switch(step)
            {
                case 1:
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, enckaaAndK2);
                    break;
                case 2:
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, kbAndEnck6ka);
                    break;
                case 3:
                    int parallelism = kbAndEnck6ka.Length / 2;
                    Numeric[] enck7p7AndK5 = new Numeric[2 * parallelism], encK5KbPlusK2AndK7 = new Numeric[2 * parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        Numeric
                            k5 = Utility.NextUnsignedNumeric(0),
                            k7 = Utility.NextUnsignedNumeric(0),
                            enckaa = enckaaAndK2[2 * p],
                            k2 = enckaaAndK2[2 * p + 1],
                            kb = kbAndEnck6ka[2 * p],
                            enck6ka = kbAndEnck6ka[2 * p + 1],
                            temp = (new Numeric(0, 0) - (kb + k2)),
                            encK5KbPlusK2 = temp + k5,
                            t1 = enckaa * temp,
                            enck7p7 = t1 + enck6ka * temp + k7;
                        enck7p7AndK5[2 * p] = enck7p7;
                        enck7p7AndK5[2 * p + 1] = k5;
                        encK5KbPlusK2AndK7[2 * p] = encK5KbPlusK2;
                        encK5KbPlusK2AndK7[2 * p + 1] = k7;
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, enck7p7AndK5);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    byte[] toKH = Message.AssembleMessage(line, opType, false, encK5KbPlusK2AndK7);
                    party.sender.SendTo(PartyType.KH, toKH);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class Multiplication : Operation
    //{
    //    public Multiplication() : base(EncryptionType.AddMod, EncryptionType.AddMod) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            enckaa = program.GetValue(code.operand1),
    //            enckbb = program.GetValue(code.operand2);
    //        Numeric.Scale(enckaa, enckbb);
    //        // Mutiplication needs AddMod encryption
    //        Numeric[] encVal = TransformEncType(enckaa, enckbb, party, code.index);
    //        var enckfatimesb = OnEVHMultiplication(party, encVal, code.index);
    //        enckfatimesb[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, enckfatimesb[0]);
    //    }

    //    public override void OnHelper(Party party, int line)
    //    {
    //        OnHelperMultiplication(party, line);
    //    }

    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            ka = program.GetValue(code.operand1),
    //            kb = program.GetValue(code.operand2);
    //        Numeric.Scale(ka, kb);
    //        Numeric[] key = TransformEncType(ka, kb, party, code.index);
    //        var kf = OnKHMultiplication(party, key, code.index);
    //        kf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, kf[0]);
    //    }
    //    //will modify the input parameters encVal, make a copy of the input
    //    //result is AddMod encrypted
    //    public static Numeric[] OnEVHMultiplication(Party party, Numeric[] encVal, int line)
    //    {
    //        int parallelism = encVal.Length / 2;
    //        int[] scaleBits = new int[parallelism];
    //        Numeric[]
    //            enckbplusk2b = new Numeric[parallelism],
    //            enckaaAndK2 = new Numeric[2 * parallelism],
    //            enckaa = new Numeric[parallelism],
    //            enckbb = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            enckaa[p] = new Numeric(encVal[2 * p]);
    //            enckbb[p] = new Numeric(encVal[2 * p + 1]);
    //            System.Diagnostics.Debug.Assert(enckaa[p].GetScaleBits() == enckbb[p].GetScaleBits());
    //            scaleBits[p] = enckaa[p].GetScaleBits();
    //            enckaa[p].ResetScaleBits();
    //            enckbb[p].ResetScaleBits();
    //            // System.Diagnostics.Debug.Assert(encVal[2 * p + 1].GetScalingFactor() == scalingFactor[p]);            
    //            // byte[] k2 = fixedKey2;
    //            Numeric
    //                k2 = Utility.NextUnsignedNumeric(0);
    //            enckbplusk2b[p] = enckbb[p] + k2;
    //            enckaaAndK2[2 * p] = enckaa[p];
    //            enckaaAndK2[2 * p + 1] = k2;
    //        }

    //        var toKH = Message.AssembleMessage(line, OperationType.Multiplication, false, enckbplusk2b);
    //        party.sender.SendTo(PartyType.KH, toKH);

    //        var toHelper = Message.AssembleMessage(line, OperationType.Multiplication, true, enckaaAndK2);
    //        party.sender.SendTo(PartyType.Helper, toHelper);

    //        byte[] fromKH = party.receiver.ReceiveFrom(PartyType.KH, line),
    //               fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
    //        System.Diagnostics.Debug.Assert(fromKH.Length == parallelism * Numeric.Size());
    //        System.Diagnostics.Debug.Assert(fromHelper.Length == parallelism * Numeric.Size() * 2);
    //        var k6 = Message.DisassembleMessage(fromKH);
    //        var enck7p7iAndK5 = Message.DisassembleMessage(fromHelper);
    //        Numeric[] enckfab = new Numeric[parallelism];
    //        List<Numeric> er = new List<Numeric>();
    //        Dictionary<int, int> erIdxMap = new Dictionary<int, int>();
    //        int count = 0;
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            //System.Diagnostics.Debug.Assert(k6[p].GetScalingFactor() == scalingFactor[p]);
    //            //System.Diagnostics.Debug.Assert(enck7p7iAndK5[2 * p].GetScalingFactor() == scalingFactor[p]);
    //            //System.Diagnostics.Debug.Assert(enck7p7iAndK5[2 * p + 1].GetScalingFactor() == scalingFactor[p]);
    //            Numeric
    //                t0 = enckaa[p] * enckbplusk2b[p],
    //                enck7p7 = enck7p7iAndK5[2 * p],
    //                k5 = enck7p7iAndK5[2 * p + 1];
    //            enckfab[p] = t0 + enck7p7 + ((new Numeric(0, 0) - k5) * (new Numeric(0, 0) - k6[p]));
    //            if (scaleBits[p] != 0)
    //            {
    //                erIdxMap.Add(count++, p);
    //                //er.Add(new Numeric(enckfab[p].ToUnsignedBigInteger() & Numeric.oneMaskMap[scalingFactor[p]], 0));
    //                er.Add(enckfab[p]);
    //            }
    //            else
    //            {
    //                enckfab[p].SetEncType(EncryptionType.AddMod);
    //            }
    //        }
    //        if (er.Count != 0)
    //        {
    //            var enc_paddedKey_a = AddModToAdd.OnEVHAddModToAdd(party, er.ToArray(), line);

    //            for (int i = 0; i < er.Count; ++i)
    //            {
    //                enckfab[erIdxMap[i]] = new Numeric((BigInteger)(enc_paddedKey_a[i].GetVal() / Math.Pow(2, scaleBits[erIdxMap[i]])), scaleBits[erIdxMap[i]]);
    //                enckfab[erIdxMap[i]].SetEncType(EncryptionType.AddMod);
    //            }
    //        }
    //        return enckfab;
    //    }

    //    public static void OnHelperMultiplication(Party party, int line)
    //    {
    //        byte[] fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line),
    //               fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        System.Diagnostics.Debug.Assert(fromEVH.Length == fromKH.Length);
    //        int parallelism = fromEVH.Length / Numeric.Size() / 2;
    //        var enckaaAndK2 = Message.DisassembleMessage(fromEVH);
    //        var kbAndEnck6ka = Message.DisassembleMessage(fromKH);

    //        Numeric[] enck7p7AndK5 = new Numeric[2 * parallelism], encK5KbPlusK2AndK7 = new Numeric[2 * parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            Numeric
    //                k5 = Utility.NextUnsignedNumeric(0),
    //                k7 = Utility.NextUnsignedNumeric(0),
    //                enckaa = enckaaAndK2[2 * p],
    //                k2 = enckaaAndK2[2 * p + 1],
    //                kb = kbAndEnck6ka[2 * p],
    //                enck6ka = kbAndEnck6ka[2 * p + 1],
    //                temp = (new Numeric(0, 0) - (kb + k2)),
    //                encK5KbPlusK2 = temp + k5,
    //                t1 = enckaa * temp,
    //                enck7p7 = t1 + enck6ka * temp + k7;
    //            enck7p7AndK5[2 * p] = enck7p7;
    //            enck7p7AndK5[2 * p + 1] = k5;
    //            encK5KbPlusK2AndK7[2 * p] = encK5KbPlusK2;
    //            encK5KbPlusK2AndK7[2 * p + 1] = k7;
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.Multiplication, false, enck7p7AndK5);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        byte[] toKH = Message.AssembleMessage(line, OperationType.Multiplication, false, encK5KbPlusK2AndK7);
    //        party.sender.SendTo(PartyType.KH, toKH);
    //    }

    //    public static Numeric[] OnKHMultiplication(Party party, Numeric[] key, int line)
    //    {
    //        int parallelism = key.Length / 2;
    //        int[] scaleBits = new int[parallelism];

    //        Numeric[]
    //            k6 = new Numeric[parallelism],
    //            kbAndEnck6ka = new Numeric[2 * parallelism],
    //            ka = new Numeric[parallelism],
    //            kb = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            ka[p] = new Numeric(key[2 * p]);
    //            kb[p] = new Numeric(key[2 * p + 1]);
    //            System.Diagnostics.Debug.Assert(ka[p].GetScaleBits() == kb[p].GetScaleBits());
    //            scaleBits[p] = ka[p].GetScaleBits();
    //            ka[p].ResetScaleBits();
    //            kb[p].ResetScaleBits();
    //            k6[p] = Utility.NextUnsignedNumeric(0);
    //            Numeric enck6ka = (new Numeric(0, 0) - ka[p]) + k6[p];
    //            kbAndEnck6ka[2 * p] = kb[p];
    //            kbAndEnck6ka[2 * p + 1] = enck6ka;
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.Multiplication, false, k6);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        byte[] toHelper = Message.AssembleMessage(line, OperationType.Multiplication, false, kbAndEnck6ka);
    //        party.sender.SendTo(PartyType.Helper, toHelper);

    //        byte[] fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line), fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
    //        var enck5kbplusk2AndK7 = Message.DisassembleMessage(fromHelper);
    //        var enckbplusk2b = Message.DisassembleMessage(fromEVH);
    //        Numeric[] kf = new Numeric[parallelism];
    //        List<Numeric> kr = new List<Numeric>();
    //        Dictionary<int, int> krIdxMap = new Dictionary<int, int>();
    //        int count = 0;
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            //System.Diagnostics.Debug.Assert(enck5kbplusk2AndK7[2 * p].GetScalingFactor() == scalingFactor[p]);
    //            // System.Diagnostics.Debug.Assert(enck5kbplusk2AndK7[2 * p + 1].GetScalingFactor() == scalingFactor[p]);
    //            //System.Diagnostics.Debug.Assert(enckbplusk2b[p].GetScalingFactor() == scalingFactor[p]);
    //            Numeric
    //                enck5kbplusk2 = enck5kbplusk2AndK7[2 * p],
    //                k7 = enck5kbplusk2AndK7[2 * p + 1],
    //                t2 = (new Numeric(0, 0) - ka[p]) * enckbplusk2b[p],
    //                t3 = (new Numeric(0, 0) - k6[p]) * enck5kbplusk2;
    //            kf[p] = k7 - t2 - t3;
    //            if (scaleBits[p] != 0)
    //            {
    //                krIdxMap.Add(count++, p);
    //                kr.Add(kf[p]);
    //            }
    //            else
    //            {
    //                kf[p].SetEncType(EncryptionType.AddMod);
    //            }
    //        }
    //        if (kr.Count != 0)
    //        {
    //            Numeric[] paddedKey = AddModToAdd.OnKHAddModToAdd(party, kr.ToArray(), line, Config.NumericBitLength, true);

    //            for (int i = 0; i < kr.Count; ++i)
    //            {
    //                kf[krIdxMap[i]] = new Numeric((BigInteger)(paddedKey[i].GetVal() / Math.Pow(2, scaleBits[krIdxMap[i]])), scaleBits[krIdxMap[i]]);
    //                kf[krIdxMap[i]].SetEncType(EncryptionType.AddMod);
    //            }
    //        }
    //        return kf;
    //    }
    //}
}
