using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;

namespace JOSPrototype.Runtime.Operation
{
    class NoneOnEVH: OperationOnEVH
    {
        public NoneOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, OperationType.None)
        { }
        public NoneOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, caller, OperationType.None)
        { }

        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    Numeric enckaa = program.GetValue(code.operand1);
                    SetResult(enckaa.GetEncType(), enckaa);
                    break;
                case 2:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }        
        }
    }
    class NoneOnKH: OperationOnKH
    {
        public NoneOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, OperationType.None)
        { }
        public NoneOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.Any, EncryptionType.Any, caller, OperationType.None)
        { }
        protected override void OnKH()
        {
            switch (step)
            {
                case 1:
                    Numeric ka = program.GetValue(code.operand1);
                    SetResult(ka.GetEncType(), ka);
                    break;
                case 2:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
            }
        }
    }
//    class None : Operation
//    {
//        public None() : base(EncryptionType.Any, EncryptionType.Any) { }
//        public override void OnEVH(Party party, ICAssignment code, Program program)
//        {
//            Numeric enckaa = program.GetValue(code.operand1);
//            program.SetValue(code.result, enckaa);
//        }

//        public override void OnHelper(Party party, int line)
//        {
//            throw new NotImplementedException();
//        }

//        public override void OnKH(Party party, ICAssignment code, Program program)
//        {
//            Numeric ka = program.GetValue(code.operand1);
//            program.SetValue(code.result, ka);
//        }
//    }
}
