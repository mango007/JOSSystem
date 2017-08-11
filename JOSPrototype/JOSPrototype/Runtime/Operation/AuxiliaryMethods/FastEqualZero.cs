using System;
using System.Collections.Generic;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class FastEqualZeroOnEVH : OperationOnEVH
    {
        public FastEqualZeroOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.FastEqualZero)
        {
            this.length = length;
            // generate look-up table for given length
            for (long i = 0; i < (1L << length); i++)
            {
                int count = 0;
                for (int j = 0; j < length; j++)
                {
                    count += ((i >> j) & 1) == 0 ? 0 : 1;
                }
                lut.Add(i, count);
            }
        }
        int length, parallelism;
        private Dictionary<long, int> lut = new Dictionary<long, int>();
        List<Numeric> enc_kp_te = new List<Numeric>();
        NumericArray enc_kpp_tk = new NumericArray(), resultAND = new NumericArray();
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallelism = encVal.Length;
                    List<Numeric> kp = new List<Numeric>();
                    for (int p = 0; p < parallelism; ++p)
                    {
                        // if the secret is 0(all bits are 0), we need to retrun 1(indicating it is true)
                        // so we need to negate all bits first, such that the result of ANDing all bits will be 1
                        long ex = (long)((~encVal[p]).GetUnsignedBigInteger() & Numeric.oneMaskMap[length]);
                        for (long j = 0; j < (1 << length); j++)
                        {
                            long aex = ex & j;
                            long te = lut[j] - lut[aex] == 0 ? 1 : 0;
                            long kpTemp = Utility.NextInt() & 1;
                            kp.Add(new Numeric(kpTemp, 0));
                            enc_kp_te.Add(new Numeric(te ^ kpTemp, 0));
                        }
                    }
                    var toKH = Message.AssembleMessage(line, opType, false, kp.ToArray());
                    party.sender.SendTo(PartyType.KH, toKH);

                    party.receiver.ReceiveFrom(PartyType.KH, line, this, enc_kpp_tk);
                    break;
                case 2:
                    List<Numeric> operanAnd = new List<Numeric>();
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * (1 << length);
                        for (int j = 0; j < (1 << length); j++)
                        {
                            operanAnd.Add(enc_kp_te[offset + j]);
                            operanAnd.Add(enc_kpp_tk[offset + j]);
                        }
                    }
                    new ANDOnEVH(party, line, this, new NumericArray(operanAnd.ToArray()), resultAND).Run();
                    break;
                case 3:
                    var res = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * (1 << length);
                        long resTemp = 0;
                        for (int j = 0; j < (1 << length); j++)
                        {
                            resTemp ^= (long)(resultAND[offset + j].ModPow(1).GetUnsignedBigInteger());
                        }
                        res[p] = new Numeric(resTemp, 0);
                    }
                    result.SetArray(res);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
            
        }
    }

    class FastEqualZeroOnKH : OperationOnKH
    {
        public FastEqualZeroOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.FastEqualZero)
        {
            this.length = length;
            // generate look-up table for given length
            for (long i = 0; i < (1L << length); i++)
            {
                int count = 0;
                for (int j = 0; j < length; j++)
                {
                    count += ((i >> j) & 1) == 0 ? 0 : 1;
                }
                lut.Add(i, count);
            }
        }
        int length, parallelism;
        private Dictionary<long, int> lut = new Dictionary<long, int>();
        List<Numeric> kpp = new List<Numeric>();
        NumericArray kp = new NumericArray(), resultAND = new NumericArray();
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    parallelism = key.Length;
                    List<Numeric> enc_kpp_tk = new List<Numeric>();
                    for (int p = 0; p < parallelism; ++p)
                    {
                        key[p] = new Numeric(key[p].GetUnsignedBigInteger(), 0);
                        for (long j = 0; j < (1L << length); j++)
                        {
                            long notj = (1 << length) - 1 - j;
                            long ak = (long)(key[p].GetUnsignedBigInteger() & Numeric.oneMaskMap[length]) & notj;
                            long tk = lut[notj] - lut[ak] == 0 ? 1 : 0;
                            long kppTemp = Utility.NextInt() & 1;
                            kpp.Add(new Numeric(kppTemp, 0));
                            enc_kpp_tk.Add(new Numeric(tk ^ kppTemp, 0));
                        }
                    }
                    var toEVH = Message.AssembleMessage(line, OperationType.FastEqualZero, false, enc_kpp_tk.ToArray());
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, kp);
                    break;
                case 2:
                    List<Numeric> operanAnd = new List<Numeric>();
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * (1 << length);
                        for (int j = 0; j < (1 << length); j++)
                        {
                            operanAnd.Add(kp[offset + j]);
                            operanAnd.Add(kpp[offset + j]);
                        }
                    }
                    new ANDOnKH(party, line, this, new NumericArray(operanAnd.ToArray()), resultAND).Run();
                    break;
                case 3:
                    var res = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * (1 << length);
                        long resTemp = 0;
                        for (int j = 0; j < (1 << length); j++)
                        {
                            resTemp ^= (long)(resultAND[offset + j].ModPow(1).GetUnsignedBigInteger());
                        }
                        res[p] = new Numeric(resTemp, 0);
                    }
                    result.SetArray(res);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class FastEqualZero : Operation
    //{
    //    public FastEqualZero() : base(EncryptionType.Any, EncryptionType.XOR) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            enckaa = program.GetValue(code.operand1);
    //        System.Diagnostics.Debug.Assert(enckaa.GetEncType() != EncryptionType.Any);
    //        var encVal = new Numeric[] { enckaa };
    //        // do not care about the encType of variable   
    //        var enckf = OnEVHFastEqualZero(party, encVal, code.index);
    //        enckf[0].SetEncType(EncryptionType.XOR);
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
    //        System.Diagnostics.Debug.Assert(ka.GetEncType() != EncryptionType.Any);
    //        var key = new Numeric[] { ka };
    //        var kf = OnKHFastEqualZero(party, key, code.index);
    //        kf[0].SetEncType(EncryptionType.XOR);
    //        program.SetValue(code.result, kf[0]);
    //    }

    //    public static Numeric[] OnEVHFastEqualZero(Party party, Numeric[] encVal, int line)
    //    {
    //        int parallelism = encVal.Length;
    //        List<Numeric> enc_kp_te = new List<Numeric>(), kp = new List<Numeric>();
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            // if the secret is 0(all bits are 0), we need to retrun 1(indicating it is true)
    //            // so we need to negate all bits first, such that the result of ANDing all bits will be 1
    //            encVal[p] = ~encVal[p];
    //            long ex = (long)(encVal[p].GetUnsignedBigInteger() & Numeric.oneMaskMap[length]);
    //            for (long j = 0; j < (1 << length); j++)
    //            {
    //                long aex = ex & j;
    //                long te = lut[j] - lut[aex] == 0 ? 1 : 0;
    //                long kpTemp = Utility.NextInt() & 1;
    //                kp.Add(new Numeric(kpTemp, 0));
    //                enc_kp_te.Add(new Numeric(te ^ kpTemp, 0));
    //            }
    //        }
    //        var toKH = Message.AssembleMessage(line, OperationType.FastEqualZero, false, kp.ToArray());
    //        party.sender.SendTo(PartyType.KH, toKH);

    //        var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        var enc_kpp_tk = Message.DisassembleMessage(fromKH);

    //        List<Numeric> operanAnd = new List<Numeric>();
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * (1 << length);
    //            for (int j = 0; j < (1 << length); j++)
    //            {
    //                operanAnd.Add(enc_kp_te[offset + j]);
    //                operanAnd.Add(enc_kpp_tk[offset + j]);
    //            }
    //        }
    //        var resultAND = AND.OnEVHAND(party, operanAnd.ToArray(), line);
    //        var res = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * (1 << length);
    //            long resTemp = 0;
    //            for (int j = 0; j < (1 << length); j++)
    //            {
    //                resTemp ^= (long)(resultAND[offset + j].ModPow(1).GetUnsignedBigInteger());
    //            }
    //            res[p] = new Numeric(resTemp, 0);
    //        }
    //        return res;
    //    }
    //    public static Numeric[] OnKHFastEqualZero(Party party, Numeric[] key, int line)
    //    {
    //        int parallelism = key.Length;
    //        List<Numeric> enc_kpp_tk = new List<Numeric>(), kpp = new List<Numeric>();
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            key[p] = new Numeric(key[p].GetUnsignedBigInteger(), 0);
    //            for (long j = 0; j < (1L << length); j++)
    //            {
    //                long notj = (1 << length) - 1 - j;
    //                long ak = (long)(key[p].GetUnsignedBigInteger() & Numeric.oneMaskMap[length]) & notj;
    //                long tk = lut[notj] - lut[ak] == 0 ? 1 : 0;
    //                long kppTemp = Utility.NextInt() & 1;
    //                kpp.Add(new Numeric(kppTemp, 0));
    //                enc_kpp_tk.Add(new Numeric(tk ^ kppTemp, 0));
    //            }
    //        }
    //        var toEVH = Message.AssembleMessage(line, OperationType.FastEqualZero, false, enc_kpp_tk.ToArray());
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
    //        var kp = Message.DisassembleMessage(fromEVH);

    //        List<Numeric> operanAnd = new List<Numeric>();
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * (1 << length);
    //            for (int j = 0; j < (1 << length); j++)
    //            {
    //                operanAnd.Add(kp[offset + j]);
    //                operanAnd.Add(kpp[offset + j]);
    //            }
    //        }
    //        var resultAND = AND.OnKHAND(party, operanAnd.ToArray(), line);
    //        var res = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * (1 << length);
    //            long resTemp = 0;
    //            for (int j = 0; j < (1 << length); j++)
    //            {
    //                resTemp ^= (long)(resultAND[offset + j].ModPow(1).GetUnsignedBigInteger());
    //            }
    //            res[p] = new Numeric(resTemp, 0);
    //        }
    //        return res;
    //    }
    //    public static int GetKeySize() { return length; }
    //    private static int length = 11;
    //    private static Dictionary<long, int> lut = new Dictionary<long, int>();
    //    static FastEqualZero()
    //    {
    //        for (long i = 0; i < (1L << length); i++)
    //        {
    //            int count = 0;
    //            for (int j = 0; j < length; j++)
    //            {
    //                count += ((i >> j) & 1) == 0 ? 0 : 1;
    //            }
    //            lut.Add(i, count);
    //        }
    //    }
    //}
}
