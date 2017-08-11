using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using System.Numerics;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    static class Inverse
    {
        // 1/x, x must be a possitive integer
        // Config.NumericBits + Config.ScaleBits < Config.EffectiveKeyBits
        public static void SetParameters()
        {
            int len = Config.NumericBits;
            // when computing (x - at)^i, we need to make sure the reust does not overflow, 
            // which means operands need to be scaled down.
            // i.e.  if Config.EffectiveKeyBits is 61 and nt is 7, maxMulLen is 8 and term (x - at)^7 will not be longer than 56 bits
            maxMulLen = Config.EffectiveKeyBits / nt;
            reductionBits = len - maxMulLen;
            reductionFactor = BigInteger.One << reductionBits;

            //magnificationBits = Config.ScaleBits;
            scaleFactor = BigInteger.One << Config.ScaleBits;

            at = new Numeric(BigInteger.Pow(2, len - 1) + BigInteger.Pow(2, len - 2), 0);
            dReciprocal = new BigInteger[nt + 1];
            dReciprocal[0] = at.GetUnsignedBigInteger();
            for (int i = 1; i <= nt; ++i)
            {
                dReciprocal[i] = (-1) * dReciprocal[i - 1] * at.GetUnsignedBigInteger();
            }
        }
        public static int nt = 7;
        public static Numeric at;
        public static BigInteger[] dReciprocal;
        public static int reductionBits;
        public static BigInteger reductionFactor;
        //public static byte magnificationBits;
        public static BigInteger scaleFactor;
        public static int maxMulLen;
        public static NumericArray ReverseBits(NumericArray val, int l)
        {
            NumericArray ret = new NumericArray(val.Length);
            for (int p = 0; p < val.Length; p++)
            {
                BigInteger dVal = 0, sVal = val[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                for (int i = 0; i < l; ++i)
                {
                    dVal += ((sVal & mask) == 0 ? BigInteger.Zero : BigInteger.One) << (l - 1 - i);
                    mask <<= 1;
                }
                ret[p] = new Numeric(dVal, 0);
            }
            return ret;
        }
    }
    
    class InverseOnEVH : OperationOnEVH
    {
        public InverseOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Inverse)
        { }
        public InverseOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Inverse)
        { }
        public InverseOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Inverse)
        { }
        int parallism, length;
        Numeric[][] et;
        NumericArray
            ePow2msb = new NumericArray(),
            encKpRevPow = new NumericArray(),
            encKppap = new NumericArray(),
            et1,
            et2 = new NumericArray(),
            et3et4 = new NumericArray(),
            et5et6et7 = new NumericArray(),
            et2ToEt7,
            eInva;
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        TransformEncType(program.GetValue(code.operand1));
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    if (!ReferenceEquals(code, null))
                    {
                        if (encVal[0].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            var re = new Numeric((BigInteger.One << (Config.ScaleBits + encVal[0].GetScaleBits())) / encVal[0].GetUnsignedBigInteger(), Config.ScaleBits);
                            eInva = new NumericArray(re);
                            // jump to round 13
                            step = 12;
                            Run();
                            break;                
                        }
                    }
                    parallism = encVal.Length;
                    length = Config.NumericBits;
                    new IndexMSBOnEVH(party, line, this, encVal, ePow2msb, length).Run();
                    break;
                case 3:
                    var eRevPow = Inverse.ReverseBits(ePow2msb, length);
                    // encKpRevPow is in {1, 2, 4, ..., 2^(Config.NumericBitLength - 1)}
                    new XORToAddModOnEVH(party, line, this, eRevPow, encKpRevPow).Run();
                    break;
                case 4:
                    var MulOperands = new NumericArray(2 * parallism);
                    for (int p = 0; p < parallism; ++p) { MulOperands[2 * p] = encVal[p]; MulOperands[2 * p + 1] = encKpRevPow[p]; }
                    // a' is in [ 2 ^ (NumericBitLength - 1), 2 ^ (NumericBitLength) )
                    new MultiplicationOnEVH(party, line, this, MulOperands, encKppap).Run();
                    break;
                case 5:
                    et = new Numeric[parallism][];
                    et1 = new NumericArray(parallism);
                    for (int p = 0; p < parallism; ++p)
                    {
                        System.Diagnostics.Debug.Assert(encKppap[p].GetScaleBits() == 0);
                        //EncKppap[p].ResetScaleBits();
                        et[p] = new Numeric[Inverse.nt + 1];
                        et[p][0] = new Numeric(1, 0);
                        // a' - at > 0
                        et1[p] = encKppap[p] - Inverse.at;
                    }
                    // AddModToAdd makes sure (a' + k" - at) > 0 and k" > 0
                    new AddModToAddOnEVH(party, line, this, et1, et1).Run();
                    break;
                // case 6~8 computes the (a' - at) ^ 2, (a' - at) ^ 3, ... , (a' - at) ^ 7
                case 6:
                    var mulOperand2 = new NumericArray(2 * parallism);
                    for (int p = 0; p < parallism; ++p)
                    {
                        et[p][1] = et1[p] >> Inverse.reductionBits;
                        mulOperand2[2 * p] = et[p][1];
                        mulOperand2[2 * p + 1] = et[p][1];
                    }
                    new MultiplicationOnEVH(party, line, this, mulOperand2, et2).Run();
                    break;
                case 7:
                    var mulOperand34 = new NumericArray(parallism * 4);
                    for (int p = 0; p < parallism; ++p)
                    {
                        et[p][2] = et2[p];
                        mulOperand34[4 * p] = et[p][1];
                        mulOperand34[4 * p + 1] = et[p][2];
                        mulOperand34[4 * p + 2] = et[p][2];
                        mulOperand34[4 * p + 3] = et[p][2];
                    }
                    new MultiplicationOnEVH(party, line, this, mulOperand34, et3et4).Run();
                    break;
                case 8:
                    var mulOperand567 = new NumericArray(parallism * 6);
                    for (int p = 0; p < parallism; ++p)
                    {
                        et[p][3] = et3et4[2 * p];
                        et[p][4] = et3et4[2 * p + 1];
                        mulOperand567[6 * p] = et[p][1];
                        mulOperand567[6 * p + 1] = et[p][4];
                        mulOperand567[6 * p + 2] = et[p][2];
                        mulOperand567[6 * p + 3] = et[p][4];
                        mulOperand567[6 * p + 4] = et[p][3];
                        mulOperand567[6 * p + 5] = et[p][4];
                    }
                    new MultiplicationOnEVH(party, line, this, mulOperand567, et5et6et7).Run();
                    break;
                case 9:
                    for (int p = 0; p < parallism; ++p)
                    {
                        et[p][5] = et5et6et7[3 * p];
                        et[p][6] = et5et6et7[3 * p + 1];
                        et[p][7] = et5et6et7[3 * p + 2];
                    }

                    et2ToEt7 = new NumericArray(parallism * 6);
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * 6;
                        for (int i = 0; i < 6; ++i)
                        {
                            et2ToEt7[offset + i] = et[p][i + 2];
                        }
                    }
                    // convert encrption for (a' - at) ^ 2, (a' - at) ^ 3, ... , (a' - at) ^ 7 from AddMod to Addition,
                    // such that when computing taylor sum, module will affect the result
                    new AddModToAddOnEVH(party, line, this, et2ToEt7, et2ToEt7).Run();
                    break;
                // compute taylor sum
                case 10:
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * 6;
                        for (int i = 0; i < 6; ++i)
                        {
                            et[p][i + 2] = et2ToEt7[offset + i];
                        }
                    }
                    eInva = new NumericArray(parallism);
                    var MulOperandsScale = new NumericArray(2 * parallism);
                    // since 1/a' is too small(even after mutilpied by scaling factor), it will always be 0 if converted to fixed pointer integer
                    // such that we need to magnify it with a factor. Here we choose 2 ^ (Config.NumericBitLength - 1)
                    BigInteger magnificationFactor = BigInteger.One << (Config.NumericBits - 1);
                    for (int p = 0; p < parallism; ++p)
                    {
                        BigInteger
                            taylorSum = Inverse.scaleFactor * magnificationFactor / Inverse.dReciprocal[0],
                            redFactor = Inverse.reductionFactor;
                        for (int i = 1; i <= Inverse.nt; ++i)
                        {
                            taylorSum += redFactor * Inverse.scaleFactor * magnificationFactor / Inverse.dReciprocal[i];
                            redFactor *= Inverse.reductionFactor;
                            // taylorSum += Inverse.d[i] * et[p][i].GetVal();
                        }
                        
                        eInva[p] = new Numeric(taylorSum, 0);
                        //System.Diagnostics.Debug.WriteLine("e: taylorSum * magnificationFactor: " + taylorSum);
                        //System.Diagnostics.Debug.WriteLine("e: val: " + taylorSum * Math.Pow(2, Config.NumericBitLength - 1));
                        //System.Diagnostics.Debug.WriteLine("e: eInva befor: " + eInva[0]);
                        MulOperandsScale[2 * p] = eInva[p];
                        MulOperandsScale[2 * p + 1] = encKpRevPow[p];
                    }
                    // 1 / a' * encKpRevPow is the result
                    new MultiplicationOnEVH(party, line, this, MulOperandsScale, eInva).Run();
                    break;
                case 11:
                    new AddModToAddOnEVH(party, line, this, eInva, eInva).Run();
                    break;
                case 12:
                    for (int p = 0; p < parallism; ++p)
                    {
                        // scale down the result by the factor 2 ^ (Config.NumericBitLength - 1)
                        eInva[p] >>= (Config.NumericBits - 1);
                        eInva[p].SetScaleBits(Config.ScaleBits);
                    }
                    //System.Diagnostics.Debug.WriteLine("e: eInva: " + eInva[0]);
                    Run();
                    break;
                case 13:
                    SetResult(encType, eInva.GetArray());
                    break;
                case 14:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class InverseOnKH : OperationOnKH
    {
        public InverseOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.Inverse)
        { }
        public InverseOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Inverse)
        { }
        public InverseOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Inverse)
        { }
        int parallelism, length;
        Numeric[][] kt;
        NumericArray
            kPow2msb = new NumericArray(),
            kp = new NumericArray(),
            kpp = new NumericArray(),
            kt1,
            kt2 = new NumericArray(),
            kt3kt4 = new NumericArray(),
            kt5kt6kt7 = new NumericArray(),
            kt2ToKt7,
            kInva;
        EncryptionType encType;
        protected override void OnKH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
                    {
                        TransformEncType(program.GetValue(code.operand1));
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        if (key[0].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            var re = new Numeric((BigInteger.One << (Config.ScaleBits + key[0].GetScaleBits())) / key[0].GetUnsignedBigInteger(), Config.ScaleBits);
                            kInva = new NumericArray(re);
                            // jump to round 13
                            step = 12;
                            Run();
                            break;
                        }
                    }
                    parallelism = key.Length;
                    length = Config.NumericBits;
                    new IndexMSBOnKH(party, line, this, key, kPow2msb, length).Run();
                    break;
                case 3:
                    var kRevPow = Inverse.ReverseBits(kPow2msb, length);
                    // encKpRevPow is in {1, 2, 4, ..., 2^(Config.NumericBitLength - 1)}
                    new XORToAddModOnKH(party, line, this, kRevPow, kp).Run();
                    break;
                case 4:
                    var MulOperands = new NumericArray(2 * parallelism);
                    for (int p = 0; p < parallelism; ++p) { MulOperands[2 * p] = key[p]; MulOperands[2 * p + 1] = kp[p]; }
                    // a' is in [ 2 ^ (NumericBitLength - 1), 2 ^ (NumericBitLength) )
                    new MultiplicationOnKH(party, line, this, MulOperands, kpp).Run();
                    break;
                case 5:
                    kt = new Numeric[parallelism][];
                    kt1 = new NumericArray(parallelism);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        System.Diagnostics.Debug.Assert(kpp[p].GetScaleBits() == 0);
                        //EncKppap[p].ResetScaleBits();
                        kt[p] = new Numeric[Inverse.nt + 1];
                        kt[p][0] = new Numeric(0, 0);
                        // a' - at > 0
                        kt1[p] = kpp[p];
                    }
                    // AddModToAdd makes sure (a' + k" - at) > 0 and k" > 0
                    new AddModToAddOnKH(party, line, this, kt1, kt1).Run();
                    break;
                // case 6~8 computes the (a' - at) ^ 2, (a' - at) ^ 3, ... , (a' - at) ^ 7
                case 6:
                    var mulOperand2 = new NumericArray(2 * parallelism);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        kt[p][1] = kt1[p] >> Inverse.reductionBits;
                        mulOperand2[2 * p] = kt[p][1];
                        mulOperand2[2 * p + 1] = kt[p][1];
                    }
                    new MultiplicationOnKH(party, line, this, mulOperand2, kt2).Run();
                    break;
                case 7:
                    var mulOperand34 = new NumericArray(parallelism * 4);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        kt[p][2] = kt2[p];
                        mulOperand34[4 * p] = kt[p][1];
                        mulOperand34[4 * p + 1] = kt[p][2];
                        mulOperand34[4 * p + 2] = kt[p][2];
                        mulOperand34[4 * p + 3] = kt[p][2];
                    }
                    new MultiplicationOnKH(party, line, this, mulOperand34, kt3kt4).Run();
                    break;
                case 8:
                    var mulOperand567 = new NumericArray(parallelism * 6);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        kt[p][3] = kt3kt4[2 * p];
                        kt[p][4] = kt3kt4[2 * p + 1];
                        mulOperand567[6 * p] = kt[p][1];
                        mulOperand567[6 * p + 1] = kt[p][4];
                        mulOperand567[6 * p + 2] = kt[p][2];
                        mulOperand567[6 * p + 3] = kt[p][4];
                        mulOperand567[6 * p + 4] = kt[p][3];
                        mulOperand567[6 * p + 5] = kt[p][4];
                    }
                    new MultiplicationOnKH(party, line, this, mulOperand567, kt5kt6kt7).Run();
                    break;
                case 9:
                    for (int p = 0; p < parallelism; ++p)
                    {
                        kt[p][5] = kt5kt6kt7[3 * p];
                        kt[p][6] = kt5kt6kt7[3 * p + 1];
                        kt[p][7] = kt5kt6kt7[3 * p + 2];
                    }

                    kt2ToKt7 = new NumericArray(parallelism * 6);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * 6;
                        for (int i = 0; i < 6; ++i)
                        {
                            kt2ToKt7[offset + i] = kt[p][i + 2];
                        }
                    }
                    // convert encrption for (a' - at) ^ 2, (a' - at) ^ 3, ... , (a' - at) ^ 7 from AddMod to Addition,
                    // such that when computing taylor sum, module will not affect the result
                    new AddModToAddOnKH(party, line, this, kt2ToKt7, kt2ToKt7).Run();
                    break;
                // compute taylor sum
                case 10:
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * 6;
                        for (int i = 0; i < 6; ++i)
                        {
                            kt[p][i + 2] = kt2ToKt7[offset + i];
                        }
                    }
                    kInva = new NumericArray(parallelism);
                    var MulOperandsScale = new NumericArray(2 * parallelism);
                    // since 1/a' is too small(even after mutilpied by scaling factor), it will always be 0 if converted to fixed pointer integer
                    // such that we need to magnify it with a factor. Here we choose 2 ^ (Config.NumericBitLength - 1)
                    BigInteger magnificationFactor = BigInteger.One << (Config.NumericBits - 1);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        BigInteger
                            taylorSum = 0,
                            redFactor = Inverse.reductionFactor;
                        for (int i = 1; i <= Inverse.nt; ++i)
                        {
                            taylorSum += redFactor * Inverse.scaleFactor * magnificationFactor / Inverse.dReciprocal[i];
                            redFactor *= Inverse.reductionFactor;
                            //taylorSum += Inverse.d[i] * kt[p][i].GetVal();
                        }
                        // since 1/a' is too small(less than 1), it will always be 0 if converted to fixed pointer integer
                        // such that we need to magnify it with a factor. Here we choose 2 ^ (Config.NumericBitLength - 1)
                        kInva[p] = new Numeric(taylorSum, 0);
                        //System.Diagnostics.Debug.WriteLine("k: taylorSum * magnificationFactor: " + taylorSum);
                        //System.Diagnostics.Debug.WriteLine("k: val: " + taylorSum * Math.Pow(2, Config.NumericBitLength - 1));
                        //System.Diagnostics.Debug.WriteLine("k: kInva before: " + kInva[0]);
                        MulOperandsScale[2 * p] = kInva[p];
                        MulOperandsScale[2 * p + 1] = kp[p];
                    }
                    // 1 / a' * encKpRevPow is the result
                    new MultiplicationOnKH(party, line, this, MulOperandsScale, kInva).Run();
                    break;
                case 11:
                    new AddModToAddOnKH(party, line, this, kInva, kInva).Run();
                    break;
                case 12:
                    for (int p = 0; p < parallelism; ++p)
                    {
                        // scale down the result by the factor 2 ^ (Config.NumericBitLength - 1)
                        kInva[p] >>= (Config.NumericBits - 1);
                        kInva[p].SetScaleBits(Config.ScaleBits);
                    }
                    //System.Diagnostics.Debug.WriteLine("k: kInva: " + kInva[0]);
                    Run();
                    break;
                case 13:
                    SetResult(encType, kInva.GetArray());
                    break;
                case 14:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class Inverse : Operation
    //{

    //    public Inverse() : base(EncryptionType.AddMod, EncryptionType.AddMod) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            enckaa = program.GetValue(code.operand1);
    //        enckaa.ResetScaleBits();
    //        var encVal = new Numeric[] { TransformEncType(enckaa, party, code.index) };
    //        var enckf = OnEVHInverse(party, encVal, code.index);
    //        enckf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, enckf[0]);
    //    }


    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            ka = program.GetValue(code.operand1);
    //        ka.ResetScaleBits();
    //        var key = new Numeric[] { TransformEncType(ka, party, code.index) };
    //        var kf = OnKHInverse(party, key, code.index);
    //        kf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, kf[0]);
    //    }

    //    public static Numeric[] OnEVHInverse(Party party, Numeric[] encVal, int line)
    //    {
    //        System.Diagnostics.Debug.WriteLine("e: encVal: " + encVal[0]);
    //        int parallism = encVal.Length;
    //        int l = Config.NumericBitLength;
    //        var ePow2msb = IndexMSB.OnEVHIndexMSB(party, encVal, line, l);
    //        System.Diagnostics.Debug.WriteLine("e: ePow2msb: " + ePow2msb[0]);
    //        var eRevPow = ReverseBits(ePow2msb, l);
    //        System.Diagnostics.Debug.WriteLine("e: eRevPow: " + eRevPow[0]);
    //        var EncKpRevPow = XORToAddMod.OnEVHXORToAddMod(party, eRevPow, line);
    //        System.Diagnostics.Debug.WriteLine("e: EncKpRevPow: " + EncKpRevPow[0]);
    //        var MulOperands = new Numeric[2 * parallism];
    //        for (int p = 0; p < parallism; ++p) { MulOperands[2 * p] = encVal[p]; MulOperands[2 * p + 1] = EncKpRevPow[p]; }
    //        var EncKppap = Multiplication.OnEVH(party, MulOperands, line);
    //        System.Diagnostics.Debug.WriteLine("e: EncKppap: " + EncKppap[0]);
    //        var et = OnEVHPow(party, EncKppap, line);
    //        System.Diagnostics.Debug.WriteLine("e: et[0]: " + et[0][0]);
    //        System.Diagnostics.Debug.WriteLine("e: et[1]: " + et[0][1]);
    //        System.Diagnostics.Debug.WriteLine("e: et[2]: " + et[0][2]);
    //        System.Diagnostics.Debug.WriteLine("e: et[3]: " + et[0][3]);
    //        System.Diagnostics.Debug.WriteLine("e: et[4]: " + et[0][4]);
    //        System.Diagnostics.Debug.WriteLine("e: et[5]: " + et[0][5]);
    //        System.Diagnostics.Debug.WriteLine("e: et[6]: " + et[0][6]);
    //        System.Diagnostics.Debug.WriteLine("e: et[7]: " + et[0][7]);
    //        var eInva = new Numeric[parallism];
    //        MulOperands = new Numeric[2 * parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            double taylorSum = d[0];
    //            for (int i = 1; i <= nt; ++i)
    //            {
    //                taylorSum += d[i] * et[p][i].GetVal();
    //            }
    //            eInva[p] = new Numeric((BigInteger)(taylorSum * Math.Pow(2, Config.NumericBitLength - Config.FractionBitLength)), 0);
    //            System.Diagnostics.Debug.WriteLine("e: taylorSum * magnificationFactor: " + taylorSum);
    //            System.Diagnostics.Debug.WriteLine("e: val: " + taylorSum * Math.Pow(2, Config.NumericBitLength - Config.FractionBitLength));
    //            System.Diagnostics.Debug.WriteLine("e: eInva befor: " + eInva[0]);
    //            MulOperands[2 * p] = eInva[p];
    //            MulOperands[2 * p + 1] = EncKpRevPow[p];
    //        }
    //        eInva = Multiplication.OnEVH(party, MulOperands, line);
    //        eInva = AddModToAdd.OnEVHAddModToAdd(party, eInva, line);
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            eInva[p].SetScaleBits(magnificationBits);
    //            eInva[p] >>= (Config.NumericBitLength - Config.FractionBitLength);
    //        }
    //        System.Diagnostics.Debug.WriteLine("e: eInva: " + eInva[0]);
    //        return eInva;
    //    }
    //    private static Numeric[][] OnEVHPow(Party party, Numeric[] EncKppap, int line)
    //    {
    //        int parallism = EncKppap.Length;
    //        EncKppap = AddModToAdd.OnEVHAddModToAdd(party, EncKppap, line);
    //        var et = new Numeric[parallism][];
    //        var mulOperand = new Numeric[parallism * 2];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            EncKppap[p].ResetScaleBits();
    //            et[p] = new Numeric[nt + 1];
    //            et[p][0] = new Numeric(1, 0);
    //            // ap - at > 0; EncKppap[p] - at > 0
    //            et[p][1] = (EncKppap[p] - at) >> reductionBits;
    //            mulOperand[2 * p] = et[p][1];
    //            mulOperand[2 * p + 1] = et[p][1];
    //        }
    //        var et2 = Multiplication.OnEVH(party, mulOperand, line);
    //        mulOperand = new Numeric[parallism * 4];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            et[p][2] = et2[p];
    //            mulOperand[4 * p] = et[p][1];
    //            mulOperand[4 * p + 1] = et[p][2];
    //            mulOperand[4 * p + 2] = et[p][2];
    //            mulOperand[4 * p + 3] = et[p][2];
    //        }
    //        var et3et4 = Multiplication.OnEVH(party, mulOperand, line);
    //        mulOperand = new Numeric[parallism * 6];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            et[p][3] = et3et4[2 * p];
    //            et[p][4] = et3et4[2 * p + 1];
    //            mulOperand[6 * p] = et[p][1];
    //            mulOperand[6 * p + 1] = et[p][4];
    //            mulOperand[6 * p + 2] = et[p][2];
    //            mulOperand[6 * p + 3] = et[p][4];
    //            mulOperand[6 * p + 4] = et[p][3];
    //            mulOperand[6 * p + 5] = et[p][4];
    //        }
    //        var et5et6et7 = Multiplication.OnEVH(party, mulOperand, line);
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            et[p][5] = et5et6et7[3 * p];
    //            et[p][6] = et5et6et7[3 * p + 1];
    //            et[p][7] = et5et6et7[3 * p + 2];
    //        }

    //        var et1_to_et7 = new Numeric[parallism * 7];
    //        var len = new int[parallism * 7];
    //        var signd = new bool[parallism * 7];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * 7;
    //            for (int i = 0; i < 7; ++i)
    //            {
    //                et1_to_et7[offset + i] = et[p][i + 1];
    //                len[offset + i] = maxMulLen * (i + 1);
    //                signd[offset + i] = false;
    //            }
    //        }
    //        et1_to_et7 = AddModToAdd.OnEVHAddModToAdd(party, et1_to_et7, line);
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * 7;
    //            for (int i = 0; i < 7; ++i)
    //            {
    //                et[p][i + 1] = et1_to_et7[offset + i];
    //            }
    //        }
    //        return et;
    //    }
    //    public static Numeric[] OnKHInverse(Party party, Numeric[] key, int line)
    //    {
    //        System.Diagnostics.Debug.WriteLine("k: key: " + key[0]);
    //        int parallism = key.Length;
    //        int l = Config.NumericBitLength;
    //        var kPow2msb = IndexMSB.OnKHIndexMSB(party, key, line, l);
    //        System.Diagnostics.Debug.WriteLine("k: kPow2msb: " + kPow2msb[0]);
    //        var kRevPow = ReverseBits(kPow2msb, l);
    //        System.Diagnostics.Debug.WriteLine("k: kRevPow: " + kRevPow[0]);
    //        var kp = XORToAddMod.OnKHXORToAddMod(party, kRevPow, line);
    //        System.Diagnostics.Debug.WriteLine("k:      kp: " + kp[0]);
    //        var MulOperands = new Numeric[2 * parallism];
    //        for (int p = 0; p < parallism; ++p) { MulOperands[2 * p] = key[p]; MulOperands[2 * p + 1] = kp[p]; }
    //        var kpp = Multiplication.OnKHMultiplication(party, MulOperands, line);
    //        System.Diagnostics.Debug.WriteLine("k:      kpp: " + kpp[0]);
    //        var kt = OnKHPow(party, kpp, line);
    //        System.Diagnostics.Debug.WriteLine("k: kt[0]: " + kt[0][0]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[1]: " + kt[0][1]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[2]: " + kt[0][2]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[3]: " + kt[0][3]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[4]: " + kt[0][4]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[5]: " + kt[0][5]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[6]: " + kt[0][6]);
    //        System.Diagnostics.Debug.WriteLine("k: kt[7]: " + kt[0][7]);
    //        var kInva = new Numeric[parallism];
    //        MulOperands = new Numeric[2 * parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            double taylorSum = 0;
    //            for (int i = 1; i <= nt; ++i)
    //            {
    //                taylorSum += d[i] * kt[p][i].GetVal();
    //            }
    //            kInva[p] = new Numeric((BigInteger)(taylorSum * Math.Pow(2, Config.NumericBitLength - Config.FractionBitLength)), 0);
    //            System.Diagnostics.Debug.WriteLine("k: taylorSum * magnificationFactor: " + taylorSum);
    //            System.Diagnostics.Debug.WriteLine("k: val: " + taylorSum * Math.Pow(2, Config.NumericBitLength - Config.FractionBitLength));
    //            System.Diagnostics.Debug.WriteLine("k: kInva before: " + kInva[0]);
    //            MulOperands[2 * p] = kInva[p];
    //            MulOperands[2 * p + 1] = kp[p];
    //        }
    //        kInva = Multiplication.OnKHMultiplication(party, MulOperands, line);
    //        kInva = AddModToAdd.OnKHAddModToAdd(party, kInva, line, Config.NumericBitLength, false);
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            kInva[p].SetScaleBits(magnificationBits);
    //            kInva[p] >>= (Config.NumericBitLength - Config.FractionBitLength);
    //        }
    //        System.Diagnostics.Debug.WriteLine("k: kInva: " + kInva[0]);
    //        return kInva;
    //    }
    //    private static Numeric[][] OnKHPow(Party party, Numeric[] kpp, int line)
    //    {
    //        int parallism = kpp.Length;
    //        kpp = AddModToAdd.OnKHAddModToAdd(party, kpp, line, Config.NumericBitLength, false);
    //        var kt = new Numeric[parallism][];
    //        var mulOperand = new Numeric[parallism * 2];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            kt[p] = new Numeric[nt + 1];
    //            kt[p][0] = new Numeric(0, 0);
    //            kt[p][1] = kpp[p] >> reductionBits;
    //            mulOperand[2 * p] = kt[p][1];
    //            mulOperand[2 * p + 1] = kt[p][1];
    //        }
    //        var kt2 = Multiplication.OnKHMultiplication(party, mulOperand, line);
    //        mulOperand = new Numeric[parallism * 4];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            kt[p][2] = kt2[p];
    //            mulOperand[4 * p] = kt[p][1];
    //            mulOperand[4 * p + 1] = kt[p][2];
    //            mulOperand[4 * p + 2] = kt[p][2];
    //            mulOperand[4 * p + 3] = kt[p][2];
    //        }
    //        var kt3kt4 = Multiplication.OnKHMultiplication(party, mulOperand, line);
    //        mulOperand = new Numeric[parallism * 6];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            kt[p][3] = kt3kt4[2 * p];
    //            kt[p][4] = kt3kt4[2 * p + 1];
    //            mulOperand[6 * p] = kt[p][1];
    //            mulOperand[6 * p + 1] = kt[p][4];
    //            mulOperand[6 * p + 2] = kt[p][2];
    //            mulOperand[6 * p + 3] = kt[p][4];
    //            mulOperand[6 * p + 4] = kt[p][3];
    //            mulOperand[6 * p + 5] = kt[p][4];
    //        }
    //        var kt5kt6kt7 = Multiplication.OnKHMultiplication(party, mulOperand, line);
    //        for (int p = 0; p < kpp.Length; ++p)
    //        {
    //            kt[p][5] = kt5kt6kt7[3 * p];
    //            kt[p][6] = kt5kt6kt7[3 * p + 1];
    //            kt[p][7] = kt5kt6kt7[3 * p + 2];
    //        }

    //        var kt1_to_kt7 = new Numeric[parallism * 7];
    //        var len = new int[parallism * 7];
    //        var signd = new bool[parallism * 7];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * 7;
    //            for (int i = 0; i < 7; ++i)
    //            {
    //                kt1_to_kt7[offset + i] = kt[p][i + 1];
    //                len[offset + i] = maxMulLen * (i + 1);
    //                signd[offset + i] = false;
    //            }
    //        }
    //        kt1_to_kt7 = AddModToAdd.OnKHAddModToAdd(party, kt1_to_kt7, line, len, signd);
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * 7;
    //            for (int i = 0; i < 7; ++i)
    //            {
    //                kt[p][i + 1] = kt1_to_kt7[offset + i];
    //            }
    //        }
    //        return kt;
    //    }
    //    //static Inverse()
    //    //{
    //    //    int l = Config.NumericBitLength;
    //    //    maxMulLen = (Config.KeyBitLength - 2) / nt;
    //    //    reductionBits = l - maxMulLen;
    //    //    reductionFactor = 1 << reductionBits;

    //    //    magnificationBits = Config.FractionBitLength;
    //    //    magnificationFactor = 1 << magnificationBits;

    //    //    at = new Numeric(BigInteger.Pow(2, l - 1) + BigInteger.Pow(2, l - 2), 0);
    //    //    d = new double[nt + 1];
    //    //    d[0] = 1 / (double)at.GetUnsignedBigInteger() * magnificationFactor;
    //    //    for (int i = 1; i <= nt; ++i)
    //    //    {
    //    //        d[i] = (-1) * d[i - 1] / ((double)at.GetUnsignedBigInteger()) * reductionFactor * magnificationFactor;
    //    //    }
    //    //}
    //    //private static int nt = 7;
    //    //private static Numeric at;
    //    //private static double[] d;
    //    //private static int reductionBits;
    //    //private static double reductionFactor;
    //    //private static int magnificationBits;
    //    //private static double magnificationFactor;
    //    //private static int maxMulLen;
    //    //private static Numeric[] ReverseBits(Numeric[] val, int l)
    //    //{
    //    //    Numeric[] ret = new Numeric[val.Length];
    //    //    for (int p = 0; p < val.Length; p++)
    //    //    {
    //    //        BigInteger dVal = 0, sVal = val[p].GetUnsignedBigInteger(), mask = BigInteger.One;
    //    //        for (int i = 0; i < l; ++i)
    //    //        {
    //    //            dVal += ((sVal & mask) == 0 ? BigInteger.Zero : BigInteger.One) << (l - 1 - i);
    //    //            mask <<= 1;
    //    //        }
    //    //        ret[p] = new Numeric(dVal, 0);
    //    //    }
    //    //    return ret;
    //    //}
    //}
}
