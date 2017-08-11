using System.Collections.Generic;
using System;
using JOSPrototype.Components;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Remoting;

namespace JOSPrototype.Runtime.Operation
{
    abstract class Operation
    {
        public OperationType opType;
        public abstract void Run();
        protected Operation(Party party, int line, Operation caller, NumericArray result, OperationType opType)
        {            
            this.party = party;        
            this.line = line;
            this.caller = caller;
            this.result = result;
            step = 0;
            this.opType = opType;
        }

        protected Party party;      
        protected int line;
        protected int step;
        // the reference of operation which uses current operation as subfunction
        protected Operation caller;
        // store the received message
        protected NumericArray result;
    }

    abstract class OperationOnEVH: Operation
    {
        public sealed override void Run()
        {
            step++;
            Task.Factory.StartNew(OnEVH);
        }
        // operation is created as subfunction
        protected OperationOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, OperationType opType)
            :base(party, line, caller, result, opType)
        {
            //isCalledByParty = false;
            encVal = operands;
            code = null;
            program = null;
            operandEncType = EncryptionType.Any;
            resultEncType = EncryptionType.Any;
        }
        // operation is created by party
        protected OperationOnEVH(Party party, ICAssignment code, Program program, EncryptionType operandEncType, EncryptionType resultEncType, OperationType opType)
            :base(party, code.index, null, null, opType)
        {
            //isCalledByParty = true;
            this.code = code;
            this.program = program;
            this.operandEncType = operandEncType;
            this.resultEncType = resultEncType;       
        }
        // operation is created by a while loop or a if-else statement
        protected OperationOnEVH(Party party, ICAssignment code, Program program, EncryptionType operandEncType, EncryptionType resultEncType, Operation caller, OperationType opType)
            : base(party, code.index, caller, null, opType)
        {
            //isCalledByParty = true;
            this.code = code;
            this.program = program;
            this.operandEncType = operandEncType;
            this.resultEncType = resultEncType;
        }
        // nested while loop or if-else statement, instantiated by another loop or if-else
        protected OperationOnEVH(Party party, IntermediateCode code, Program program, Operation caller, OperationType opType)
            : base(party, code.index, caller, null, opType)
        {
            this.program = program;
        }
        // out most while loop or if-else statement, instantiated by party 
        protected OperationOnEVH(Party party, IntermediateCode code, Program program, OperationType opType)
            : base(party, code.index, null, null, opType)
        {
            this.program = program;
        }

        protected NumericArray encVal;
        protected ICAssignment code;
        protected Program program;
        protected EncryptionType operandEncType, resultEncType;

        protected abstract void OnEVH();

