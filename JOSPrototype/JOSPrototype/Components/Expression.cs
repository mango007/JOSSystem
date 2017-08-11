using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Components
{
    abstract class Expression
    {
    }

    abstract class ETerminal : Expression
    {
        public static bool IsEqual(ETerminal term1, ETerminal term2)
        {
            //if(term1 is ETemporaryVariable && term2 is ETemporaryVariable)
            //{
            //    return Object.ReferenceEquals(term1, term2);
            //}
            //else 
            if(term1 is EVariable && term2 is EVariable)
            {
                return ((EVariable)term1).name == ((EVariable)term2).name;
            }
            else
            {
                return false;
            }
        }
    }

    abstract class ELiteral<T> : ETerminal
    {
        public abstract T GetValue();
        //public static U GetValue<U>(ELiteral<U> literal)
        //{
        //    return literal.GetValue();
        //}
    }

    [Serializable]
    class ENumericLiteral : ELiteral<Numeric>
    {
        public bool needEnc = true;
        private Numeric value;      
        public ENumericLiteral(Numeric value)
        {
            this.value = value;
        }
        public ENumericLiteral(int value, byte scaleBits)
        {
            this.value = new Numeric(value, scaleBits);
        }
        public ENumericLiteral(string value, byte scaleBits)
        {
            this.value = new Numeric (value, scaleBits);
        }
        public ENumericLiteral(BigInteger value, byte scaleBits)
        {
            this.value = new Numeric(value, scaleBits);
        }

        public override Numeric  GetValue()
        {
            return value;
        }
    }

    //class EBigBooleanLiteral : ELiteral<FixedLengthBigInteger>
    //{
    //    private FixedLengthBigInteger value;
    //    public EBigBooleanLiteral(bool value)
    //    {
    //        if(value)
    //        {
    //            this.value = new FixedLengthBigInteger(1);
    //        }
    //        else
    //        {
    //            this.value = new FixedLengthBigInteger(0);
    //        }

    //    }
    //    public override FixedLengthBigInteger GetValue()
    //    {
    //        return value;
    //    }
    //}

    //class EIntegerLiteral: ELiteral<int>
    //{
    //    private int value;
    //    public EIntegerLiteral(int value)
    //    {
    //        this.value = value;
    //    }
    //    public override int GetValue()
    //    {
    //        return value;
    //    }
    //}

    //[Serializable]
    //class EArray : ETerminal
    //{
    //    public string name;
    //    public ETerminal index;
    //    public EArray(string name, ETerminal index)
    //    {
    //        this.name = name;
    //        this.index = index;
    //    }
    //}

    [Serializable]
    class EVariable : ETerminal
    {
        public string name;
        public bool isTemporary;
        // if the value is not in [leftBoundary, rightBoundary], reveal it with probability prob
        public Numeric leftBoundary;
        public Numeric rightBoundary;
        public Numeric prob;
        public EVariable(Program program, string name)
        {
            program.vTable.AddOrUpdate(name, new Object(), (k, v) => new Object());
            this.name = name;
            isTemporary = false;
        }

        public EVariable(Program program)
        {
            name = "$" + tempVarCount++;
            program.vTable.AddOrUpdate(name, new Object(), (k, v) => new Object());
            isTemporary = true;
        }
        public static void ResetCount() { tempVarCount = 0; }
        private static int tempVarCount = 0;
    }

    //[Serializable]
    //class EProgramDefinedVariable : EVariable
    //{     
    //    public string name;
    //    public EProgramDefinedVariable(Program program, string name)
    //    {
    //        this.program = program;
    //        this.name = name;
    //    }

    //    public override Object GetObject()
    //    {
    //        return program.vTable[name];
    //    }

    //    public override void AddOrUpdateObject(Object obj)
    //    {
    //        program.vTable.AddOrUpdate(name, obj, (k, v) => obj);
    //    }
    //    private Program program;
    //    private static int tempVarCount = 0;
    //}

    //class ETemporaryVariable : EVariable
    //{
    //    private Object tempVar;

    //    public override void AddOrUpdateObject(object obj)
    //    {
    //        tempVar = obj;
    //    }

    //    public override Object GetObject()
    //    {
    //        return tempVar;
    //    }
    //}

    class EBinaryOperation : Expression
    {
        public EBinaryOperation(Expression operand1, Expression operand2, OperationType operation)
        {
            Operand1 = operand1;
            Operand2 = operand2;
            Operation = operation;
        }
        public Expression Operand1 { get; private set; }
        public Expression Operand2 { get; private set; }
        public OperationType Operation { get; private set; }
    }

    class EUnaryOperation : Expression
    {
        public EUnaryOperation(Expression operand, OperationType operation)
        {
            Operand = operand;
            Operation = operation;
        }
        public Expression Operand { get; private set; }
        public OperationType Operation { get; private set; }
    }


}
