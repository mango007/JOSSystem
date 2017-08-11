using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class MultiLessZeroOnEVH: OperationOnEVH
    {
        public MultiLessZeroOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }
        public MultiLessZeroOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }
        public MultiLessZeroOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, caller, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }   
        const int ne = 5;
        int length, parallelism;
        NumericArray KInversePlusKip, elei, enckf;
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
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
                    // input value is not encrypted
                    if (!ReferenceEquals(code, null) && encVal[0].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        enckf = new NumericArray(1);
                        if (encVal[0].GetUnsignedBigInteger() < 0)
                        {
                            enckf[0] = new Numeric(1, 0);
                        }
                        else
                        {
                            enckf[0] = new Numeric(0, 0);
                        }
                        // jump to round 6
                        step = 5;
                        Run();
                        break;
                    }
                    parallelism = encVal.Length;
                    KInversePlusKip = new NumericArray();
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, KInversePlusKip);
                    break;
                case 3:
                    var Enckipa = new NumericArray(parallelism * ne);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * ne;
                        for (int i = 0; i < ne; i++)
                        {
                            Enckipa[offset + i] = encVal[p] + KInversePlusKip[offset + i];
                        }
                    }
                    elei = new NumericArray();
                    new LessZeroOnEVH(party, line, this, Enckipa, elei, length).Run();
                    break;
                case 4:
                    new XORToAddModOnEVH(party, line, this, elei, elei).Run();
                    break;
                case 5:
                    var sele = new NumericArray(parallelism);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        sele[p] = new Numeric(ne / 2, 0);
                        int offset = p * ne;
                        for (int i = 0; i < ne; i++)
                        {
                            sele[p] -= elei[offset + i];
                        }
                        //System.Diagnostics.Debug.WriteLine("EVH SUM: " + sele[p].ToUnsignedBigInteger());
                    }
                    enckf = new NumericArray();
                    new LessZeroOnEVH(party, line, this, sele, enckf, length).Run();
                    break;
                case 6:
                    SetResult(encType, enckf.GetArray());
                    break;
                case 7:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class MultiLessZeroOnKH: OperationOnKH
    {
        public MultiLessZeroOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }
        public MultiLessZeroOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }
        public MultiLessZeroOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.XOR, caller, OperationType.MultiLessZero)
        {
            length = Config.KeyBits;
        }
        const int ne = 5;
        int length, parallelism;
        NumericArray klei, kf;
        EncryptionType encType;
        protected override void OnKH()
        {
            switch(step)
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
                    encType = resultEncType;
                    // input value is not encrypted
                    if (!ReferenceEquals(code, null) && key[0].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        kf = new NumericArray(1);
                        if (key[0].GetUnsignedBigInteger() < 0)
                        {
                            kf[0] = new Numeric(1, 0);
                        }
                        else
                        {
                            kf[0] = new Numeric(0, 0);
                        }
                        // jump to round 5
                        step = 4;
                        Run();
                        break;
                    }
                    parallelism = key.Length;
                    var Kip = new NumericArray(parallelism * ne);
                    var KInversePlusKip = new Numeric[parallelism * ne];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        int offset = p * ne;
                        for (int i = 0; i < ne; ++i)
                        {
                            Kip[offset + i] = Utility.NextUnsignedNumeric(key[p].GetScaleBits());
                            KInversePlusKip[offset + i] = Kip[offset + i] - key[p];
                        }
                    }
                    var toEVH = Message.AssembleMessage(line, opType, false, KInversePlusKip);
                    party.sender.SendTo(PartyType.EVH, toEVH);
                    klei = new NumericArray();
                    new LessZeroOnKH(party, line, this, Kip, klei, length).Run();
                    break;
                case 3:
                    new XORToAddModOnKH(party, line, this, klei, klei).Run();
                    break;
                case 4:
                    var skle = new NumericArray(parallelism);
                    for (int p = 0; p < parallelism; ++p)
                    {
                        skle[p] = new Numeric(0, 0);
                        int offset = p * ne;
                        for (int i = 0; i < ne; i++)
                        {
                            skle[p] -= klei[offset + i];
                        }
                        //System.Diagnostics.Debug.WriteLine("KH  SUM: " + skle[p].ToUnsignedBigInteger());
                    }
                    kf = new NumericArray();
                    new LessZeroOnKH(party, line, this, skle, kf, length).Run();
                    break;
                case 5:
                    SetResult(encType, kf.GetArray());
                    break;
                case 6:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
