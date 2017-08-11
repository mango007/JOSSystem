using System;
using System.Numerics;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class XORToAddModOnEVH : OperationOnEVH
    {
        public XORToAddModOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
         : base(party, line, caller, operands, result, OperationType.XORToAddMod)
        { }

        int parallelism;
        byte[] scaleBits;
        NumericArray enc_kfi_ai = new NumericArray();
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallelism = encVal.Length;
                    scaleBits = new byte[parallelism];
                    Numeric[] kipp = new Numeric[parallelism * Config.KeyBits],
                        //seedForKipp = new Numeric[parallelism],
                        enc_kipp_enc_ki_ai = new Numeric[parallelism * Config.KeyBits];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        // System.Diagnostics.Debug.Assert(encVal[p].GetEncType() == EncryptionType.XOR);
                        scaleBits[p] = encVal[p].GetScaleBits();
                        //int seedForKippInt = Config.KeyByteLength >= sizeof(int) ? Utility.NextInt() : (Utility.NextInt() % (int)Math.Pow(2, Config.KeyBitLength));
                        //Random rnd = new Random(seedForKippInt);
                        //seedForKipp[p] = new Numeric(seedForKippInt, 0);
                        BigInteger encValBi = encVal[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                        int offset = p * Config.KeyBits;
                        for (int i = 0; i < Config.KeyBits; ++i)
                        {
                            kipp[offset + i] = Utility.NextUnsignedNumeric(0, Config.KeyBits - i);
                            enc_kipp_enc_ki_ai[offset + i] = ((((encValBi & mask) == BigInteger.Zero) ? new Numeric(0, 0) : new Numeric(1, 0)) + kipp[offset + i]).ModPow(Config.KeyBits - i);
                            mask <<= 1;
                            //System.Diagnostics.Debug.Assert(enckippenckiai[offset + i].GetScalingFactor() == scalingFactor[p]);
                        }
                    }

                    var toKH = Message.AssembleMessage(line, opType, false, kipp);
                    party.sender.SendTo(PartyType.KH, toKH);

                    var toHelper = Message.AssembleMessage(line, opType, true, enc_kipp_enc_ki_ai);
                    party.sender.SendTo(PartyType.Helper, toHelper);

                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enc_kfi_ai);
                    break;
                case 2:
                    Numeric[] enc_kf_a = new Numeric[parallelism];

                    for (int p = 0; p < parallelism; ++p)
                    {
                        enc_kf_a[p] = new Numeric(0, 0);
                        int offset = p * Config.KeyBits;
                        for (int i = 0; i < Config.KeyBits; ++i)
                        {
                            enc_kf_a[p] += enc_kfi_ai[offset + i] << i;
                            //System.Diagnostics.Debug.Assert(enckfiai[offset + i].GetScalingFactor() == scalingFactor[p]);
                        }
                        enc_kf_a[p].SetScaleBits(scaleBits[p]);
                        //enc_kf_a[p].SetEncType(EncryptionType.AddMod);
                        //System.Diagnostics.Debug.Assert(enckfa[p].GetScalingFactor() == scalingFactor[p]);
                    }
                    result.SetArray(enc_kf_a);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    class XORToAddModOnKH: OperationOnKH
    {
        public XORToAddModOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
         : base(party, line, caller, operands, result, OperationType.XORToAddMod)
        { }
        int parallelism, length;
        byte[] scaleBits;
        NumericArray kipp = new NumericArray(), kippp = new NumericArray();
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    parallelism = key.Length;
                    length = Config.KeyBits;
                    scaleBits = new byte[parallelism];
                    var toHelper = Message.AssembleMessage(line, opType, false, key.GetArray());
                    party.sender.SendTo(PartyType.Helper, toHelper);
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, kipp);
                    break;
                case 2:
                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, kippp);
                    break;
                case 3:
                    Numeric[] kf = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        scaleBits[p] = key[p].GetScaleBits();
                        kf[p] = new Numeric(0, 0);
                        //int seedForKippInt = (int)kipp[p].GetUnsignedBigInteger(),
                        //    seedForKipppInt = (int)kippp[p].GetUnsignedBigInteger();
                        //System.Diagnostics.Debug.Assert(seedForKipp[p].GetScalingFactor() == scalingFactor[p]);
                        //System.Diagnostics.Debug.Assert(seedForKippp[p].GetScalingFactor() == scalingFactor[p]);
                        //Random rndKipp = new Random(seedForKippInt), rndKippp = new Random(seedForKipppInt);
                        int offset = length * p;
                        BigInteger keyBi = key[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                        for (int i = 0; i < length; ++i)
                        {
                            Numeric
                                //kipp = rndKipp.NextUnsignedNumeric(0, length - i),
                                //kippp = rndKippp.NextUnsignedNumeric(0, length - i),
                                kfi;
                            if ((keyBi & mask) == BigInteger.Zero)
                            {
                                kfi = (kipp[offset + i] + kippp[offset + i]).ModPow(length - i);
                            }
                            else
                            {
                                kfi = (new Numeric(0, 0) - kipp[offset + i] + kippp[offset + i]).ModPow(length - i);
                            }
                            mask <<= 1;
                            kf[p] += kfi << i;
                        }
                        kf[p].SetScaleBits(scaleBits[p]);
                    }
                    result.SetArray(kf);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    class XORToAddModOnHelper : OperationOnHelper
    {
        public XORToAddModOnHelper(Party party, int line)
            : base(party, line, OperationType.XORToAddMod)
        { }
        NumericArray enc_kipp_enc_ki_ai = new NumericArray(), key = new NumericArray();
        protected override void OnHelper()
        {
            switch(step)
            {
                case 1:
                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, enc_kipp_enc_ki_ai);
                    break;
                case 2:
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, key);
                    break;
                case 3:
                    int length = Config.KeyBits, parallelism = key.Length;
                    Numeric[] kippp = new Numeric[parallelism * length], enc_kfi_ai = new Numeric[parallelism * Config.KeyBits];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        //int seedForKipppInt = Config.KeyByteLength >= sizeof(int) ? Utility.NextInt() : (Utility.NextInt() % (int)Math.Pow(2, Config.KeyBitLength));
                        //Random rnd = new Random(seedForKipppInt);
                        //seedForKippp[p] = new Numeric(seedForKipppInt, 0);

                        BigInteger keyBi = key[p].GetUnsignedBigInteger(), mask = BigInteger.One;
                        int offset = p * length;
                        for (int i = 0; i < length; ++i)
                        {
                            //System.Diagnostics.Debug.Assert(scalingFactor[p] == enckippenckiai[offset + i].GetScalingFactor());
                            kippp[offset + i] = Utility.NextUnsignedNumeric(0, length - i);
                            // kppp[i] = fixedKey;
                            if ((keyBi & mask) == BigInteger.Zero)
                            {
                                enc_kfi_ai[offset + i] = (enc_kipp_enc_ki_ai[offset + i] + kippp[offset + i]).ModPow(length - i);
                            }
                            else
                            {
                                enc_kfi_ai[offset + i] = (new Numeric(0, 0) - enc_kipp_enc_ki_ai[offset + i] + new Numeric(1, 0) + kippp[offset + i]).ModPow(length - i);
                            }
                            mask <<= 1;
                        }
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, enc_kfi_ai);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    var toKH = Message.AssembleMessage(line, opType, false, kippp);
                    party.sender.SendTo(PartyType.KH, toKH);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}
