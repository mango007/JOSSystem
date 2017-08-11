using JOSPrototype.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Operation
{
    class XOROnEVH: OperationOnEVH
    {
        public XOROnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.XOR)
        { }
        public XOROnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.XOR)
        { }

        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    Numeric
                        enckaa = program.GetValue(code.operand1),
                        enckbb = program.GetValue(code.operand2);
                    // XOR needs XOR encryption
                    TransformEncType(enckaa, enckbb);
                    break;
                case 2:
                    // make sure two operands have same scaleBits
                    Numeric.Scale(encVal[0], encVal[1]);
                    EncryptionType encType = resultEncType;
                    // **tested**
                    if (encVal[0].GetEncType() == EncryptionType.None && encVal[1].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                    }
                    Numeric enc_kf_a_XOR_b = encVal[0] ^ encVal[1];
                    SetResult(encType, enc_kf_a_XOR_b);
                    break;
                case 3:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class XOROnKH : OperationOnKH
    {
        public XOROnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.XOR)
        { }
        public XOROnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.XOR)
        { }
        protected override void OnKH()
        {
            switch (step)
            {
                case 1:
                    Numeric
                        ka = program.GetValue(code.operand1),
                        kb = program.GetValue(code.operand2);
                    // XOR needs XOR encryption
                    TransformEncType(ka, kb);
                    break;
                case 2:
                    Numeric.Scale(key[0], key[1]);
                    EncryptionType encType = resultEncType;
                    Numeric kf;
                    if (key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        kf = key[0] ^ key[1];
                    }
                    else if (key[0].GetEncType() == EncryptionType.None && key[1].GetEncType() != EncryptionType.None)
                    {
                        kf = key[1];
                    }
                    else if (key[0].GetEncType() != EncryptionType.None && key[1].GetEncType() == EncryptionType.None)
                    {
                        kf = key[0];
                    }
                    else
                    {
                        kf = key[0] ^ key[1];
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
}
