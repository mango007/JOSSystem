//using JOSPrototype.Components;
//using JOSPrototype.Runtime;
//using JOSPrototype.Runtime.Network;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace JOSPrototype.Test
//{
//    class EncryptCodeTest
//    {
//        public static Numeric p2()
//        {
//            Program program = new Program();

//            Expression resvar = new EVariable(program, "result");
//          //  Expression CurrentValue = new EVariable(program, "CurrentValue");
//         //   Expression calc_month = new EVariable(program, "calc_month");
//          //  Expression Invalid = new EVariable(program, "Invalid");
//            Expression mCounter = new EVariable(program, "mCounter");            
//            //program.statementsList.Add(new SAssignment(Invalid, new ENumericLiteral(1, 0)));
//            program.AddStatement(new SAssignment(mCounter, new ENumericLiteral(1, 0)));
//            //program.statementsList.Add(new SAssignment(CurrentValue, new ENumericLiteral(1, 0)));
//           // program.statementsList.Add(new SAssignment(calc_month, CurrentValue));
//            //Expression resvar = new EVariable(program, "result");            
//            program.AddStatement(new SAssignment(resvar, new ENumericLiteral(1, 0)));
//            Expression expr0 = new EBinaryOperation(resvar, new ENumericLiteral(1, 0), OperationType.Addition);
//            Expression expr1 = new EBinaryOperation(resvar, mCounter, OperationType.Addition);
//            //program.statementsList.Add(new SAssignment(resvar, expr));

//            Expression expr2 = new EBinaryOperation(resvar, mCounter, OperationType.Multiplication);

//            Expression expr3 = new EBinaryOperation(expr0, expr1, OperationType.Multiplication);
//            Expression expr4 = new EBinaryOperation(expr3, expr2, OperationType.Addition);
//            Expression expr5 = new EBinaryOperation(expr4, expr0, OperationType.Multiplication);
//            Expression expr6 = new EBinaryOperation(resvar, mCounter, OperationType.Multiplication);
//            Expression expr7 = new EBinaryOperation(resvar, resvar, OperationType.Multiplication);
//            Expression expr8 = new EBinaryOperation(expr6, expr7, OperationType.Addition);
//            Expression expr9 = new EBinaryOperation(expr5, expr8, OperationType.Addition);
//            Expression expr10 = new EBinaryOperation(expr9, expr8, OperationType.Multiplication);
//            program.AddStatement(new SAssignment(resvar, expr10));



//            //Expression cond1 = new EUnaryOperation(new EBinaryOperation(Invalid, new ENumericLiteral("-1", 0), OperationType.Addition), OperationType.EqualZero);
//            //Statement stat1 = new SAssignment(CurrentValue, new ENumericLiteral("8", 0));
//            //program.statementsList.Add(new SIfElse(cond1, stat1, null));
//            //program.statementsList.Add(new SAssignment(mCounter, new EBinaryOperation(mCounter, new ENumericLiteral(1, 0), OperationType.Addition)));
//            //Expression greaterThan1800 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("1800", 0), mCounter, OperationType.Substraction), OperationType.LessZero);
//            //Expression equalTo1800 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("1800", 0), mCounter, OperationType.Substraction), OperationType.EqualZero);
//            //Expression greaterThanOrEqualTo1800 = new EBinaryOperation(greaterThan1800, equalTo1800, OperationType.OR);
//            //List<Statement> statList = new List<Statement>();
//            //statList.Add(new SAssignment(mCounter, new ENumericLiteral("0", 0)));
//            //Expression equalTo12 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("12", 0), CurrentValue, OperationType.Substraction), OperationType.EqualZero);
//            //Statement innerIfElse = new SIfElse(
//            //    equalTo12,
//            //    new SAssignment(CurrentValue, new ENumericLiteral("1", 0)),
//            //    new SAssignment(CurrentValue, new EBinaryOperation(calc_month, new ENumericLiteral("1", 0), OperationType.Addition)));
//            //statList.Add(innerIfElse);
//            //program.statementsList.Add(new SIfElse(greaterThanOrEqualTo1800, new SSequence(statList.ToArray()), null));
//            //program.statementsList.Add(new SReturn(new Expression[] { CurrentValue }));



//            //Expression dummyOp = new EUnaryOperation(resvar, OperationType.NOT);
//            //Expression mul1 = new EBinaryOperation(ENumericLiteral.One, expr, OperationType.Multiplication);
//            //Expression mul2 = new EBinaryOperation(ENumericLiteral.Zero, dummyOp, OperationType.Multiplication);
//            //program.statementsList.Add(new SAssignment(resvar, new EBinaryOperation(mul1, mul2, OperationType.Addition)));

//            ////program.statementsList.Add(new SAssignment(resvar, mul1));

//            program.AddStatement(new SReturn(new Expression[] { resvar }));       

//            Program pro = runPro(program);
//            return (Numeric)pro.vTable["result"]; ;
//        }

      

