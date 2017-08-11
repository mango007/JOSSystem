using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class LessZeroOnEVH: OperationOnEVH
    {
        public LessZeroOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.LessZero)
        {
            this.length = length;
        }
        public LessZeroOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, OperationType.LessZero)
        { }
        public LessZeroOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, caller, OperationType.LessZero)
        { }
        int length, round2 = 0;
        NumericArray EncKp1b, EncKppbk, EncKp,
            Enck1lle0andEncK2rle0, ElandEr, ANDResult, El;
        Numeric[] re;
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
                    {
                        Numeric enckaa = program.GetValue(code.operand1);
                        enckaa.ResetScaleBits();
                        length = Config.KeyBits;
                        TransformEncType(enckaa);
                    }
                    else
                    {
                        for (int i = 0; i < encVal.Length; ++i)
                        {
                            if (encVal[i].GetScaleBits() != 0)
                            {
                                encVal[i] = new Numeric(encVal[i]);
                                encVal[i].ResetScaleBits();
                            }
                        }
                        Run();
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(length > 0);
                    encType = resultEncType;
                    round2++;
                    // **tested**
                    // input value is not encrypted
                    if (!ReferenceEquals(code, null) && encVal[0].GetEncType() == EncryptionType.None)
                    {
                        switch(round2)
                        {
                            case 1:
                                encType = EncryptionType.None;
                                re = new Numeric[1];
                                if (encVal[0].GetSignedBigInteger() < 0)
                                {
                                    re[0] = new Numeric(1, 0);
                                }
                                else
                                {
                                    re[0] = new Numeric(0, 0);
                                }
                                SetResult(encType, re);
                                break;
                            case 2:
                                InvokeCaller();
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                    else
                    {
                        if (length == 1)
                        {
                            switch (round2)
                            {
                                case 1:
                                    EncKp1b = new NumericArray();
                                    new EqualZeroOnEVH(party, line, this, encVal, EncKp1b, length).Run();
                                    break;
                                case 2:
                                    var ANDOperand1 = new NumericArray(2 * encVal.Length);
                                    for (int p = 0; p < encVal.Length; ++p)
                                    {
                                        //EncKp1b[p].SetScaleBits(encVal[p].GetScaleBits());
                                        ANDOperand1[2 * p] = EncKp1b[p];
                                        ANDOperand1[2 * p + 1] = new Numeric(0);
                                    }
                                    EncKppbk = new NumericArray();
                                    new ANDOnEVH(party, line, this, ANDOperand1, EncKppbk).Run();
                                    break;
                                case 3:
                                    re = new Numeric[encVal.Length];
                                    for (int p = 0; p < encVal.Length; ++p)
                                    {
                                        re[p] = EncKppbk[p].ModPow(1);
                                        re[p].ResetScaleBits();
                                    }
                                    //System.Diagnostics.Debug.WriteLine("EVH: length: 1");
                                    //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}",
                                    //    "EVH, EncKppbkTemp :", EncKppbkTemp[0].GetUnsignedBigInteger(), ", encVal ", encVal[0].GetUnsignedBigInteger(), ", EncKppbk: ", EncKppbk[0].GetUnsignedBigInteger(), ", length: ", length));
                                    SetResult(encType, re);
                                    break;
                                case 4:
                                    InvokeCaller();
                                    break;
                                default:
                                    throw new Exception();
                            }
                        }
                        else
                        {
                            switch (round2)
                            {
                                case 1:
                                    El = new NumericArray(encVal.Length);
                                    ElandEr = new NumericArray(encVal.Length * 2);

                                    for (int p = 0; p < encVal.Length; ++p)
                                    {
                                        El[p] = encVal[p] >> (length / 2);
                                        ElandEr[2 * p] = El[p];
                                        ElandEr[2 * p + 1] = encVal[p].ModPow(length / 2);
                                    }
                                    EncKp = new NumericArray();
                                    new EqualZeroOnEVH(party, line, this, El, EncKp, length).Run();
                                    break;
                                case 2:
                                    System.Diagnostics.Debug.Assert(length - length / 2 == length / 2);
                                    Enck1lle0andEncK2rle0 = new NumericArray();
                                    new LessZeroOnEVH(party, line, this, ElandEr, Enck1lle0andEncK2rle0, length / 2).Run();
                                    //    Enck1lle0 = new NumericArray();
                                    //    new LessZeroOnEVH(party, line, this, El, Enck1lle0, length - length / 2).Run();
                                    //    break;
                                    //case 3:
                                    //    EncK2rle0 = new NumericArray();
                                    //    new LessZeroOnEVH(party, line, this, Er, EncK2rle0, length / 2).Run();
                                    break;
                                case 3:
                                    NumericArray ANDOperand = new NumericArray(4 * encVal.Length);
                                    for (int p = 0; p < encVal.Length; ++p)
                                    {
                                        //EncKp[p].SetScaleBits(encVal[p].GetScaleBits());
                                        ANDOperand[4 * p] = EncKp[p];
                                        ANDOperand[4 * p + 1] = Enck1lle0andEncK2rle0[2 * p];
                                        var neb = new Numeric(1, 0) - EncKp[p];
                                        ANDOperand[4 * p + 2] = neb;
                                        ANDOperand[4 * p + 3] = Enck1lle0andEncK2rle0[2 * p + 1];
                                    }
                                    ANDResult = new NumericArray();
                                    new ANDOnEVH(party, line, this, ANDOperand, ANDResult).Run();
                                    break;
                                case 4:
                                    re = new Numeric[encVal.Length];
                                    for (int p = 0; p < encVal.Length; ++p)
                                    {
                                        var EncK3rble0 = ANDResult[2 * p].ModPow(1);
                                        var EncK4rbre0 = ANDResult[2 * p + 1].ModPow(1);
                                        re[p] = EncK3rble0 ^ EncK4rbre0;
                                        re[p].ResetScaleBits();
                                    }
                                    //System.Diagnostics.Debug.WriteLine("EVH: length: " + length);
                                    //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}{8, -15}{9, -2}{10, -15}{11, -2}{12, -15}{13, -3}",
                                    //    "EVH, EncKp: ", EncKp[0].GetUnsignedBigInteger(), ", Enck1lle0: ", Enck1lle0[0].GetUnsignedBigInteger(), ", EncK2rle0: ", EncK2rle0[0].GetUnsignedBigInteger(), ", EncK3rble0: ", ANDResult[0].ModPow(1).GetUnsignedBigInteger(), ", EncK4rbre0: ", ANDResult[1].ModPow(1).GetUnsignedBigInteger(), ", result: ", re[0].GetUnsignedBigInteger(), ", length: ", length));
                                    SetResult(encType, re);
                                    break;
                                case 5:
                                    InvokeCaller();
                                    break;
                                default:
                                    throw new Exception();
                            }
                        }
                    }                   
                    break;                  
            }
        }
    }
    class LessZeroOnKH: OperationOnKH
    {
        public LessZeroOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.LessZero)
        {
            this.length = length;
        }
        public LessZeroOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, OperationType.LessZero)
        { }
        public LessZeroOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, caller, OperationType.LessZero)
        { }
        int length, round2 = 0, parallelism;
        NumericArray kp1, kpp, klandkr, kp, k1andk2, ANDResult, kl;
        Numeric[] re;
        EncryptionType encType;
        protected override void OnKH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric ka = program.GetValue(code.operand1);
                        ka.ResetScaleBits();
                        length = Config.KeyBits;
                        TransformEncType(ka);
                    }
                    else
                    {
                        for(int i = 0; i < key.Length; ++i)
                        {
                            if(key[i].GetScaleBits() != 0)
                            {
                                key[i] = new Numeric(key[i]);
                                key[i].ResetScaleBits();
                            }
                        }
                        Run();
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(length > 0);
                    round2++;
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null) && key[0].GetEncType() == EncryptionType.None)
                    {
                        switch (round2)
                        {
                            case 1:
                                encType = EncryptionType.None;
                                re = new Numeric[1];
                                if (key[0].GetSignedBigInteger() < 0)
                                {
                                    re[0] = new Numeric(1, 0);
                                }
                                else
                                {
                                    re[0] = new Numeric(0, 0);
                                }
                                SetResult(encType, re);
                                break;
                            case 2:
                                InvokeCaller();
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                    else
                    {
                        if (length == 1)
                        {
                            switch (round2)
                            {
                                case 1:
                                    parallelism = key.Length;
                                    kp1 = new NumericArray();
                                    new EqualZeroOnKH(party, line, this, key, kp1, length).Run();
                                    break;
                                case 2:
                                    var ANDOperand1 = new NumericArray(2 * parallelism);
                                    for (int p = 0; p < parallelism; ++p)
                                    {
                                        //kp1[p].SetScaleBits(key[p].GetScaleBits());
                                        ANDOperand1[2 * p] = kp1[p];
                                        ANDOperand1[2 * p + 1] = key[p];
                                    }
                                    kpp = new NumericArray();
                                    new ANDOnKH(party, line, this, ANDOperand1, kpp).Run();
                                    break;
                                case 3:
                                    re = new Numeric[parallelism];
                                    for (int p = 0; p < parallelism; ++p)
                                    {
                                        re[p] = kpp[p].ModPow(1);
                                        re[p].ResetScaleBits();
                                    }
                                    //System.Diagnostics.Debug.WriteLine("KH: length: 1");
                                    //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}",
                                    //    "KH,  kppTemp:", kppTemp[0].GetUnsignedBigInteger(), ", K: ", key[0].GetUnsignedBigInteger(), ", Kpp: ", kpp[0].GetUnsignedBigInteger(), ", length: ", length));
                                    SetResult(encType, re);
                                    break;
                                case 4:
                                    InvokeCaller();
                                    break;
                                default:
                                    throw new Exception();
                            }
                        }
                        else
                        {
                            switch(round2)
                            {
                                case 1:
                                    parallelism = key.Length;
                                    kl = new NumericArray(parallelism);
                                    klandkr = new NumericArray(parallelism * 2);
                                    for (int p = 0; p < parallelism; ++p)
                                    {
                                        kl[p] = key[p] >> (length / 2);
                                        klandkr[2 * p] = kl[p];
                                        klandkr[2 * p + 1] = key[p].ModPow(length / 2);
                                        //kr[p] = key[p].ModPow(length / 2);
                                    }
                                    kp = new NumericArray();
                                    new EqualZeroOnKH(party, line, this, kl, kp, length).Run();
                                    break;
                                case 2:
                                    //System.Diagnostics.Debug.Assert(length - length / 2 == length / 2);
                                    k1andk2 = new NumericArray();
                                    new LessZeroOnKH(party, line, this, klandkr, k1andk2, length / 2).Run();
                                //    k1 = new NumericArray();
                                //    new LessZeroOnKH(party, line, this, kl, k1, length - length / 2).Run();
                                //    break;
                                //case 3:
                                //    k2 = new NumericArray();
                                //    new LessZeroOnKH(party, line, this, kr, k2, length / 2).Run();
                                    break;
                                case 3:                                                               
                                    NumericArray ANDOperand = new NumericArray(4 * parallelism);
                                    for (int p = 0; p < parallelism; ++p)
                                    {
                                        //Kp[p].SetScaleBits(key[p].GetScaleBits());
                                        ANDOperand[4 * p] = kp[p];
                                        ANDOperand[4 * p + 1] = k1andk2[2 * p];
                                        var nk = (new Numeric(0, 0) - kp[p]).ModPow(1);
                                        ANDOperand[4 * p + 2] = nk;
                                        ANDOperand[4 * p + 3] = k1andk2[2 * p + 1];
                                    }
                                    ANDResult = new NumericArray();
                                    new ANDOnKH(party, line, this, ANDOperand, ANDResult).Run();
                                    break;
                                case 4:
                                    re = new Numeric[parallelism];
                                    for (int p = 0; p < parallelism; ++p)
                                    {
                                        var K3 = ANDResult[2 * p].ModPow(1);
                                        var K4 = ANDResult[2 * p + 1].ModPow(1);
                                        re[p] = K3 ^ K4;
                                        re[p].ResetScaleBits();
                                    }
                                    //System.Diagnostics.Debug.WriteLine("KH: length: " + length);
                                    //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}{8, -15}{9, -2}{10, -15}{11, -2}{12, -15}{13, -3}",
                                    //    "KH,  Kp :", kp[0].GetUnsignedBigInteger(), ", K1: ", k1[0].GetUnsignedBigInteger(), ", K2: ", k2[0].GetUnsignedBigInteger(), ", K3: ", ANDResult[0].ModPow(1).GetUnsignedBigInteger(), ", K4: ", ANDResult[1].ModPow(1).GetUnsignedBigInteger(), ", result: ", re[0].GetUnsignedBigInteger(), ", length: ", length));
                                    SetResult(encType, re);
                                    break;
                                case 5:
                                    InvokeCaller();
                                    break;
                                default:
                                    throw new Exception();
                            }                      
                        }
                    }

                    break;
            }
        }
    }

    //class LessZero : Operation
    //{
    //    public LessZero() : base(EncryptionType.AddMod, EncryptionType.XOR) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric 
    //            enckaa = program.GetValue(code.operand1);
    //        var encVal = new Numeric [] { TransformEncType(enckaa, party, code.index) };
    //        var enckf = OnEVHLessZero(party, encVal, code.index);
    //        enckf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, enckf[0]);
    //    }

    //    public override void OnHelper(Party party, int line)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric 
    //            ka = program.GetValue(code.operand1);
    //        var key = new Numeric [] { TransformEncType(ka, party, code.index) };
    //        var kf = OnKHLessZero(party, key, code.index);
    //        kf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, kf[0]);
    //    }

    //    private static Numeric [] _OnEVHLessZero(Party party, Numeric [] encVal, int length, int line)
    //    {
    //        System.Diagnostics.Debug.Assert(length > 0);
            
    //        if (length == 1)
    //        {
    //            var EncKpb1 = EqualZero.OnEVHEqualZero(party, encVal, line, length);
    //            var ANDOperand1 = new Numeric [2 * encVal.Length];
    //            for (int p = 0; p < encVal.Length; ++p)
    //            {
    //                EncKpb1[p].SetScaleBits(encVal[p].GetScaleBits());
    //                ANDOperand1[2 * p] = EncKpb1[p];
    //                ANDOperand1[2 * p + 1] = new Numeric (0, encVal[p].GetScaleBits());
    //            }
    //            var EncKppbk = AND.OnEVHAND(party, ANDOperand1, line);
    //            for (int p = 0; p < encVal.Length; ++p)
    //            {
    //                EncKppbk[p] = EncKppbk[p].ModPow(1);
    //            }
    //            //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}",
    //            //    "EVH, EncKpb :", EncKpb1[0].ToUnsignedBigInteger(), ", EncKa ", encVal[0].ToUnsignedBigInteger(), ", EncKppbk: ", EncKppbk[0].ToUnsignedBigInteger(), ", length: ", l));
    //            return EncKppbk;
    //        }
    //        Numeric []
    //            El = new Numeric [encVal.Length],
    //            Er = new Numeric [encVal.Length],
    //            result = new Numeric [encVal.Length];
    //        for (int p = 0; p < encVal.Length; ++p)
    //        {
    //            El[p] = encVal[p] >> (length / 2);
    //            Er[p] = encVal[p].ModPow(length / 2);
    //        }
    //        var EncKpb = EqualZero.OnEVHEqualZero(party, El, line, length);
    //        var Enck1lle0 = _OnEVHLessZero(party, El, length - length / 2, line);
    //        var EncK2rle0 = _OnEVHLessZero(party, Er, length / 2, line);
    //        Numeric [] ANDOperand = new Numeric [4 * encVal.Length];
    //        for (int p = 0; p < encVal.Length; ++p)
    //        {
    //            EncKpb[p].SetScaleBits(encVal[p].GetScaleBits());
    //            ANDOperand[4 * p] = EncKpb[p];
    //            ANDOperand[4 * p + 1] = Enck1lle0[p];
    //            var neb = new Numeric(1, encVal[p].GetScaleBits()) - EncKpb[p];
    //            ANDOperand[4 * p + 2] = neb;
    //            ANDOperand[4 * p + 3] = EncK2rle0[p];
    //        }
    //        var ANDResult = AND.OnEVHAND(party, ANDOperand, line);
    //        for (int p = 0; p < encVal.Length; ++p)
    //        {
    //            var EncK3rble0 = ANDResult[2 * p].ModPow(1);
    //            var EncK4rbre0 = ANDResult[2 * p + 1].ModPow(1);
    //            result[p] = EncK3rble0 ^ EncK4rbre0;
    //        }
    //        //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}{8, -15}{9, -2}{10, -15}{11, -2}{12, -15}{13, -3}",
    //        //    "EVH, EncKpb: ", EncKpb[0].ToUnsignedBigInteger(), ", Enck1lle0: ", Enck1lle0[0].ToUnsignedBigInteger(), ", EncK2rle0: ", EncK2rle0[0].ToUnsignedBigInteger(), ", EncK3rble0: ", ANDResult[0].ModPow(1).ToUnsignedBigInteger(), ", EncK4rbre0: ", ANDResult[1].ModPow(1).ToUnsignedBigInteger(), ", result: ", result[0].ToUnsignedBigInteger(), ", length: ", l));
    //        return result;
    //    }
    //    //will modify the input parameters encVal, make a copy of the input
    //    //result is XOR encrypted
    //    public static Numeric [] OnEVHLessZero(Party party, Numeric [] encVal, int line)
    //    {
    //        //Numeric[] e = new Numeric[encVal.Length];
    //        //for (int p = 0; p < encVal.Length; ++p) { e[p] = new Numeric(encVal[p]); e[p].ResetScalingFactor(); }
    //        var enckfalz = _OnEVHLessZero(party, encVal, Config.KeyBitLength, line);
    //        for (int p = 0; p < encVal.Length; ++p) { enckfalz[p].ResetScaleBits(); }
    //        return enckfalz;
    //    }

    //    public static Numeric [] _OnKHLessZero(Party party, Numeric [] key, int length, int line)
    //    {
    //        System.Diagnostics.Debug.Assert(length > 0);
    //        int parallelism = key.Length;
    //        if (length == 1)
    //        {
    //            var kp1 = EqualZero.OnKHEqualZero(party, key, line, length);
    //            var ANDOperand1 = new Numeric [2 * parallelism];
    //            for (int p = 0; p < parallelism; ++p)
    //            {
    //                kp1[p].SetScaleBits(key[p].GetScaleBits());
    //                ANDOperand1[2 * p] = kp1[p];
    //                ANDOperand1[2 * p + 1] = key[p];
    //            }
    //            var kpp = AND.OnKHAND(party, ANDOperand1, line);
    //            for (int p = 0; p < parallelism; ++p)
    //            {
    //                kpp[p] = kpp[p].ModPow(1);
    //            }
    //            //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}",
    //            //    "KH,  Kp:", Kp1[0].ToUnsignedBigInteger(), ", K: ", key[0].ToUnsignedBigInteger(), ", Kpp: ", kpp[0].ToUnsignedBigInteger(), ", length: ", l));
    //            return kpp;
    //        }
    //        Numeric []
    //            Kl = new Numeric [parallelism],
    //            Kr = new Numeric [parallelism],
                
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            Kl[p] = key[p] >> (length / 2);
    //            Kr[p] = key[p].ModPow(length / 2);
    //        }
    //        var Kp = EqualZero.OnKHEqualZero(party, Kl, line, length);
    //        var K1 = _OnKHLessZero(party, Kl, length - length / 2, line);
    //        var K2 = _OnKHLessZero(party, Kr, length / 2, line);
    //        Numeric [] ANDOperand = new Numeric [4 * parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            Kp[p].SetScaleBits(key[p].GetScaleBits());
    //            ANDOperand[4 * p] = Kp[p];
    //            ANDOperand[4 * p + 1] = K1[p];
    //            var nk = (new Numeric(0, key[p].GetScaleBits()) - Kp[p]).ModPow(1);
    //            ANDOperand[4 * p + 2] = nk;
    //            ANDOperand[4 * p + 3] = K2[p];
    //        }
    //        var ANDResult = AND.OnKHAND(party, ANDOperand, line);
    //        Numeric[] re = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            var K3 = ANDResult[2 * p].ModPow(1);
    //            var K4 = ANDResult[2 * p + 1].ModPow(1);
    //            re[p] = K3 ^ K4; 
    //        }           
    //        //System.Diagnostics.Debug.WriteLine(String.Format("{0,-15}{1, -2}{2, -15}{3, -2}{4, -15}{5, -2}{6, -15}{7, -2}{8, -15}{9, -2}{10, -15}{11, -2}{12, -15}{13, -3}", 
    //        //    "KH,  Kp :", Kp[0].ToUnsignedBigInteger(), ", K1: ", K1[0].ToUnsignedBigInteger(), ", K2: ", K2[0].ToUnsignedBigInteger(), ", K3: ", ANDResult[0].ModPow(1).ToUnsignedBigInteger(), ", K4: ", ANDResult[1].ModPow(1).ToUnsignedBigInteger(), ", result: ", result[0].ToUnsignedBigInteger(), ", length: ", l));
    //        return re;
    //    }

    //    public static Numeric [] OnKHLessZero(Party party, Numeric [] key, int line)
    //    {
    //        //Numeric[] k = new Numeric[key.Length];
    //        //for (int p = 0; p < key.Length; ++p) { k[p] = new Numeric(key[p]); k[p].ResetScalingFactor(); }
    //        var kf = _OnKHLessZero(party, key, Config.KeyBitLength, line);
    //        for (int p = 0; p < key.Length; ++p) { kf[p].ResetScaleBits(); }
    //        return kf;
    //    }
    //}
}
