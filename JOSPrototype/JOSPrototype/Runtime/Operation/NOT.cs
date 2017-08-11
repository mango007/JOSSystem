using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;

namespace JOSPrototype.Runtime.Operation
{
    // boolean NOT, not bitwise not
    class NOTOnEVH: OperationOnEVH
    {
        public NOTOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.NOT)
        { }
        public NOTOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.NOT)
        { }
        public NOTOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.NOT)
        { }
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    if(!ReferenceEquals(code, null))
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
                    var encType = resultEncType;
                    var enckf = new Numeric[encVal.Length];
                    if (!ReferenceEquals(code, null) && encVal[0].GetEncType() == EncryptionType.None)
                    {                   
                        encType = EncryptionType.None;
                        enckf[0] = new Numeric(1, 0) ^ encVal[0];
                    }
                    else
                    {
                        for (int p = 0; p < encVal.Length; ++p)
                        {
                            enckf[p] = new Numeric(1, 0) ^ encVal[p];
                        }
                    }
                    SetResult(encType, enckf);
                    break;
                case 3:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
    class NOTOnKH: OperationOnKH
    {
        public NOTOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.NOT)
        { }
        public NOTOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, OperationType.NOT)
        { }
        public NOTOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.XOR, EncryptionType.XOR, caller, OperationType.NOT)
        { }
        protected override void OnKH()
        {
            switch (step)
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
                    var encType = resultEncType;
                    var kf = new Numeric[key.Length];
                    if (!ReferenceEquals(code, null) && key[0].GetEncType() == EncryptionType.None)
                    {
                        encType = EncryptionType.None;
                        kf[0] = new Numeric(1, 0) ^ key[0];
                    }
                    else
                    {
                        for (int p = 0; p < key.Length; ++p)
                        {
                            kf[p] =  key[p];
                        }
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
    //class NOT : Operation
    //{
    //    public NOT() : base(EncryptionType.XOR, EncryptionType.XOR) { }
    //    public override void OnEVH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            enckaa = program.GetValue(code.operand1);
    //        var encVal = new Numeric[] { TransformEncType(enckaa, party, code.index) };
    //        var enckf = new Numeric(encVal[0]);
    //        enckf ^= new Numeric(1, 0);
    //        enckf.SetEncType(resultEncType);
    //        program.SetValue(code.result, enckf);
    //    }

    //    public override void OnHelper(Party party, int line)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void OnKH(Party party, ICAssignment code, Program program)
    //    {
    //        Numeric
    //            ka = program.GetValue(code.operand1);
    //        var key = new Numeric[] { TransformEncType(ka, party, code.index) };
    //        var kf = new Numeric(key[0]);
    //        kf.SetEncType(resultEncType);
    //        program.SetValue(code.result, kf);
    //    }
    }
}