        protected void TransformEncType(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(party.Type == PartyType.EVH);
            System.Diagnostics.Debug.Assert(a.GetEncType() != EncryptionType.Any
                && b.GetEncType() != EncryptionType.Any
                && operandEncType != EncryptionType.Any);
            encVal = new NumericArray(a, b);

            switch (operandEncType)
            {
                case EncryptionType.AddMod:
                    switch (a.GetEncType() | b.GetEncType())
                    {
                        case EncryptionType.None | EncryptionType.AddMod:
                        case EncryptionType.None | EncryptionType.None:
                        case EncryptionType.AddMod | EncryptionType.AddMod:
                            Run();
                            break;
                        case EncryptionType.None | EncryptionType.XOR:
                        case EncryptionType.AddMod | EncryptionType.XOR:
                            if (b.GetEncType() == EncryptionType.XOR)
                            {
                                new XORToAddModOnEVH(party, line, this, new NumericArray(b), new NumericArray(b)).Run();
                            }
                            else
                            {
                                new XORToAddModOnEVH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            }
                            break;
                        case EncryptionType.XOR | EncryptionType.XOR:
                            new XORToAddModOnEVH(party, line, this, new NumericArray(a, b), new NumericArray(a, b)).Run();
                            break;                     
                        default:
                            throw new ArgumentException();
                    }
                    break;
                case EncryptionType.XOR:
                    switch (a.GetEncType() | b.GetEncType())
                    {
                        case EncryptionType.None | EncryptionType.None:
                        case EncryptionType.None | EncryptionType.XOR:
                        case EncryptionType.XOR | EncryptionType.XOR:
                            Run();
                            break;
                        case EncryptionType.None | EncryptionType.AddMod:
                        case EncryptionType.AddMod | EncryptionType.XOR:
                            if (b.GetEncType() == EncryptionType.AddMod)
                            {
                                new AddModToXOROnEVH(party, line, this, new NumericArray(b), new NumericArray(b)).Run();
                            }
                            else
                            {
                                new AddModToXOROnEVH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            }
                            break;
                        case EncryptionType.AddMod | EncryptionType.AddMod:
                            new AddModToXOROnEVH(party, line, this, new NumericArray(a, b), new NumericArray(a, b)).Run();
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
        }
        protected void TransformEncType(Numeric a)
        {
            System.Diagnostics.Debug.Assert(party.Type == PartyType.EVH);
            System.Diagnostics.Debug.Assert(a.GetEncType() != EncryptionType.Any
                && operandEncType != EncryptionType.Any);
            encVal = new NumericArray(a);
            switch (operandEncType)
            {
                case EncryptionType.AddMod:
                    switch (a.GetEncType())
                    {
                        case EncryptionType.None:
                        case EncryptionType.AddMod:
                            Run();
                            break;
                        case EncryptionType.XOR:
                            new XORToAddModOnEVH(party, line, this, encVal, encVal).Run();
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    break;
                case EncryptionType.XOR:
                    switch (a.GetEncType())
                    {
                        case EncryptionType.None:
                        case EncryptionType.XOR:
                            Run();
                            break;
                        case EncryptionType.AddMod:
                            new AddModToXOROnEVH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
        }
        protected void Evaluate(IntermediateCode code)
        {
            //System.Diagnostics.Debug.WriteLine("EVH: " + code);
            if (code is ICAssignment)
            {
                ICAssignment ica = (ICAssignment)code;
                // create Operation instance
                ObjectHandle handle = Activator.CreateInstance("JOSPrototype", "JOSPrototype.Runtime.Operation." + ica.op + "OnEVH", false, 0, null, new object[] { party, code, program, this }, null, null);
                Operation opImpl = (Operation)handle.Unwrap();
                // execute the operation on EVH
                opImpl.Run();
            }
            else if (code is ICWhile)
            {
                ICWhile icw = (ICWhile)code;
                WhileOnEVH op = new WhileOnEVH(party, icw, program, this);
                op.Run();
            }
            else
            {
                ICIfElse icie = (ICIfElse)code;
                IfElseOnEVH op = new IfElseOnEVH(party, icie, program, this);
                op.Run();
            }
        }
        protected void SetResult(EncryptionType encType, params Numeric[] encRe)
        {
            if (!ReferenceEquals(code, null))
            {
                // called by party or a statement inside while or if-else
                encRe[0].SetEncType(encType);
                //if(opType == OperationType.LessZero)
                //{
                //    Console.WriteLine("EVH: " + encRe[0].GetUnsignedBigInteger());
                //}
                new ConcealOnEVH(party, line, this, new NumericArray(encRe[0]), code.result, program).Run();
                // program.SetValue(code.result, encRe[0]);
            }
            else
            {
                // as subfunction
                result.SetArray(encRe);
                Run();
            }           
        }
        protected void InvokeCaller()
        {
            if (!ReferenceEquals(caller, null))
            {
                // as a subfunction or a statement inside while or if-else
                caller.Run();
            }
            else
            {
                // called by party
                program.evaluatedIC.AddOrUpdate(code, new Object(), (k, v) => new Object());
            }
        }
    }

    abstract class OperationOnKH : Operation
    {
        protected OperationOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, OperationType opType)
            : base(party, line, caller, result, opType)
        {
            //isCalledByParty = false;
            key = operands;
        }
        protected OperationOnKH(Party party, ICAssignment code, Program program, EncryptionType operandEncType, EncryptionType resultEncType, OperationType opType)
            : base(party, code.index, null, null, opType)
        {
            //isCalledByParty = true;
            this.code = code;
            this.program = program;
            this.operandEncType = operandEncType;
            this.resultEncType = resultEncType;
        }
        // operation is created by a while loop or if-else
        protected OperationOnKH(Party party, ICAssignment code, Program program, EncryptionType operandEncType, EncryptionType resultEncType, Operation caller, OperationType opType)
            : base(party, code.index, caller, null, opType)
        {
            //isCalledByParty = true;
            this.code = code;
            this.program = program;
            this.operandEncType = operandEncType;
            this.resultEncType = resultEncType;
        }
        // out most while loop or if-else,instantiated by party
        protected OperationOnKH(Party party, IntermediateCode code, Program program, OperationType opType)
            : base(party, code.index, null, null, opType)
        {
            //isCalledByParty = true;
            this.program = program;
        }
        // nested while loop or if-else, instantiated by another loop or if-else
        protected OperationOnKH(Party party, IntermediateCode code, Program program, Operation caller, OperationType opType)
            : base(party, code.index, caller, null, opType)
        {
            //isCalledByParty = true;
            this.program = program;
        }

        public sealed override void Run()
        {
            step++;
            Task.Factory.StartNew(OnKH);
        }
        protected NumericArray key;
        //protected bool isCalledByParty;
        protected ICAssignment code;
        protected Program program;
        protected EncryptionType operandEncType, resultEncType;
        protected abstract void OnKH();

        protected void TransformEncType(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(party.Type == PartyType.KH);
            System.Diagnostics.Debug.Assert(a.GetEncType() != EncryptionType.Any
                && b.GetEncType() != EncryptionType.Any
                && operandEncType != EncryptionType.Any);
            key = new NumericArray(a, b);
            switch (operandEncType)
            {
                case EncryptionType.AddMod:
                    switch (a.GetEncType() | b.GetEncType())
                    {
                        case EncryptionType.None | EncryptionType.AddMod:
                        case EncryptionType.None | EncryptionType.None:
                        case EncryptionType.AddMod | EncryptionType.AddMod:
                            Run();
                            break;
                        case EncryptionType.None | EncryptionType.XOR:
                        case EncryptionType.AddMod | EncryptionType.XOR:
                            if (b.GetEncType() == EncryptionType.XOR)
                            {
                                new XORToAddModOnKH(party, line, this, new NumericArray(b), new NumericArray(b)).Run();
                            }
                            else
                            {
                                new XORToAddModOnKH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            }
                            break;
                        case EncryptionType.XOR | EncryptionType.XOR:
                            new XORToAddModOnKH(party, line, this, new NumericArray(a, b), new NumericArray(a, b)).Run();
                            break;                                           
                        default:
                            throw new ArgumentException();
                    }
                    break;
                case EncryptionType.XOR:
                    switch (a.GetEncType() | b.GetEncType())
                    {
                        case EncryptionType.None | EncryptionType.None:
                        case EncryptionType.None | EncryptionType.XOR:
                        case EncryptionType.XOR | EncryptionType.XOR:
                            Run();
                            break;
                        case EncryptionType.None | EncryptionType.AddMod:
                        case EncryptionType.AddMod | EncryptionType.XOR:
                            if (b.GetEncType() == EncryptionType.AddMod)
                            {
                                new AddModToXOROnKH(party, line, this, new NumericArray(b), new NumericArray(b)).Run();             
                            }
                            else
                            {
                                new AddModToXOROnKH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            }
                            break;
                        case EncryptionType.AddMod | EncryptionType.AddMod:
                            new AddModToXOROnKH(party, line, this, new NumericArray(a, b), new NumericArray(a, b)).Run();
                            break;                      
                        default:
                            throw new ArgumentException();
                    }
                    break;
                case EncryptionType.None:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException();
            }
        }
        protected void TransformEncType(Numeric a)
        {
            System.Diagnostics.Debug.Assert(party.Type == PartyType.KH);
            System.Diagnostics.Debug.Assert(a.GetEncType() != EncryptionType.Any
                && operandEncType != EncryptionType.Any);
            key = new NumericArray(a);
            switch (operandEncType)
            {
                case EncryptionType.AddMod:
                    switch (a.GetEncType())
                    {
                        case EncryptionType.None:
                        case EncryptionType.AddMod:
                            Run();
                            break;
                        case EncryptionType.XOR:
                            new XORToAddModOnKH(party, line, this, key, key).Run();
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    break;
                case EncryptionType.XOR:
                    switch (a.GetEncType())
                    {
                        case EncryptionType.None:
                        case EncryptionType.XOR:
                            Run();
                            break;
                        case EncryptionType.AddMod:
                            new AddModToXOROnKH(party, line, this, new NumericArray(a), new NumericArray(a)).Run();
                            break;
                        default:
                            throw new ArgumentException();
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
        }
        protected void Evaluate(IntermediateCode code)
        {
            //System.Diagnostics.Debug.WriteLine("KH: " + code);
            if (code is ICAssignment)
            {
                ICAssignment ica = (ICAssignment)code;
                // create Operation instance
                ObjectHandle handle = Activator.CreateInstance("JOSPrototype", "JOSPrototype.Runtime.Operation." + ica.op + "OnKH", false, 0, null, new object[] { party, code, program, this }, null, null);
                Operation op = (Operation)handle.Unwrap();
                // execute the operation on EVH
                op.Run();
            }
            else if (code is ICWhile)
            {
                ICWhile icw = (ICWhile)code;
                WhileOnKH op = new WhileOnKH(party, icw, program, this);
                op.Run();
            }
            else
            {
                ICIfElse icie = (ICIfElse)code;
                IfElseOnKH op = new IfElseOnKH(party, icie, program, this);
                op.Run();
            }
        }
        protected void SetResult(EncryptionType encType, params Numeric[] keyRe)
        {
            if (!ReferenceEquals(code, null))
            {
                // called by party or a statement inside while or if-else
                keyRe[0].SetEncType(encType);
                //if (opType == OperationType.LessZero)
                //{
                //    Console.WriteLine("KH: " + keyRe[0].GetUnsignedBigInteger());
                //}
                new ConcealOnKH(party, line, this, new NumericArray(keyRe[0]), code.result, program).Run();
                // program.SetValue(code.result, encRe[0]);
            }
            else
            {
                // as subfunction
                result.SetArray(keyRe);
                Run();
            }
        }
        protected void InvokeCaller()
        {
            if (!ReferenceEquals(caller, null))
            {
                // as a subfunction or a statement inside while or if-else
                caller.Run();
            }
            else
            {
                // called by party
                program.evaluatedIC.AddOrUpdate(code, new Object(), (k, v) => new Object());
            }
        }
    }

    abstract class OperationOnHelper : Operation
    {
        protected OperationOnHelper(Party party, int line, Operation caller, NumericArray result, OperationType opType)
            : base(party, line, caller, result, opType)
        { }

        protected OperationOnHelper(Party party, int line, OperationType opType)
            : base(party, line, null, null, opType)
        { }
        public sealed override void Run()
        {
            step++;
            Task.Factory.StartNew(this.OnHelper);
        }
        protected abstract void OnHelper();
    }
}
