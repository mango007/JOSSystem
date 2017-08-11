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
    class AddModToXOROnEVH: OperationOnEVH
    {
        public AddModToXOROnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.AddModToXOR)
        { }
        int parallism;
        byte[] scaleBits;
        NumericArray kip_minus_2_times_kmi, eci;
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallism = encVal.Length;
                    scaleBits = new byte[parallism];
                    kip_minus_2_times_kmi = new NumericArray();
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, kip_minus_2_times_kmi);
                    break;
                case 2:
                    var enc_kip_eip = new NumericArray(parallism * Config.KeyBits);
                    for (int p = 0; p < parallism; ++p)
                    {
                        //System.Diagnostics.Debug.Assert(encVal[p].GetEncType() == EncryptionType.AddMod);
                        scaleBits[p] = encVal[p].GetScaleBits();
                        int offset = p * Config.KeyBits;
                        var emi = new Numeric[Config.KeyBits];
                        for (int i = 0; i < Config.KeyBits; ++i)
                        {
                            emi[i] = encVal[p].ModPow(i);
                            emi[i].ResetScaleBits();
                            enc_kip_eip[offset + i] = emi[i] + kip_minus_2_times_kmi[offset + i];
                        }
                    }
                    eci = new NumericArray();
                    new LessZeroOnEVH(party, line, this, enc_kip_eip, eci, Config.KeyBits).Run();
                    break;
                case 3:
                    Numeric[] enc_kf_a = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * Config.KeyBits;
                        BigInteger bi = BigInteger.Zero;
                        for (int i = Config.KeyBits - 1; i >= 0; --i)
                        {
                            bi = (bi << 1) + eci[offset + i].GetUnsignedBigInteger();
                        }
                        enc_kf_a[p] = new Numeric(bi, scaleBits[p]);
                        //System.Diagnostics.Debug.WriteLine("EVH    EncVal: " + encVal[p]);
                        //System.Diagnostics.Debug.WriteLine("EVH CarryBits: " + enckf[p]);
                        enc_kf_a[p] ^= encVal[p];
                        //System.Diagnostics.Debug.WriteLine("EVH   Results: " + enckf[p]);
                        enc_kf_a[p].SetScaleBits(scaleBits[p]);
                        //enckf[p].SetEncType(EncryptionType.XOR);
                    }
                    result.SetArray(enc_kf_a);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class AddModToXOROnKH : OperationOnKH
    {
        public AddModToXOROnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
         : base(party, line, caller, operands, result, OperationType.AddModToXOR)
        { }
        int parallism;
        byte[] scaleBits;
        NumericArray kci;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    parallism = key.Length;
                    scaleBits = new byte[parallism];
                    Numeric[] kip = new Numeric[Config.KeyBits * parallism], kip_minus_2_times_kmi = new Numeric[Config.KeyBits * parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        //System.Diagnostics.Debug.Assert(key[p].GetEncType() == EncryptionType.AddMod);
                        scaleBits[p] = key[p].GetScaleBits();
                        int offset = p * Config.KeyBits;
                        var kmi = new Numeric[Config.KeyBits];
                        for (int i = 0; i < Config.KeyBits; ++i)
                        {
                            kmi[i] = key[p].ModPow(i);
                            kmi[i].ResetScaleBits();
                            kip[offset + i] = Utility.NextUnsignedNumeric(0);
                            kip_minus_2_times_kmi[offset + i] = kip[offset + i] - kmi[i];
                        }
                    }
                    var toEVH = Message.AssembleMessage(line, opType, false, kip_minus_2_times_kmi);
                    party.sender.SendTo(PartyType.EVH, toEVH);
                    kci = new NumericArray();
                    new LessZeroOnKH(party, line, this, new NumericArray(kip), kci, Config.KeyBits).Run();
                    break;
                case 2:
                    Numeric[] kf = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * Config.KeyBits;
                        BigInteger bi = BigInteger.Zero;
                        for (int i = Config.KeyBits - 1; i >= 0; --i)
                        {
                            bi = (bi << 1) + kci[offset + i].GetUnsignedBigInteger();
                        }
                        kf[p] = new Numeric(bi, scaleBits[p]);
                        //System.Diagnostics.Debug.WriteLine("KH        Key: " + key[p]);
                        //System.Diagnostics.Debug.WriteLine("KH  CarryBits: " + kf[p]);
                        kf[p] ^= key[p];
                        //System.Diagnostics.Debug.WriteLine("KH    Results: " + kf[p]);
                        kf[p].SetScaleBits(scaleBits[p]);
                        //kf[p].SetEncType(EncryptionType.XOR);
                    }
                    result.SetArray(kf);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    //class AddModToXOR/* : Operation*/
    //{

    //    public static Numeric[] OnEVHAddModToXOR(Party party, Numeric[] encVal, int line)
    //    {
    //      //  Console.Write(" AtoX ");
    //        int parallism = encVal.Length;
    //        int[] scaleBits = new int[parallism];
    //        var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        var kip_minus_2_times_kmi = Message.DisassembleMessage(fromKH);
    //        var enc_kip_eip = new Numeric[parallism * Config.KeyBitLength];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            //System.Diagnostics.Debug.Assert(encVal[p].GetEncType() == EncryptionType.AddMod);
    //            scaleBits[p] = encVal[p].GetScaleBits();
    //            int offset = p * Config.KeyBitLength;
    //            var emi = new Numeric[Config.KeyBitLength];
    //            for(int i = 0; i < Config.KeyBitLength; ++i)
    //            {
    //                emi[i] = encVal[p].ModPow(i);
    //                emi[i].ResetScaleBits();
    //                enc_kip_eip[offset + i] = emi[i] + kip_minus_2_times_kmi[offset + i];
    //            }
    //        }
    //        var eci = LessZero.OnEVHLessZero(party, enc_kip_eip, line);
    //        Numeric[] enc_kf_a = new Numeric[parallism];
    //        for(int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * Config.KeyBitLength;
    //            BigInteger bi = BigInteger.Zero;
    //            for(int i = Config.KeyBitLength - 1; i >= 0; --i)
    //            {
    //                bi = (bi << 1) + eci[offset + i].GetUnsignedBigInteger();
    //            }
    //            enc_kf_a[p] = new Numeric(bi, scaleBits[p]);
    //            //System.Diagnostics.Debug.WriteLine("EVH    EncVal: " + encVal[p]);
    //            //System.Diagnostics.Debug.WriteLine("EVH CarryBits: " + enckf[p]);
    //            enc_kf_a[p] ^= encVal[p];
    //            //System.Diagnostics.Debug.WriteLine("EVH   Results: " + enckf[p]);
    //            enc_kf_a[p].SetScaleBits(scaleBits[p]);
    //            //enckf[p].SetEncType(EncryptionType.XOR);
    //        }
    //        return enc_kf_a;
    //    }

    //    public static Numeric[] OnKHAddModToXOR(Party party, Numeric[] key, int line)
    //    {
    //        int parallism = key.Length;
    //        int[] scaleBits = new int[parallism];
    //        Numeric[] kip = new Numeric[Config.KeyBitLength * parallism], kip_minus_2_times_kmi = new Numeric[Config.KeyBitLength * parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            //System.Diagnostics.Debug.Assert(key[p].GetEncType() == EncryptionType.AddMod);
    //            scaleBits[p] = key[p].GetScaleBits();
    //            int offset = p * Config.KeyBitLength;
    //            var kmi = new Numeric[Config.KeyBitLength];
    //            for (int i = 0; i < Config.KeyBitLength; ++i)
    //            {
    //                kmi[i] = key[p].ModPow(i);
    //                kmi[i].ResetScaleBits();
    //                kip[offset + i] = Utility.NextUnsignedNumeric(0);
    //                kip_minus_2_times_kmi[offset + i] = kip[offset + i] - kmi[i];
    //            }
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.AddModToXOR, false, kip_minus_2_times_kmi);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var kci = LessZero.OnKHLessZero(party, kip, line);
    //        Numeric[] kf = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * Config.KeyBitLength;
    //            BigInteger bi = BigInteger.Zero;
    //            for (int i = Config.KeyBitLength - 1; i >= 0; --i)
    //            {
    //                bi = (bi << 1) + kci[offset + i].GetUnsignedBigInteger();
    //            }
    //            kf[p] = new Numeric(bi, scaleBits[p]);
    //            //System.Diagnostics.Debug.WriteLine("KH        Key: " + key[p]);
    //            //System.Diagnostics.Debug.WriteLine("KH  CarryBits: " + kf[p]);
    //            kf[p] ^= key[p];
    //            //System.Diagnostics.Debug.WriteLine("KH    Results: " + kf[p]);
    //            kf[p].SetScaleBits(scaleBits[p]);
    //            //kf[p].SetEncType(EncryptionType.XOR);
    //        }
    //        return kf;
    //    }
    //}
}
