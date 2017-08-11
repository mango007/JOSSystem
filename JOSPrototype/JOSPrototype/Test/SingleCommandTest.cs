using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Optimization;
using JOSPrototype.Runtime;
using JOSPrototype.Runtime.Network;
using System.Numerics;
using System.Threading;
using JOSPrototype.Runtime.Operation;
using System.Diagnostics;
using System.IO;

namespace JOSPrototype.Test
{
    class SingleCommandTest
    {
        public double Accuracy { get; private set; }

        private long RunTest(int commandNum, OperationType op, int keyLen, int numericBitLen, byte scaleBits, bool isOptimized, bool isOp1Enc, bool isOp2Enc)
        {
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, isOptimized);
            Program program = new Program();
            Numeric [] expectedResult = new Numeric [commandNum];
            Expression[] ret = new Expression[commandNum];
            Numeric[] operand1 = new Numeric[commandNum], operand2 = new Numeric[commandNum];


            for (int i = 0; i < commandNum; ++i)
            {
                Expression variable = new EVariable(program, "a" + i);
                if(op == OperationType.Inverse)
                {
                    //operand1[i] = new Numeric(1000, 0);
                    operand1[i] = Utility.NextUnsignedNumeric(0, 14);
                    operand2[i] = Utility.NextUnsignedNumeric(0, 14);
                }
                else if (op == OperationType.Multiplication)
                {
                    //operand1[i] = new Numeric(30 << 21, 21);
                    operand1[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen / 2);
                    operand2[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen / 2);
                }
                else if(op == OperationType.Sin)
                {
                    operand1[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen);
                    operand2[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen);
                }
                else if (op == OperationType.Switch)
                {
                    operand1[i] = i % 12;
                    operand2[i] = 1;
                }
                else if(op == OperationType.NOT)
                {
                    operand1[i] = Utility.NextUnsignedNumeric(0, 1);
                    operand2[i] = 1;
                }
                else
                {
                    //operand1[i] = new Numeric(30 << 21, 21);
                    operand1[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen);
                    operand2[i] = Utility.NextSignedNumeric(scaleBits, numericBitLen);
                    //operand1[i] = new Numeric(-1350, fractionBitLength);
                }
                ENumericLiteral op1 = new ENumericLiteral(operand1[i]), op2 = new ENumericLiteral(operand2[i]);
                op1.needEnc = isOp1Enc;
                op2.needEnc = isOp2Enc;
                Expression expression = new EBinaryOperation(op1, op2, op);
                Statement statement = new SAssignment(variable, expression);
                program.AddStatement(statement);
                ret[i] = variable;
                switch (op)
                {
                    case OperationType.Addition:
                        expectedResult[i] = operand1[i] + operand2[i];
                        break;
                    case OperationType.Substraction:
                        expectedResult[i] = operand1[i] - operand2[i];
                        break;
                    case OperationType.AND:
                        expectedResult[i] = operand1[i] & operand2[i];
                        break;
                    case OperationType.XOR:
                        expectedResult[i] = operand1[i] ^ operand2[i];
                        break;
                    case OperationType.Multiplication:
                        expectedResult[i] = operand1[i] * operand2[i];
                        break;
                    case OperationType.OR:
                        expectedResult[i] = operand1[i] | operand2[i];
                        break;
                    case OperationType.Sin:
                        expectedResult[i] = new Numeric((BigInteger)(Math.Sin(operand1[i].GetVal()) * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        break;
                    case OperationType.EqualZero:
                        expectedResult[i] = (operand1[i].GetVal() == 0) ? new Numeric (1, 0) : new Numeric (0, 0);
                        break;
                    //case OperationType.FastEqualZero:
                    //    expectedResult[i] = ((operand1[i].ModPow(FastEqualZero.GetKeySize())).GetVal() == 0) ? new Numeric(1, 0) : new Numeric(0, 0);
                    //    break;
                    case OperationType.LessZero:
                    case OperationType.MultiLessZero:
                        expectedResult[i] = (operand1[i].GetVal() >= 0) ? new Numeric (0, 0) : new Numeric (1, 0);
                        break;
                    case OperationType.None:
                        expectedResult[i] = operand1[i];
                        break;
                    case OperationType.HammingDistance:
                        break;
                    case OperationType.Inverse:
                        expectedResult[i] = operand1[i];
                        break;
                    case OperationType.IndexMSB:
                    
                    case OperationType.Switch:
                        expectedResult[i] = operand1[i];
                        break;
                    case OperationType.NOT:
                        expectedResult[i] = operand1[i] ^ new Numeric(1, 0);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            program.AddStatement(new SReturn(ret));
            program.Translate();

            var programEnc = program.EncProgram();

            if(op == OperationType.HammingDistance)
            {
                for (int i = 0; i < commandNum; ++i)
                {
                    expectedResult[i] = ((((ENumericLiteral)((ICAssignment)programEnc[0].icList[i]).operand1).GetValue()) ^ (((ENumericLiteral)((ICAssignment)programEnc[1].icList[i]).operand1).GetValue())).SumBits(Config.KeyBits);
                }
            }

            var totalTime = Party.RunAllParties(program, programEnc);

            int corrCount = 0;
            for(int i = 0; i < commandNum; ++i)
            {
                var computedRes = ((Numeric )program.vTable["a" + i]);
                var expRes = expectedResult[i];

                if(op == OperationType.HammingDistance)
                {
                    if(computedRes.GetVal() == expRes.GetVal() || computedRes.GetVal() + (int)Math.Pow(2, Math.Ceiling(Math.Log(Config.KeyBits + 1, 2))) == expRes.GetVal())
                    {
                        corrCount++;
                    }
                }
                else if(op == OperationType.Inverse)
                {
                    if(Math.Abs(expRes.GetVal() - 1 / computedRes.GetVal()) < 10)
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + 1 / expRes.GetVal());
                        
                        corrCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + 1 / expRes.GetVal());
                        Console.WriteLine("corr: " + 1 / expRes.GetVal() + ", computed: " + computedRes.GetVal());
                    }
                }
                else if(op == OperationType.NOT)
                {
                    if(computedRes.GetUnsignedBigInteger() == expRes.GetUnsignedBigInteger())
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + expectedResult[i]);
                        corrCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + expectedResult[i]);
                        Console.WriteLine("corr: " + expRes.GetVal() + ", computed: " + computedRes.GetVal());
                    }
                }
                else{
                    if (Math.Abs(computedRes.GetVal() - expRes.GetVal()) < 0.1)
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + expectedResult[i]);
                        corrCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(i + ":");
                        System.Diagnostics.Debug.WriteLine("operand1: " + operand1[i]);
                        System.Diagnostics.Debug.WriteLine("operand2: " + operand2[i]);
                        System.Diagnostics.Debug.WriteLine("computed: " + program.vTable["a" + i]);
                        System.Diagnostics.Debug.WriteLine("expected: " + expectedResult[i]);
                        Console.WriteLine("corr: " + expRes.GetVal() + ", computed: " + computedRes.GetVal());
                    }
                }
  
            }
            Accuracy = (double)corrCount / commandNum;
            return totalTime;
        }
        public static void Main(string[] args)
        {
            // create a new file
            string path = @"single_command_result.txt";
            using (StreamWriter file = new StreamWriter(path)) { }
            var test = new SingleCommandTest();
            List<OperationType> opToTest = new List<OperationType>
            {
                //OperationType.Addition, OperationType.Substraction, OperationType.Multiplication, OperationType.Sin,
                //OperationType.XOR, OperationType.AND, OperationType.OR, OperationType.NOT,
                //OperationType.EqualZero, 
                OperationType.LessZero,OperationType.Inverse
            };
            List<OperationType> binaryOp = new List<OperationType>
            {
                OperationType.Addition, OperationType.Substraction, OperationType.Multiplication,
                OperationType.XOR, OperationType.AND, OperationType.OR
            };
            List<OperationType> booleanOp = new List<OperationType>
            {
                OperationType.XOR, OperationType.AND, OperationType.OR, OperationType.NOT
            };
            int commandNum = 10;
            int keyBits = 64;
            int numericBits = 60;
            byte scaleBits = 21;
            string output;
            double avgTime; 
            foreach (var op in opToTest)
            {
                if (op == OperationType.Sin)
                {
                    keyBits = 64;
                    numericBits = 60;
                    scaleBits = 21;
                }
                else
                {
                    keyBits = 128;
                    numericBits = 80;
                    scaleBits = 21;
                }
                if(op == OperationType.Inverse)
                {
                    commandNum = 10;
                }
                //AddMod to XOR is too slow
                if (booleanOp.Contains(op))
                {
                    Config.DefaultEnc = EncryptionType.XOR;
                }
                else
                {
                    Config.DefaultEnc = EncryptionType.AddMod;
                }
                Console.WriteLine(op +  ": #Command:" + commandNum + ", Key Length: " + keyBits + ", Numeric Length: " + numericBits + ", Fraction Length: " + scaleBits);
                Console.WriteLine("    Optimized:");
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(op + ": #Command:" + commandNum + ", Key Length: " + keyBits + ", Numeric Length: " + numericBits + ", Fraction Length: " + scaleBits);
                    sw.WriteLine("    Optimized:");
                }
                // optimized 
                // all operands are encrypted
                avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, true, true, true);
                output = "        all encrpted:         accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                Console.WriteLine(output);
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(output);
                }
                // all operands are not encrypted
                avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, true, false, false);
                output = "        all not encrpted:     accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                Console.WriteLine(output);
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(output);
                }
                if(binaryOp.Contains(op))
                {
                    avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, true, true, false);
                    output = "        one operand encrpted: accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                    Console.WriteLine(output);
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(output);
                    }
                }
                // not optimized
                Console.WriteLine("    Not Optimized:");
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("    Not Optimized:");
                }
                // all operands are encrypted
                avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, false, true, true);
                output = "        all encrpted:         accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                Console.WriteLine(output);
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(output);
                }
                // all operands are not encrypted
                avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, false, false, false);
                output = "        all not encrpted:     accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                
                Console.WriteLine(output);
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(output);
                }
                if (binaryOp.Contains(op))
                {
                    avgTime = test.RunTest(commandNum, op, keyBits, numericBits, scaleBits, false, true, false);
                    output = "        one operand encrpted: accuracy: " + test.Accuracy + ", Time: " + avgTime + " ms";
                    Console.WriteLine(output);
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(output);
                    }
                }
            }
            Console.ReadKey();
        }
    }
}