//        public static Numeric p1_getNewTemperatureValue(int CurrentValueVal, int InvalidVal, int mCounterVal)
//        {
//            Program program = new Program();
//            Expression calc_month = new EVariable(program, "calc_month");
//            Expression Invalid = new EVariable(program, "Invalid");
//            Expression CurrentValue = new EVariable(program, "CurrentValue");
//            Expression mCounter = new EVariable(program, "mCounter");
//            program.AddStatement(new SAssignment(CurrentValue, new ENumericLiteral(CurrentValueVal, 0)));
//            program.AddStatement(new SAssignment(calc_month, CurrentValue));
//            program.AddStatement(new SAssignment(Invalid, new ENumericLiteral(InvalidVal, 0)));
//            program.AddStatement(new SAssignment(mCounter, new ENumericLiteral(mCounterVal, 0)));

//            Expression cond1 = new EUnaryOperation(new EBinaryOperation(Invalid, new ENumericLiteral("-1", 0), OperationType.Addition), OperationType.EqualZero);
//            Statement stat1 = new SAssignment(CurrentValue, new ENumericLiteral("8", 0));
//            program.AddStatement(new SIfElse(cond1, stat1, null));
//            program.AddStatement(new SAssignment(mCounter, new EBinaryOperation(mCounter, new ENumericLiteral(1, 0), OperationType.Addition)));
//            Expression greaterThan1800 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("1800", 0), mCounter, OperationType.Substraction), OperationType.LessZero);
//            Expression equalTo1800 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("1800", 0), mCounter, OperationType.Substraction), OperationType.EqualZero);
//            Expression greaterThanOrEqualTo1800 = new EBinaryOperation(greaterThan1800, equalTo1800, OperationType.OR);
//            List<Statement> statList = new List<Statement>();
//            statList.Add(new SAssignment(mCounter, new ENumericLiteral("0", 0)));
//            Expression equalTo12 = new EUnaryOperation(new EBinaryOperation(new ENumericLiteral("12", 0), CurrentValue, OperationType.Substraction), OperationType.EqualZero);
//            Statement innerIfElse = new SIfElse(
//                equalTo12,
//                new SAssignment(CurrentValue, new ENumericLiteral("1", 0)),
//                new SAssignment(CurrentValue, new EBinaryOperation(calc_month, new ENumericLiteral("1", 0), OperationType.Addition)));
//            statList.Add(innerIfElse);
//            program.AddStatement(new SIfElse(greaterThanOrEqualTo1800, new SSequence(statList.ToArray()), null));
//            program.AddStatement(new SReturn(new Expression[] { CurrentValue }));

//            runPro(program);
//            return (Numeric)program.vTable["CurrentValue"]; ;
//        }

//        public static Program runPro(Program program)
//        {
//            program.Translate();           
//            CodeEncryption codeenc = new CodeEncryption();
//            Program encPro = codeenc.encrypt(program);
//            encPro.Translate();
//            var programEnc = encPro.EncProgram();
//            //var programEnc = program.EncProgram();

//            List<Party> parties = new List<Party>();

//            Client client = new Client(encPro);
//            parties.Add(client);
//            EVH evh = new EVH(programEnc[0]);
//            parties.Add(evh);
//            KH kh = new KH(programEnc[1]);
//            parties.Add(kh);
//            Helper helper = new Helper();
//            parties.Add(helper);

//            Network.NetworkInitialize(parties);

//            Thread thread = new Thread(() => evh.RunParty());
//            thread.Name = "EVH";
//            thread.Start();

//            thread = new Thread(() => kh.RunParty());
//            thread.Name = "KH";
//            thread.Start();

//            client.RunParty();
//            return encPro;
//        }

//        private static int CorrGetNewTemperatureValue(int CurrentValue, int Invalid, int mCounter)
//        {
//            int calc_month = CurrentValue;
//            if (Invalid == 1)
//                CurrentValue = 8;

//            mCounter++;
//            if (mCounter >= 1800)    //One day is 60 seconds -> 1 month = 1800 iterations..
//            {
//                mCounter = 0;
//                if (CurrentValue != 12)
//                    CurrentValue = calc_month + 1;
//                else
//                    CurrentValue = 1;
//            }
//            return CurrentValue;
//        }

//        public static void RunTest()
//        {
//            int keyLen = 32;
//            int numericBitLen = 16;
//            int fractionBitLen = 2;
//            Config.SetGlobalParameters(keyLen, numericBitLen, fractionBitLen, false);
//            int attemps = 3;
//            Random rnd = new Random(123);
//            int corrCount = 0;
//            for (int i = 0; i < attemps; i++)
//            {
//                /*int CurrentValue = Math.Abs(rnd.Next()) % 100;
//                int Invalid = 0;// rnd.Next() & 1;
//                int mCounterVal = Math.Abs(rnd.Next()) % 3600;
//                var corr = CorrGetNewTemperatureValue(CurrentValue, Invalid, mCounterVal);
//                var res = p1_getNewTemperatureValue(CurrentValue, Invalid, mCounterVal);*/

//                var corr = 2;
//                var res = p2();

//                if ((int)res.GetVal() == corr)
//                { corrCount++; }
//                Console.WriteLine("Corr: " + corr + " Computed:" + (int)res.GetVal());
//            }
//            Console.WriteLine("Accuracy: " + corrCount * 1.0 / attemps);
//        }

//        public static void Main(string[] args)
//        {
//            Console.WriteLine(" Encrypting Code:");
//            RunTest();
//            Console.ReadKey();
//        }
//    }
//}
