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
    class IndexMSBOnEVH: OperationOnEVH
    {
        public IndexMSBOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.IndexMSB)
        {
            this.length = length;
        }
        int length, parallism;
        NumericArray KliMinusK = new NumericArray(), elei = new NumericArray();
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallism = encVal.Length;
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, KliMinusK);
                    break;
                case 2:
                    var ELi = new NumericArray(parallism * length);
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            ELi[offset + i] = encVal[p] - new Numeric(BigInteger.Pow(2, i), 0) + KliMinusK[offset + i];
                        }
                    }
                    new LessZeroOnEVH(party, line, this, ELi, elei, Config.KeyBits).Run();
                    break;
                case 3:
                    var ePow2MSB = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * length;
                        ePow2MSB[p] = elei[offset + length - 1] ^ new Numeric(1, 0);
                        for (int i = length - 2; i >= 0; --i)
                        {
                            ePow2MSB[p] <<= 1;
                            ePow2MSB[p] += elei[offset + i] ^ elei[offset + i + 1];
                        }
                    }
                    result.SetArray(ePow2MSB);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class IndexMSBOnKH: OperationOnKH
    {
        public IndexMSBOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.IndexMSB)
        {
            this.length = length;
        }
        int length, parallism;
        NumericArray klei = new NumericArray();
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    parallism = key.Length;
                    NumericArray Kli = new NumericArray(parallism * length);
                    Numeric[] KliMinusK = new Numeric[parallism * length];
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            Kli[offset + i] = Utility.NextUnsignedNumeric(0);
                            KliMinusK[offset + i] = Kli[offset + i] - key[p];
                        }
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, KliMinusK);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    new LessZeroOnKH(party, line, this, Kli, klei, Config.KeyBits).Run();
                    break;
                case 2:
                    var kPow2MSB = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        int offset = p * length;
                        kPow2MSB[p] = klei[offset + length - 1] ^ new Numeric(0, 0);
                        for (int i = length - 2; i >= 0; --i)
                        {
                            kPow2MSB[p] <<= 1;
                            kPow2MSB[p] += klei[offset + i] ^ klei[offset + i + 1];
                        }
                    }
                    result.SetArray(kPow2MSB);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class IndexMSB
    //{
    //    public static Numeric[] OnEVHIndexMSB(Party party, Numeric[] encVal, int line, int length)
    //    {
    //        int parallism = encVal.Length;

    //        var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
    //        var KliMinusK = Message.DisassembleMessage(fromKH);

    //        Numeric[] ELi = new Numeric[parallism * length];
    //        for(int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * length;
    //            for (int i = 0; i < length; ++i)
    //            {
    //                ELi[offset + i] = encVal[p] - new Numeric(BigInteger.Pow(2, i), 0) + KliMinusK[offset + i];
    //            }
    //        }

    //        var elei = LessZero.OnEVHLessZero(party, ELi, line);

    //        var ePow2MSB = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {                
    //            int offset = p * length;
    //            ePow2MSB[p] = elei[offset + length - 1] ^ new Numeric(1, 0);
    //            for (int i = length - 2; i >= 0; --i)
    //            {
    //                ePow2MSB[p] <<= 1;
    //                ePow2MSB[p] += elei[offset + i] ^ elei[offset + i + 1];
    //            }
    //        }
    //        return ePow2MSB;
    //    }

    //    public static Numeric[] OnKHIndexMSB(Party party, Numeric[] key, int line, int length)
    //    {
    //        int parallism = key.Length;

    //        Numeric[] Kli = new Numeric[parallism * length], KliMinusK = new Numeric[parallism * length];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * length;
    //            for (int i = 0; i < length; ++i)
    //            {
    //                Kli[offset + i] = Utility.NextUnsignedNumeric(0);
    //                KliMinusK[offset + i] = Kli[offset + i] - key[p];
    //            }
    //        }

    //        var toEVH = Message.AssembleMessage(line, OperationType.IndexMSB, false, KliMinusK);
    //        party.sender.SendTo(PartyType.EVH, toEVH);

    //        var klei = LessZero.OnKHLessZero(party, Kli, line);

    //        var kPow2MSB = new Numeric[parallism];
    //        for (int p = 0; p < parallism; ++p)
    //        {
    //            int offset = p * length;
    //            kPow2MSB[p] = klei[offset + length - 1] ^ new Numeric(0, 0);
    //            for (int i = length - 2; i >= 0; --i)
    //            {
    //                kPow2MSB[p] <<= 1;
    //                kPow2MSB[p] += klei[offset + i] ^ klei[offset + i + 1];
    //            }
    //        }
    //        return kPow2MSB;
    //    }
    //}

}
