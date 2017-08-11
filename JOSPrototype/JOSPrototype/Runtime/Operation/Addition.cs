using System;
using System.Collections.Generic;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class AdditionOnEVH: OperationOnEVH
    {
        public AdditionOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Addition)
        { }
        public AdditionOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Addition)
        { }
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    Numeric
                        enckaa = program.GetValue(code.operand1),
                        enckbb = program.GetValue(code.operand2);
                    // Addition needs AddMod encryption
                    TransformEncType(enckaa, enckbb);
                    break;
                case 2:
                    // make sure two operands have same scaleBits
                    Numeric.Scale(encVal[0], encVal[1]);
                    EncryptionType encType = resultEncType;
                    // **tested**
                    if(encVal[0].GetEncType() == EncryptionType.None && encVal[1].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                    }
                    Numeric enc_kf_a_plus_b = encVal[0] + encVal[1];
                    SetResult(encType, enc_kf_a_plus_b);
                    break;
                case 3:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class AdditionOnKH: OperationOnKH
    {
        public AdditionOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, OperationType.Addition)
        { }
        public AdditionOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.AddMod, EncryptionType.AddMod, caller, OperationType.Addition)
        { }
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    Numeric
                        ka = program.GetValue(code.operand1),
                        kb = program.GetValue(code.operand2);
                    // Addition needs AddMod encryption
                    TransformEncType(ka, kb);
                    break;
                case 2:
                    Numeric.Scale(key[0], key[1]);
                    EncryptionType encType = resultEncType;
                    Numeric kf;
                    if (key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        kf = key[0] + key[1];
                    }
                    else if(key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() != EncryptionType.None)
                    {
                        kf = key[1];
                    }
                    else if(key[0].GetEncType() != EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                    {
                        kf = key[0];
                    }
                    else
                    {
                        kf = key[0] + key[1];
                    }
                    SetResult(encType, kf);
                    break;
                case 3:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    //class Addition : Operation
    //{
    //    public Addition() : base(EncryptionType.AddMod, EncryptionType.AddMod) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric  
    //            enckaa = program.GetValue(code.operand1),
    //            enckbb = program.GetValue(code.operand2);
    //        // make sure two operands have same scaleBits
    //        Numeric.Scale(enckaa, enckbb);
    //        // Addition needs AddMod encryption
    //        Numeric[] encVal = TransformEncType(enckaa, enckbb, party, code.index);
    //        //System.Diagnostics.Debug.Assert(encVal[0].GetEncType() == EncryptionType.AddMod && encVal[1].GetEncType() == EncryptionType.AddMod);
    //        Numeric  enc_kf_a_plus_b = encVal[0] + encVal[1];

    //        enc_kf_a_plus_b.SetEncType(resultEncType);
    //        program.SetValue(code.result, enc_kf_a_plus_b);
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
    //        //System.Diagnostics.Debug.Assert(key[0].GetEncType() == EncryptionType.AddMod && key[1].GetEncType() == EncryptionType.AddMod);          

    //        Numeric  kf = key[0] + key[1];
    //        kf.SetEncType(resultEncType);
    //        program.SetValue(code.result, kf);
    //    }
    //}
}
