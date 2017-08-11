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
    class HammingDistanceOnEVH : OperationOnEVH
    {
        public HammingDistanceOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.HammingDistance)
        {
            this.length = length;
        }
        int length;
        int parallelism;
        int modpow;
        Numeric[] encki0ei;
        NumericArray encki1ki = new NumericArray(), Eeki = new NumericArray(), Aekei = new NumericArray();
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallelism = encVal.Length;
                    modpow = (int)Math.Ceiling(Math.Log(length + 1, 2));
                    encki0ei = new Numeric[parallelism * length];
                    // send K0 to KH
                    Numeric[] ki0 = new Numeric[parallelism * length];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        BigInteger encValBi = encVal[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            ki0[offset + i] = Utility.NextUnsignedNumeric(0);
                            encki0ei[offset + i] = new Numeric((encValBi & mask) == 0 ? 0 : 1, 0) ^ ki0[offset + i];
                            mask <<= 1;
                        }
                    }
                    var toKH = Message.AssembleMessage(line, opType, false, ki0);
                    party.sender.SendTo(PartyType.KH, toKH);
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, encki1ki);
                    break;
                case 2:
                    // AND
                    Numeric[] encValForAnd = new Numeric[parallelism * length * 2];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            encValForAnd[(offset + i) * 2] = encki0ei[offset + i];
                            encValForAnd[(offset + i) * 2 + 1] = encki1ki[offset + i];
                        }
                    }
                    new ANDOnEVH(party, line, this, new NumericArray(encValForAnd), Eeki).Run();
                    break;
                case 3:
                    new XORToAddModOnEVH(party, line, this, Eeki, Aekei).Run();
                    break;
                case 4:
                    Numeric[] re = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        Numeric enckpek = new Numeric(0, 0), enckpp_ek, es;
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            enckpek += Aekei[offset + i];
                        }
                        //System.Diagnostics.Debug.WriteLine("enckpek: " + enckpek);
                        enckpek = new Numeric(2, 0) * enckpek;
                        enckpp_ek = new Numeric(0, 0) - enckpek;
                        es = encVal[p].SumBits(length);
                        //System.Diagnostics.Debug.WriteLine("es: " + es);
                        re[p] = (es + enckpp_ek).ModPow(modpow);
                        // re[p].SetEncType(EncryptionType.AddMod);
                    }
                    result.SetArray(re);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class HammingDistanceOnKH: OperationOnKH
    {
        public HammingDistanceOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int le)
            : base(party, line, caller, operands, result, OperationType.HammingDistance)
        {
            this.le = le;
        }
        int le;
        int parallelism;
        int modpow;
        Numeric[] ki1;
        NumericArray ki0 = new NumericArray(), ki2 = new NumericArray(), Aekki = new NumericArray();
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    parallelism = key.Length;
                    //int[] scalingFactor = new int[parallelism];
                    modpow = (int)Math.Ceiling(Math.Log(le + 1, 2));
                    ki1 = new Numeric[parallelism * le];
                    Numeric[] encki1ki = new Numeric[parallelism * le];
                    // send encrpted key to EVH
                    for (int p = 0; p < parallelism; ++p)
                    {
                        BigInteger keyBi = key[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                        //Random rnd = new Random(EnDec.randGen.Next());
                        int offset = p * le;
                        for (int i = 0; i < le; ++i)
                        {
                            ki1[offset + i] = Utility.NextUnsignedNumeric(0);
                            encki1ki[offset + i] = new Numeric((keyBi & mask) == 0 ? 0 : 1, 0) ^ ki1[offset + i];
                            mask <<= 1;
                        }
                    }
                    byte[] toEVH = Message.AssembleMessage(line, opType, false, encki1ki);
                    party.sender.SendTo(PartyType.EVH, toEVH);
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, ki0);
                    break;
                case 2:
                    // AND
                    Numeric[] k0Andk1 = new Numeric[parallelism * le * 2];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * le;
                        for (int i = 0; i < le; ++i)
                        {
                            k0Andk1[(offset + i) * 2] = ki0[offset + i];
                            k0Andk1[(offset + i) * 2 + 1] = ki1[offset + i];
                        }
                    }
                    new ANDOnKH(party, line, this, new NumericArray(k0Andk1), ki2).Run();
                    break;
                case 3:
                    new XORToAddModOnKH(party, line, this, ki2, Aekki).Run();
                    break;
                case 4:
                    Numeric[] re = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        Numeric kp = new Numeric(0, 0), kpp, ks;
                        int offset = p * le;
                        for (int i = 0; i < le; ++i)
                        {
                            kp += Aekki[offset + i];
                        }
                        //System.Diagnostics.Debug.WriteLine("kp: " + kp);
                        kp = new Numeric(2, 0) * kp;
                        kpp = new Numeric(0, 0) - kp;
                        ks = key[p].SumBits(le);
                        //System.Diagnostics.Debug.WriteLine("ks: " + ks);
                        re[p] = (new Numeric(0, 0) - ks + kpp).ModPow(modpow);
                        // re[p].SetEncType(EncryptionType.AddMod);
                    }
                    result.SetArray(re);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //internal class HammingDistance/* : Operation*/
    //{
    //    public static Numeric [] OnEVHHammingDistance(Party party, Numeric [] encVal, int line, int le)
    //    {
    //        int parallelism = encVal.Length;
    //        int modpow = (int)Math.Ceiling(Math.Log(le + 1, 2));
    //        // send K0 to KH
    //        Numeric[]
    //            encki0ei = new Numeric[parallelism * le],
    //            seedForKi0 = new Numeric[parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int seedForKi0Int = Config.KeyByteLength >= sizeof(int) ? Utility.NextInt() : (Utility.NextInt() % (int)Math.Pow(2, Config.KeyBitLength));
    //            seedForKi0[p] = new Numeric(seedForKi0Int, 0);
    //            Random rnd = new Random(seedForKi0Int);
    //            BigInteger encValBi = encVal[p].GetUnsignedBigInteger(), mask = BigInteger.One;
    //            int offset = p * le;
    //            for (int i = 0; i < le; ++i)
    //            {
    //                Numeric 
    //                    ki0 = rnd.NextUnsignedNumeric(0);
    //                encki0ei[offset + i] = new Numeric((encValBi & mask) == 0 ? 0 : 1, 0) ^ ki0;
    //                mask <<= 1;
    //            }
    //        }
    //        var toKH = Message.AssembleMessage(line, OperationType.HammingDistance, false, seedForKi0);
    //        party.sender.SendTo(PartyType.KH, toKH);
    //        // receive encrpted key from KH
    //        byte[] fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        System.Diagnostics.Debug.Assert(fromKH.Length == parallelism * le * Numeric.Size());
    //        var encki1ki = Message.DisassembleMessage(fromKH);
    //        // AND
    //        Numeric [] encValForAnd = new Numeric [parallelism * le * 2];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * le;
    //            for(int i = 0; i < le; ++i)
    //            {
    //                encValForAnd[(offset + i) * 2] = encki0ei[offset + i];
    //                encValForAnd[(offset + i) * 2 + 1] = encki1ki[offset + i];
    //            }
    //        }
    //        var Eeki = AND.OnEVHAND(party, encValForAnd, line);
    //        System.Diagnostics.Debug.Assert(Eeki.Length == parallelism * le);
    //        // XOR to Add
    //        var Aekei = XORToAddMod.OnEVHXORToAddMod(party, Eeki, line);

    //        Numeric [] re = new Numeric [parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            Numeric  enckpek = new Numeric (0, 0), enckpp_ek, es;
    //            int offset = p * le;
    //            for (int i = 0; i < le; ++i)
    //            {
    //                enckpek += Aekei[offset + i];
    //            }
    //            //System.Diagnostics.Debug.WriteLine("enckpek: " + enckpek);
    //            enckpek = new Numeric (2, 0) * enckpek;
    //            enckpp_ek = new Numeric(0, 0) - enckpek;
    //            es = encVal[p].SumBits(le);
    //            //System.Diagnostics.Debug.WriteLine("es: " + es);
    //            re[p] = (es + enckpp_ek).ModPow(modpow);
    //            // re[p].SetEncType(EncryptionType.AddMod);
    //        }
    //        return re;
    //    }

    //    public static Numeric [] OnKHHammingDistance(Party party, Numeric [] key, int line, int le)
    //    {
    //        int parallelism = key.Length;
    //        //int[] scalingFactor = new int[parallelism];
    //        int modpow = (int)Math.Ceiling(Math.Log(le + 1, 2));
    //        Numeric[]
    //            ki1 = new Numeric[parallelism * le],
    //            ki0 = new Numeric[parallelism * le],
    //            encki1ki = new Numeric[parallelism * le];
    //        // send encrpted key to EVH
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            BigInteger keyBi = key[p].GetUnsignedBigInteger(), mask = BigInteger.One;
    //            //Random rnd = new Random(EnDec.randGen.Next());
    //            int offset = p * le;
    //            for (int i = 0; i < le; ++i)
    //            {
    //                ki1[offset + i] = Utility.NextUnsignedNumeric(0);
    //                encki1ki[offset + i] = new Numeric ((keyBi & mask) == 0 ? 0 : 1, 0) ^ ki1[offset + i];
    //                mask <<= 1;
    //            }
    //        }
    //        byte[] toEVH = Message.AssembleMessage(line, OperationType.HammingDistance, false, encki1ki);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        // receive k0 from EVH
    //        byte[] fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
    //        var seedForKi0 = Message.DisassembleMessage(fromEVH);
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int seedForKi0Int = (int)seedForKi0[p].GetUnsignedBigInteger();
    //            Random rnd = new Random(seedForKi0Int);
    //            int offset = p * le;
    //            for(int i = 0; i < le; ++i)
    //            {
    //                ki0[offset + i] = rnd.NextUnsignedNumeric(0);
    //            }
    //        }
    //        // AND
    //        Numeric [] k0Andk1 = new Numeric [parallelism * le * 2];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            int offset = p * le;
    //            for (int i = 0; i < le; ++i)
    //            {
    //                k0Andk1[(offset + i) * 2] = ki0[offset + i];
    //                k0Andk1[(offset + i) * 2 + 1] = ki1[offset + i];
    //            }
    //        }
    //        var ki2 = AND.OnKHAND(party, k0Andk1, line);
    //        // XOR to Add
    //        var Aekki = XORToAddMod.OnKHXORToAddMod(party, ki2, line);

    //        Numeric [] re = new Numeric [parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            Numeric  kp = new Numeric (0, 0), kpp, ks;
    //            int offset = p * le;
    //            for (int i = 0; i < le; ++i)
    //            {
    //                kp += Aekki[offset + i];
    //            }
    //            //System.Diagnostics.Debug.WriteLine("kp: " + kp);
    //            kp = new Numeric (2, 0) * kp;
    //            kpp = new Numeric(0, 0) - kp;
    //            ks = key[p].SumBits(le);
    //            //System.Diagnostics.Debug.WriteLine("ks: " + ks);
    //            re[p] = (new Numeric(0, 0) - ks + kpp).ModPow(modpow);
    //            // re[p].SetEncType(EncryptionType.AddMod);
    //        }
    //        return re;
    //    }
    //}
}
