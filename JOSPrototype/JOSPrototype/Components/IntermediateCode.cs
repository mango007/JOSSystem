using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Components
{
    [Serializable]
    abstract class IntermediateCode
    {
        public static void ResetCount() { count = 1; }
        private static int count = 1;
        protected IntermediateCode()
        {
            if (!(this is ICSequence))
                index = count++;
            else
                index = -1;
        }
        
        public bool hasBeenOrIsBeingEvaluated = false;
        // for synchronization between different parties
        public int index;
        public Dictionary<IntermediateCode, HashSet<DependencyType>> inEdge = new Dictionary<IntermediateCode, HashSet<DependencyType>>();
        public Dictionary<IntermediateCode, HashSet<DependencyType>> outEdge = new Dictionary<IntermediateCode, HashSet<DependencyType>>();
    }
    [Serializable]
    class ICSequence : IntermediateCode
    {
        public void Add(IntermediateCode code) { icCodes.Add(code); }
        public void AddRange(ICSequence codes) { icCodes.AddRange(codes.GetCodes()); }
        public void AddRange(params IntermediateCode[] codes) { icCodes.AddRange(codes); }
        public List<IntermediateCode> GetCodes() { return icCodes; }
        public IntermediateCode this[int index]
        {
            get { return icCodes[index]; }
            set { icCodes[index] = value; }
        }
        public int Count
        {
            get { return icCodes.Count; }
        }

        private List<IntermediateCode> icCodes = new List<IntermediateCode>();
    }
    [Serializable]
    class ICAssignment : IntermediateCode
    {
        // indices of commands will be incremented automatically
        public ICAssignment(OperationType op, EVariable result, ETerminal operand1, ETerminal operand2)
        {
            this.op = op;
            this.result = result;
            this.operand1 = operand1;
            this.operand2 = operand2;
        }
        // used when translating code at runtime, indices of commands at EVH or KH need to be syncronized.
        // For newly generated commands, we use the index of old command, 
        // which means commands inside if-else statement or while loop cannot be parallelized 
        public ICAssignment(OperationType op, EVariable result, ETerminal operand1, ETerminal operand2, int index)
        {
            this.op = op;
            this.result = result;
            this.operand1 = operand1;
            this.operand2 = operand2;
            this.index = index;
        }
        // used when encrypting the program
        public ICAssignment(ICAssignment ica, ref Numeric  key1, ref Numeric  key2, PartyType party)
        {
            index = ica.index;
            op = ica.op;
            result = ica.result;
            if(ica.operand1 is ENumericLiteral)
            {
                var opr1 = (ENumericLiteral)ica.operand1;
                switch (party)
                {
                    case PartyType.EVH:
                        if(!opr1.needEnc)
                        {
                            var val = new Numeric(opr1.GetValue());
                            val.SetEncType(EncryptionType.None);
                            operand1 = new ENumericLiteral(val);
                            ((ENumericLiteral)operand1).needEnc = false;
                        }
                        else
                        {
                            switch (Config.DefaultEnc)
                            {
                                case EncryptionType.AddMod:
                                    operand1 = new ENumericLiteral(opr1.GetValue() + key1);
                                    break;
                                case EncryptionType.XOR:
                                    operand1 = new ENumericLiteral(opr1.GetValue() ^ key1);
                                    break;
                                default:
                                    operand1 = new ENumericLiteral(opr1.GetValue());
                                    break;
                            }
                        }      
                        break;
                    case PartyType.KH:
                        if(!opr1.needEnc)
                        {
                            var val = new Numeric(opr1.GetValue());
                            val.SetEncType(EncryptionType.None);
                            operand1 = new ENumericLiteral(val);
                            ((ENumericLiteral)operand1).needEnc = false;
                        }
                        else
                        {
                            key1 = Utility.NextUnsignedNumeric(opr1.GetValue().GetScaleBits());
                            operand1 = new ENumericLiteral(key1);
                        }                       
                        break;
                    default:
                        throw new ArgumentException();
                }     
            }
            else
            {
                operand1 = ica.operand1;
            }

            if(ica.operand2 is ENumericLiteral)
            {
                var opr2 = (ENumericLiteral)ica.operand2;
                switch (party)
                {
                    case PartyType.EVH:
                        if(!opr2.needEnc)
                        {
                            var val = new Numeric(opr2.GetValue());
                            val.SetEncType(EncryptionType.None);
                            operand2 = new ENumericLiteral(val);
                            ((ENumericLiteral)operand2).needEnc = false;
                        }
                        else
                        {
                            switch (Config.DefaultEnc)
                            {
                                case EncryptionType.AddMod:
                                    operand2 = new ENumericLiteral(opr2.GetValue() + key2);
                                    break;
                                case EncryptionType.XOR:
                                    operand2 = new ENumericLiteral(opr2.GetValue() ^ key2);
                                    break;
                                default:
                                    operand2 = new ENumericLiteral(opr2.GetValue());
                                    break;
                            }
                        }
                        
                        break;
                    case PartyType.KH:
                        if(!opr2.needEnc)
                        {
                            var val = new Numeric(opr2.GetValue());
                            val.SetEncType(EncryptionType.None);
                            operand2 = new ENumericLiteral(val);
                            ((ENumericLiteral)operand2).needEnc = false;
                        }
                        else
                        {
                            key2 = Utility.NextUnsignedNumeric(opr2.GetValue().GetScaleBits());
                            operand2 = new ENumericLiteral(key2);
                        }                        
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
            {
                operand2 = ica.operand2;
            }
        }
        public ICAssignment() { }
        public OperationType op;
        public EVariable result;
        public ETerminal operand1;
        public ETerminal operand2;
        public override string ToString()
        {
            return "Assignment: index: " + index 
                + ", operation: " + op + ", result: " + result.name
                + ", operand1: " + ((operand1 is EVariable) ? ((EVariable)operand1).name : ((!ReferenceEquals(operand1, null)) ? ((ENumericLiteral)operand1).GetValue().ToString() : null)) 
                + ", operand2: " + ((operand2 is EVariable) ? ((EVariable)operand2).name : ((!ReferenceEquals(operand2, null)) ? ((ENumericLiteral)operand2).GetValue().ToString() : null));
        }
    }
    [Serializable]
    class ICWhile : IntermediateCode
    {
        //// nested while loop
        //public ICWhile(ICWhile icw, List<IntermediateCode> codes)
        //{
        //    index = icw.index;
        //    condition = icw.condition;
        //    this.codes = codes;
        //}
        //public ICWhile() { }
        public ICSequence codes = new ICSequence();
        public ICSequence conditionCodes = new ICSequence();
        public ETerminal condition;
        public const double incFactor = 1.5;
        public const int firstCheck = 5;

        public override string ToString()
        {
            return "While: index: " + index;
        }
    }

    [Serializable]
    class ICIfElse: IntermediateCode
    {
        // if condition == revealCond, reveal it with probability prob
        public Numeric revealCond;
        public Numeric prob;
        public ETerminal condition;
        public ETerminal outerCondition = null;
        public ICSequence conditionCodes = new ICSequence();
        public ICSequence codesIf = new ICSequence();
        public ICSequence codesElse = new ICSequence();
        public override string ToString()
        {
            return "If-Else: index: " + index;
        }
    }
}
