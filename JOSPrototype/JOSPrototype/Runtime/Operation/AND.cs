using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class ANDOnEVH: OperationOnEVH
    {
        public ANDOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.AND)
        { }
        public ANDOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.AND)
        { }
        public ANDOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.AND)
        { }
        int parallelism;
        byte[] scaleBits;
        Numeric[] enckbxork2b, enckaa, enc_kf_a_AND_b;
        NumericArray enck7p7Andk5 = new NumericArray(), k6 = new NumericArray();
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
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
                    parallelism = encVal.Length / 2;
                    enc_kf_a_AND_b = new Numeric[parallelism];
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        System.Diagnostics.Debug.Assert(parallelism == 1);
                        Numeric.Scale(encVal[0], encVal[1]);   
                        // **tested**                     
                        // both operands are not encrypted 
                        if(encVal[0].GetEncType() == EncryptionType.None && encVal[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                        }
                        // if at least one operand is not encrypted
                        if (encVal[0].GetEncType() == EncryptionType.None || encVal[1].GetEncType() == EncryptionType.None)
                        {
                            enc_kf_a_AND_b[0] = encVal[0] & encVal[1];
                            // jump to round 5
                            step = 4;
                            Run();
                            break;
                        }
                    }                                       
                    scaleBits = new byte[parallelism];
                    enckbxork2b = new Numeric[parallelism];
                    enckaa = new Numeric[parallelism];
                    Numeric[]
                        enckaaAndK2 = new Numeric[2 * parallelism],
                        enckbb = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        enckaa[p] = encVal[2 * p];
                        enckbb[p] = encVal[2 * p + 1];
                        System.Diagnostics.Debug.Assert(enckaa[p].GetScaleBits() == enckbb[p].GetScaleBits());
                        scaleBits[p] = enckaa[p].GetScaleBits();
                        //enckaa[p].ResetScalingFactor();
                        //enckbb[p].ResetScalingFactor();

                        Numeric
                            k2 = Utility.NextUnsignedNumeric(scaleBits[p]);
                        enckbxork2b[p] = enckbb[p] ^ k2;
                        enckaaAndK2[2 * p] = enckaa[p];
                        enckaaAndK2[2 * p + 1] = k2;
                    }

                    byte[] toKH = Message.AssembleMessage(line, opType, false, enckbxork2b),
                           toHelper = Message.AssembleMessage(line, opType, true, enckaaAndK2);
                    party.sender.SendTo(PartyType.KH, toKH);
                    party.sender.SendTo(PartyType.Helper, toHelper);
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, k6);
                    break;
                case 3:
                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enck7p7Andk5);
                    break;
                case 4:
                    for (int p = 0; p < parallelism; ++p)
                    {
                        Numeric
                            t0 = enckaa[p] & enckbxork2b[p],
                            enck7p7 = enck7p7Andk5[2 * p],
                            k5 = enck7p7Andk5[2 * p + 1];
                        enc_kf_a_AND_b[p] = t0 ^ enck7p7 ^ (k5 & k6[p]);
                        //enckfab[p].SetScalingFactor(scalingFactor[p]);
                    }
                    Run();
                    break;
                case 5:
                    SetResult(encType, enc_kf_a_AND_b);
                    break;
                case 6:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class ANDOnKH : OperationOnKH
    {
        public ANDOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.AND)
        { }
        public ANDOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.AND)
        { }
        public ANDOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.AND)
        { }
        int parallelism;
        byte[] scaleBits;
        Numeric[] ka, k6, kf;
        NumericArray enckbXORk2b = new NumericArray(), enck5kbXORk2AndK7 = new NumericArray();
        EncryptionType encType;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
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
                    kf = new Numeric[parallelism];
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        System.Diagnostics.Debug.Assert(parallelism == 1);
                        Numeric.Scale(key[0], key[1]);    
                        if (key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;                       
                        }
                        // if at least one operand is not encrypted
                        if (key[0].GetEncType() == EncryptionType.None || key[1].GetEncType() == EncryptionType.None)
                        {
                            kf[0] = key[0] & key[1];
                            // jump to round 5
                            step = 4;
                            Run();
                            break;
                        }
                    }
                    scaleBits = new byte[parallelism];
                    // byte[] k6 = fixedKey6;
                    k6 = new Numeric[parallelism];
                    ka = new Numeric[parallelism];
                    Numeric[]
                        kbAndEnck6ka = new Numeric[2 * parallelism],
                        kb = new Numeric[parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        ka[p] = key[2 * p];
                        kb[p] = key[2 * p + 1];
                        System.Diagnostics.Debug.Assert(ka[p].GetScaleBits() == kb[p].GetScaleBits());
                        scaleBits[p] = ka[p].GetScaleBits();
                        //ka[p].ResetScalingFactor();
                        //kb[p].ResetScalingFactor();
                        k6[p] = Utility.NextUnsignedNumeric(scaleBits[p]);
                        Numeric
                            enck6ka = ka[p] ^ k6[p];
                        kbAndEnck6ka[2 * p] = kb[p];
                        kbAndEnck6ka[2 * p + 1] = enck6ka;
                    }

                    byte[] toEVH = Message.AssembleMessage(line, opType, false, k6);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    byte[] toHelper = Message.AssembleMessage(line, opType, false, kbAndEnck6ka);
                    party.sender.SendTo(PartyType.Helper, toHelper);

                    party.receiver.ReceiveFrom(PartyType.EVH, line, this, enckbXORk2b);
                    break;
                case 3:
                    party.receiver.ReceiveFrom(PartyType.Helper, line, this, enck5kbXORk2AndK7);
                    break;
                case 4:
                    for (int p = 0; p < parallelism; ++p)
                    {
                        Numeric
                            enck5kbXORk2 = enck5kbXORk2AndK7[2 * p],
                            k7 = enck5kbXORk2AndK7[2 * p + 1],
                            t2 = ka[p] & enckbXORk2b[p],
                            t3 = k6[p] & enck5kbXORk2;
                        kf[p] = k7 ^ t2 ^ t3;
                        //kf[p].SetScalingFactor(scalingFactor[p]);
                    }
                    Run();
                    break;
                case 5:
                    SetResult(encType, kf);
                    break;
                case 6:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class ANDOnHelper : OperationOnHelper
    {
        public ANDOnHelper(Party party, int line)
            : base(party, line, OperationType.AND)
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
                    int parallelism = enckaaAndK2.Length / 2;
                    var scaleBits = new byte[parallelism];
                    Numeric[]
                        enck7p7AndK5 = new Numeric[2 * parallelism],
                        encK5KbXORK2AndK7 = new Numeric[2 * parallelism];
                    for (int p = 0; p < parallelism; ++p)
                    {
                        scaleBits[p] = enckaaAndK2[2 * p].GetScaleBits();
                        System.Diagnostics.Debug.Assert(enckaaAndK2[2 * p + 1].GetScaleBits() == scaleBits[p]);
                        System.Diagnostics.Debug.Assert(kbAndEnck6ka[2 * p].GetScaleBits() == scaleBits[p]);
                        System.Diagnostics.Debug.Assert(kbAndEnck6ka[2 * p + 1].GetScaleBits() == scaleBits[p]);
                        Numeric
                            k5 = Utility.NextUnsignedNumeric(scaleBits[p]),
                            k7 = Utility.NextUnsignedNumeric(scaleBits[p]),
                            enckaa = enckaaAndK2[2 * p],
                            k2 = enckaaAndK2[2 * p + 1],
                            kb = kbAndEnck6ka[2 * p],
                            enck6ka = kbAndEnck6ka[2 * p + 1],
                            encK5KbXORK2 = kb ^ k2 ^ k5,
                            t1 = enckaa & (kb ^ k2),
                            enck7p7 = t1 ^ (enck6ka & (kb ^ k2)) ^ k7;
                        enck7p7AndK5[2 * p] = enck7p7;
                        enck7p7AndK5[2 * p + 1] = k5;
                        encK5KbXORK2AndK7[2 * p] = encK5KbXORK2;
                        encK5KbXORK2AndK7[2 * p + 1] = k7;
                    }

                    var toEVH = Message.AssembleMessage(line, opType, false, enck7p7AndK5);
                    party.sender.SendTo(PartyType.EVH, toEVH);

                    var toKH = Message.AssembleMessage(line, opType, false, encK5KbXORK2AndK7);
                    party.sender.SendTo(PartyType.KH, toKH);
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}
