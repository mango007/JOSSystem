using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
    class OROnEVH: OperationOnEVH
    {
        public OROnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.OR)
        { }
        public OROnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.OR)
        { }
        public OROnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.OR)
        { }
        int parallelism;
        NumericArray encek = new NumericArray();
        EncryptionType encType;
        Numeric[] enc_kf_a_OR_b;
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    if (!ReferenceEquals(code, null))
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
                    enc_kf_a_OR_b = new Numeric[parallelism];
                    encType = resultEncType;
                    // **tested**
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric.Scale(encVal[0], encVal[1]);
                        // both operands are not encrypted 
                        if (encVal[0].GetEncType() == EncryptionType.None && encVal[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                        }
                        // if at least one operand is not encrypted
                        // if one operand is encrpted while the other is not, according to the fomula a | (b ^ kb) = (a | b) ^ (~a ^ kb),
                        // the encrypted value will be a | enc_kb_b and the key will be ~a ^ kb
                        if (encVal[0].GetEncType() == EncryptionType.None || encVal[1].GetEncType() == EncryptionType.None)
                        {
                            enc_kf_a_OR_b[0] = encVal[0] | encVal[1];
                            // jump to round 4
                            step = 3;
                            Run();
                            break;
                        }
                    }
                    
                    new ANDOnEVH(party, line, this, encVal, encek).Run();
                    break;
                case 3:
                    // a | b = (a & b) ^ (a ^ b)
                    for (int p = 0; p < parallelism; ++p)
                    {
                        enc_kf_a_OR_b[p] = (encVal[2 * p] ^ encVal[2 * p + 1]) ^ encek[p];
                    }
                    Run();
                    break;
                case 4:
                    SetResult(encType, enc_kf_a_OR_b);
                    break;
                case 5:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class OROnKH: OperationOnKH
    {
        public OROnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.OR)
        { }
        public OROnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.OR)
        { }
        public OROnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.OR)
        { }
        int parallelism;
        NumericArray encek = new NumericArray();
        Numeric[] kf;
        EncryptionType encType;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
                    {
                        Numeric
                            ka = program.GetValue(code.operand1),
                            kb = program.GetValue(code.operand2);                      
                        TransformEncType(ka, kb);
                    }
                    else
                    {
                        Run();
                    }
                    break;
                case 2:
                    parallelism = key.Length / 2;
                    encType = resultEncType;
                    kf = new Numeric[parallelism];
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric.Scale(key[0], key[1]);
                        if(key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            kf[0] = key[0] | key[1];
                        }
                        else if(key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() != EncryptionType.None)
                        {
                            kf[0] = (~key[0]) & key[1];
                        }
                        else if(key[0].GetEncType() != EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                        {
                            kf[0] = key[0] & (~key[1]);
                        }
                        if(key[0].GetEncType() == EncryptionType.None || key[1].GetEncType() == EncryptionType.None)
                        {
                            // jump to round 4
                            step = 3;
                            Run();
                            break;
                        }
                    }                  
                    new ANDOnKH(party, line, this, key, encek).Run();
                    break;
                case 3: 
                    for (int p = 0; p < parallelism; ++p)
                    {
                        kf[p] = (key[2 * p] ^ key[2 * p + 1]) ^ encek[p];
                    }
                    Run();
                    break;
                case 4:
                    SetResult(encType, kf);
                    break;
                case 5:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class OR: Operation
    //{
    //    public OR() : base(EncryptionType.XOR, EncryptionType.XOR) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric 
    //            enckaa = program.GetValue(code.operand1),
    //            enckbb = program.GetValue(code.operand2);
    //        Numeric.Scale(enckaa, enckbb);
    //        // OR needs XOR encryption
    //        Numeric[] encVal = TransformEncType(enckaa, enckbb, party, code.index);         
    //        var enckfab = OnEVHOR(party, encVal, code.index);
    //        enckfab[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, enckfab[0]);
    //    }

    //    public override void OnHelper(Party party, int line)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric 
    //            ka = program.GetValue(code.operand1),
    //            kb = program.GetValue(code.operand2);
    //        Numeric.Scale(ka, kb);
    //        Numeric[] key = TransformEncType(ka, kb, party, code.index);
    //        var kf = OnKHOR(party, key, code.index);
    //        kf[0].SetEncType(resultEncType);
    //        program.SetValue(code.result, kf[0]);
    //    }

    //    public static Numeric [] OnEVHOR(Party party, Numeric [] encVal, int line)
    //    {
    //        int parallelism = encVal.Length / 2;
    //        // var enckaaAndEncKbb = new Numeric[parallelism * 2];
    //        var encek = AND.OnEVHAND(party, encVal, line);
    //        var enckfaORb = new Numeric [parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            enckfaORb[p] = (encVal[2 * p] ^ encVal[2 * p + 1]) ^ encek[p];
    //            //enckfaORb[p].SetEncType(EncryptionType.XOR);
    //        }
    //        return enckfaORb;
    //    }

    //    public static Numeric [] OnKHOR(Party party, Numeric [] key, int line)
    //    {
    //        int parallelism = key.Length / 2;
    //        var encek = AND.OnKHAND(party, key, line);
    //        var kf = new Numeric [parallelism];
    //        for (int p = 0; p < parallelism; ++p)
    //        {
    //            kf[p] = (key[2 * p] ^ key[2 * p + 1]) ^ encek[p];
    //            //kf[p].SetEncType(EncryptionType.XOR);
    //        }
    //        return kf;
    //    }
    //}
}
