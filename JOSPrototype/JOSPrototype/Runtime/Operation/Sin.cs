using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System.Numerics;

namespace JOSPrototype.Runtime.Operation
{
    class SinOnEVH: OperationOnEVH
    {
        public SinOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Sin)
        { }
        public SinOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Sin)
        { }
        public SinOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Sin)
        { }
        int parallism;
        Numeric[] encKpppt0;
        NumericArray kpp = new NumericArray(), enckppppt1 = new NumericArray(), eak = new NumericArray();
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
                    {
                        Numeric enckaa = program.GetValue(code.operand1);
                        TransformEncType(enckaa);
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    encType = resultEncType;
                    // tested
                    if (!ReferenceEquals(code, null) && encVal[0].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        var re = new Numeric((BigInteger)(Math.Sin(encVal[0].GetVal()) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        eak = new NumericArray(re);
                        // jump to round 6
                        step = 5;
                        Run();
                        break;
                    }
                    new AddModToAddOnEVH(party, line, this, encVal, encVal).Run();
                    break;
                case 3:
                    parallism = encVal.Length;
                    encKpppt0 = new Numeric[parallism];
                    //int[] scaleBits = new int[parallism];
                    Numeric[]
                        t0 = new Numeric[parallism],
                        kppp = new Numeric[parallism];
                    //encVal = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        // to make sure a and k do not differ to much
                        if (encVal[p].GetScaleBits() < Config.ScaleBits)
                        {
                            encVal[p] = new Numeric(encVal[p].GetUnsignedBigInteger() << (Config.ScaleBits - encVal[p].GetScaleBits()), Config.ScaleBits);
                        }
                        //System.Diagnostics.Debug.WriteLine("a + k: "+encVal[p]);
                        t0[p] = new Numeric((BigInteger)(2 * Math.Sin(encVal[p].GetVal() / 2) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        //System.Diagnostics.Debug.WriteLine("sin((a + k) / 2): " + t0[p]);
                        kppp[p] = Utility.NextUnsignedNumeric(Config.ScaleBits);
                        encKpppt0[p] = t0[p] + kppp[p];
                    }
                    var toKH = Message.AssembleMessage(line, opType, false, kppp);
                    party.sender.SendTo(PartyType.KH, toKH);

                    party.receiver.ReceiveFrom(PartyType.KH, line, this, kpp);
                    break;
                case 4:
                    Numeric[] encMinusKPlusKpa = new Numeric[parallism];

                    for (int p = 0; p < parallism; ++p)
                    {
                        encMinusKPlusKpa[p] = encVal[p] + kpp[p];
                    }

                    var toHelper = Message.AssembleMessage(line, opType, true, encMinusKPlusKpa);
                    party.sender.SendTo(PartyType.Helper, toHelper);

                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enckppppt1);
                    break;
                case 5:
                    var enct0Andenct1 = new NumericArray(2 * parallism);
                    for (int p = 0; p < parallism; ++p)
                    {
                        enct0Andenct1[2 * p] = encKpppt0[p];
                        enct0Andenct1[2 * p + 1] = enckppppt1[p];
                    }
                    new MultiplicationOnEVH(party, line, this, enct0Andenct1, eak).Run();
                    break;
                case 6:
                    SetResult(encType, eak.GetArray());
                    break;
                case 7:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class SinOnKH : OperationOnKH
    {
        public SinOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Sin)
        { }
        public SinOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Sin)
        { }
        public SinOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Sin)
        { }
        int parallism;
        Numeric[] sk, kf;
        NumericArray kppp = new NumericArray(), kpppp = new NumericArray(), kak = new NumericArray();
        EncryptionType encType;
        protected override void OnKH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric ka = program.GetValue(code.operand1);
                        TransformEncType(ka);
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    parallism = key.Length;
                    encType = resultEncType;
                    kf = new Numeric[parallism];
                    if (!ReferenceEquals(code, null) && key[0].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        kf[0] = new Numeric((BigInteger)(Math.Sin(key[0].GetVal()) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);                       
                        // jump to round 7
                        step = 6;
                        Run();
                        break;
                    }
                    new AddModToAddOnKH(party, line, this, key, key).Run();
                    break;
                case 3:                
                    for (int p = 0; p < parallism; ++p)
                    {
                        // to make sure a and k do not differ to much
                        if (key[p].GetScaleBits() < Config.ScaleBits)
                        {
                            key[p] = new Numeric(key[p].GetUnsignedBigInteger() << (Config.ScaleBits - key[p].GetScaleBits()), Config.ScaleBits);
                        }
                        //System.Diagnostics.Debug.WriteLine("key: " + key[p]);
                    }
                    sk = new Numeric[parallism];
                    Numeric[]
                        kpp = new Numeric[parallism],                      
                        kp = new Numeric[parallism];
                    //key = new Numeric[parallism];
                    for (int p = 0; p < parallism; p++)
                    {
                        // key[p] = key[p].ModPow(GlobalSetting.EffectiveBitLength);
                        //scaleBits[p] = key[p].GetScaleBits();
                        kp[p] = Utility.NextUnsignedNumeric(key[p].GetScaleBits());
                        kpp[p] = kp[p] - key[p] - key[p];
                        //sk[p] = Numeric.Sin(key[p]);
                        sk[p] = new Numeric((BigInteger)(Math.Sin(key[p].GetVal()) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, kpp);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    var toHelper = Message.AssembleMessage(line, opType, false, kp);
                    party.sender.SendTo(PartyType.Helper, toHelper);

                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, kppp);
                    break;
                case 4:
                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, kpppp);
                    break;
                case 5:
                    var encKpppAndKpppp = new NumericArray(2 * parallism);
                    for (int p = 0; p < parallism; ++p)
                    {
                        encKpppAndKpppp[2 * p] = kppp[p];
                        encKpppAndKpppp[2 * p + 1] = kpppp[p];
                    }
                    new MultiplicationOnKH(party, line, this, encKpppAndKpppp, kak).Run();
                    break;
                case 6:
                    for (int p = 0; p < parallism; ++p)
                    {
                        kf[p] = kak[p] + sk[p];
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
    class SinOnHelper: OperationOnHelper
    {
        public SinOnHelper(Party party, int line)
            : base(party, line, OperationType.Sin)
        { }
        NumericArray encMinusKPlusKpa = new NumericArray(), kp = new NumericArray();
        protected override void OnHelper()
        {
            switch(step)
            {
                case 1:
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, encMinusKPlusKpa);
                    break;
                case 2:
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, kp);
                    break;
                case 3:
                    int parallism = kp.Length;
                    //int[] scaleBits = new int[parallism];
                    Numeric[]
                        encMinusKa = new Numeric[parallism],
                        t1 = new Numeric[parallism],
                        kpppp = new Numeric[parallism],
                        enckppppt1 = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        encMinusKa[p] = encMinusKPlusKpa[p] - kp[p];
                        //System.Diagnostics.Debug.WriteLine("a - k: " + encMinusKa[p]);
                        t1[p] = new Numeric((BigInteger)(Math.Cos(encMinusKa[p].GetVal() / 2) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        //System.Diagnostics.Debug.WriteLine("cos((a - k) / 2): " + t1[p]);
                        //t1[p] = Numeric.Cos(Numeric.DevideBy2(encMinusKa[p]));
                        kpppp[p] = Utility.NextUnsignedNumeric(Config.ScaleBits);
                        enckppppt1[p] = t1[p] + kpppp[p];
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, enckppppt1);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    var toKH = Message.AssembleMessage(line, opType, false, kpppp);
                    party.sender.SendTo(PartyType.KH, toKH);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class Sin : Operation
    //{
    //    public Sin() : base(EncryptionType.AddMod, EncryptionType.AddMod) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric enckaa = program.GetValue(code.operand1);
    //        Numeric[] encVal = new Numeric[] { TransformEncType(enckaa, party, code.index) };
    //        var enckfsina = OnEVHSin(party, encVal, code.index);
    //        enckfsina[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, enckfsina[0]);
    //    }

    //    public override void OnHelper(Party party, int line)
    //    {
    //        OnHelperSin(party, line);
    //    }

    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric ka = program.GetValue(code.operand1);
    //        Numeric[] key = new Numeric[] { TransformEncType(ka, party, code.index) };
    //        var kf = OnKHSin(party, key, code.index);
    //        kf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, kf[0]);
    //    }

    //    public static Numeric[] OnEVHSin(Party party, Numeric[] encVal, int line)
    //    {
    //        encVal = AddModToAdd.OnEVHAddModToAdd(party, encVal, line);

    //        int parallism = encVal.Length;
    //        //int[] scaleBits = new int[parallism];
    //        Numeric[]
    //            t0 = new Numeric[parallism],
    //            kppp = new Numeric[parallism],
    //            encKpppt0 = new Numeric[parallism];
    //            //encVal = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            // to make sure a and k do not differ to much
    //            if (encVal[p].GetScaleBits() < Config.FractionBitLength)
    //            {
    //                encVal[p] = new Numeric(encVal[p].GetUnsignedBigInteger() << (Config.FractionBitLength - encVal[p].GetScaleBits()), Config.FractionBitLength);
    //            }
    //            //System.Diagnostics.Debug.WriteLine("a + k: "+encVal[p]);
    //            t0[p] = new Numeric((BigInteger)(2 * Math.Sin(encVal[p].GetVal() / 2) * Math.Pow(2, Config.FractionBitLength)), Config.FractionBitLength);
    //            //System.Diagnostics.Debug.WriteLine("sin((a + k) / 2): " + t0[p]);
    //            kppp[p] = Utility.NextUnsignedNumeric(Config.FractionBitLength);
    //            encKpppt0[p] = t0[p] + kppp[p];
    //        }
    //        var toKH = Message.AssembleMessage(line, OperationType.Sin, false, kppp);
    //        party.sender.SendTo(PartyType.KH, toKH);

    //        var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        var kpp = Message.DisassembleMessage(fromKH);
    //        Numeric[] encMinusKPlusKpa = new Numeric[parallism];

    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            encMinusKPlusKpa[p] = encVal[p] + kpp[p];
    //        }

    //        var toHelper = Message.AssembleMessage(line, OperationType.Sin, true, encMinusKPlusKpa);
    //        party.sender.SendTo(PartyType.Helper, toHelper);

    //        var fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
    //        var enckppppt1 = Message.DisassembleMessage(fromHelper);

    //        var enct0Andenct1 = new Numeric[2 * parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            enct0Andenct1[2 * p] = encKpppt0[p];
    //            enct0Andenct1[2 * p + 1] = enckppppt1[p];
    //        }

    //        var eak = Multiplication.OnEVH(party, enct0Andenct1, line);
    //        return eak;
    //    }

    //    public static void OnHelperSin(Party party, int line)
    //    {
    //        var fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
    //        var encMinusKPlusKpa = Message.DisassembleMessage(fromEVH);

    //        var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        var kp = Message.DisassembleMessage(fromKH);

    //        int parallism = kp.Length;
    //        //int[] scaleBits = new int[parallism];
    //        Numeric[]
    //            encMinusKa = new Numeric[parallism],
    //            t1 = new Numeric[parallism],
    //            kpppp = new Numeric[parallism],
    //            enckppppt1 = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            encMinusKa[p] = encMinusKPlusKpa[p] - kp[p];
    //            //System.Diagnostics.Debug.WriteLine("a - k: " + encMinusKa[p]);
    //            t1[p] = new Numeric((BigInteger)(Math.Cos(encMinusKa[p].GetVal() / 2) * Math.Pow(2, Config.FractionBitLength)), Config.FractionBitLength);
    //            //System.Diagnostics.Debug.WriteLine("cos((a - k) / 2): " + t1[p]);
    //            //t1[p] = Numeric.Cos(Numeric.DevideBy2(encMinusKa[p]));
    //            kpppp[p] = Utility.NextUnsignedNumeric(Config.FractionBitLength);
    //            enckppppt1[p] = t1[p] + kpppp[p];
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.Sin, false, enckppppt1);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var toKH = Message.AssembleMessage(line, OperationType.Sin, false, kpppp);
    //        party.sender.SendTo(PartyType.KH, toKH);
    //    }

    //    public static Numeric[] OnKHSin(Party party, Numeric[] key, int line)
    //    {
    //        key = AddModToAdd.OnKHAddModToAdd(party, key, line, Config.NumericBitLength, true);
    //        int parallism = key.Length;
            
    //        for(int p = 0; p < parallism; ++p)
    //        {
    //            // to make sure a and k do not differ to much
    //            if (key[p].GetScaleBits() < Config.FractionBitLength)
    //            {
    //                key[p] = new Numeric(key[p].GetUnsignedBigInteger() << (Config.FractionBitLength - key[p].GetScaleBits()), Config.FractionBitLength);
    //            }
    //            //System.Diagnostics.Debug.WriteLine("key: " + key[p]);
    //        }
    //        Numeric[]
    //            kpp = new Numeric[parallism],
    //            sk = new Numeric[parallism],
    //            kp = new Numeric[parallism];
    //            //key = new Numeric[parallism];
    //        for (int p = 0; p < parallism; p++)
    //        {
    //            // key[p] = key[p].ModPow(GlobalSetting.EffectiveBitLength);
    //            //scaleBits[p] = key[p].GetScaleBits();
    //            kp[p] = Utility.NextUnsignedNumeric(key[p].GetScaleBits());
    //            kpp[p] = kp[p] - key[p] - key[p];
    //            //sk[p] = Numeric.Sin(key[p]);
    //            sk[p] = new Numeric((BigInteger)(Math.Sin(key[p].GetVal()) * Math.Pow(2, Config.FractionBitLength)), Config.FractionBitLength);
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.Sin, false, kpp);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var toHelper = Message.AssembleMessage(line, OperationType.Sin, false, kp);
    //        party.sender.SendTo(PartyType.Helper, toHelper);

    //        var fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
    //        var kppp = Message.DisassembleMessage(fromEVH);
            
    //        var fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
    //        var kpppp = Message.DisassembleMessage(fromHelper);

    //        var encKpppAndKpppp = new Numeric[2 * parallism];
    //        for(int p = 0; p < parallism; ++p)
    //        {
    //            encKpppAndKpppp[2 * p] = kppp[p];
    //            encKpppAndKpppp[2 * p + 1] = kpppp[p];
    //        }
    //        var kak = Multiplication.OnKHMultiplication(party, encKpppAndKpppp, line);

    //        var kf = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            kf[p] = kak[p] + sk[p];
    //        }
    //        return kf;
    //    }
    //}
}