//    class MultiLessZero : Operation
//    {
//        public MultiLessZero() : base(EncryptionType.AddMod, EncryptionType.XOR) { }
//        public override void OnEVH(Party party, ICAssignment code, Program program)
//        {
//            Numeric
//                enckaa = program.GetValue(code.operand1);
//            var encVal = new Numeric[] { TransformEncType(enckaa, party, code.index) };
//            var enckf = OnEVHMultiLessZero(party, encVal, code.index);
//            enckf[0].SetEncType(resultEncType);
//            program.SetValue(code.result, enckf[0]);
//        }

//        public override void OnHelper(Party party, int line)
//        {
//            throw new NotImplementedException();
//        }

//        public override void OnKH(Party party, ICAssignment code, Program program)
//        {
//            Numeric
//                ka = program.GetValue(code.operand1);
//            var key = new Numeric[] { TransformEncType(ka, party, code.index) };
//            var kf = OnKHMultiLessZero(party, key, code.index);
//            kf[0].SetEncType(resultEncType);
//            program.SetValue(code.result, kf[0]);
//        }

//        public static Numeric[] OnEVHMultiLessZero(Party party, Numeric[] encVal, int line)
//        {
//            int parallelism = encVal.Length;
//            byte[] fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
//            var KInversePlusKip = Message.DisassembleMessage(fromKH);
//            Numeric[] Enckipa = new Numeric[parallelism * ne];
//            for (int p = 0; p < parallelism; ++p)
//            {
//                int offset = p * ne;
//                for (int i = 0; i < ne; i++)
//                {
//                    Enckipa[offset + i] = encVal[p] + KInversePlusKip[offset + i];
//                }
//            }
//            var elei = LessZero.OnEVHLessZero(party, Enckipa, line);
//            elei = XORToAddMod.OnEVHXORToAddMod(party, elei, line);
//            var sele = new Numeric[parallelism];
//            for (int p = 0; p < parallelism; ++p)
//            {
//                sele[p] = new Numeric(ne / 2, 0);
//                int offset = p * ne;
//                for (int i = 0; i < ne; i++)
//                {
//                    sele[p] -= elei[offset + i];
//                }
//                //System.Diagnostics.Debug.WriteLine("EVH SUM: " + sele[p].ToUnsignedBigInteger());
//            }
//            return LessZero.OnEVHLessZero(party, sele, line);
//        }

//        public static Numeric[] OnKHMultiLessZero(Party party, Numeric[] key, int line)
//        {
//            int parallelism = key.Length;
//            Numeric[] Kip = new Numeric[parallelism * ne];
//            var KInversePlusKip = new Numeric[parallelism * ne];
//            for (int p = 0; p < parallelism; ++p)
//            {
//                int offset = p * ne;
//                for (int i = 0; i < ne; ++i)
//                {
//                    Kip[offset + i] = Utility.NextUnsignedNumeric(key[p].GetScaleBits());
//                    KInversePlusKip[offset + i] = Kip[offset + i] - key[p];
//                }
//            }
//            var toEVH = Message.AssembleMessage(line, OperationType.MultiLessZero, false, KInversePlusKip);
//            party.sender.SendTo(PartyType.EVH, toEVH);

//            var klei = LessZero.OnKHLessZero(party, Kip, line);
//            klei = XORToAddMod.OnKHXORToAddMod(party, klei, line);
//            var skle = new Numeric[parallelism];
//            for (int p = 0; p < parallelism; ++p)
//            {
//                skle[p] = new Numeric(0, 0);
//                int offset = p * ne;
//                for (int i = 0; i < ne; i++)
//                {
//                    skle[p] -= klei[offset + i];
//                }
//                //System.Diagnostics.Debug.WriteLine("KH  SUM: " + skle[p].ToUnsignedBigInteger());
//            }
//            return LessZero.OnKHLessZero(party, skle, line);
//        }
//        private static int ne = 5;
//    }
}
