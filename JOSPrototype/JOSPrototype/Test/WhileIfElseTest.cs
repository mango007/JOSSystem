using JOSPrototype.Components;
using JOSPrototype.Frontend;
using JOSPrototype.Runtime;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    class WhileIfElseTest
    {
        private static string retVal = "v";
        private static Numeric NestedWhileTest(double x)
        {
            var code =
            @"
            {
                int x = " + x + @";
                while(x != 10)
                {
                    x = x + 1;
                    while(x != 10)
                    {
                        x = x + 1;
                    }
                }
                return " + retVal + @";
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable[retVal];
        }
        private static Numeric NestedIfElseTest(double x)
        {
            var code =
            @"
            {
                double x = " + x + @"[1,5];
                double v;
                if (x < 5(none))
                {
                    if (x == 1)
                    {
                        v = System.Math.Sin(x) * 1;
                    }
                    else
                    {
                        if (x == 2)
                        {
                            v = System.Math.Sin(x) * 2;
                        }
                        else
                        {
                             if (x == 3)
                            {
                                v = System.Math.Sin(x) * 3;
                            }
                            else
                            {
                                v = System.Math.Sin(x) * 4;
                            }
                        }
                    }
                }
                else
                {
                    if (x == 5)
                    {
                        v = System.Math.Sin(x) * 5;
                    }
                    else
                    {
                        if (x == 6)
                        {
                            v = System.Math.Sin(x) * 6;
                        }
                        else
                        {
                            if (x == 7)
                            {
                                v = System.Math.Sin(x) * 7;
                            }
                            else
                            {
                                if (x == 8)
                                {
                                    v = System.Math.Sin(x) * 8;
                                }
                                else
                                {
                                    v = System.Math.Sin(x) * 9;
                                }
                            }
                        }
                    }
                }
                return " + retVal + @";
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable[retVal];
        }
        private static Numeric WhileInIfElseTest(double x)
        {
            var code =
            @"
            {
                int x = "+ x + @";
                if (x < 5) [false, 0.9] 
                {
                    if (x == 1)
                    {
                        x = x + 1;
                    }
                    else
                    {
                        if (x == 2)[true, 0.2]
                        {
                            x = x + 2;
                        }
                        else
                        {
                            while(x != 7)
                            {
                                x = x + 1;
                            }
                        }
                    }
                }
                else
                {
                    if (x == 5)
                    {
                        x = x + 5;
                    }
                    else
                    {
                        if (x == 6)
                        {
                            x = x + 6;
                        }
                        else
                        {
                            if (x == 7)[true, 0.9]
                            {
                                x = x + 7;
                            }
                            else
                            {
                                if (x == 8)
                                {
                                    x = x + 8;
                                }
                                else
                                {
                                    x = x + 9;
                                }
                            }
                        }
                    }
                }
                return " + retVal + @";
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable[retVal];
        }
        private static Numeric IfElseInWhileTest(double x)
        {
            var code =
            @"
            {
                double x = " + x + @";
                int v;
                while(x != 10)
                {
                    if(x == 0)[true, 0.9]
                    {
                        v = x + 5;
                    }
                    else
                    {
                        if(x == 1)
                        {
                            v = x + 4;
                        }
                        else
                        {
                            if(x == 2)
                            {
                                v = x + 3;
                            }
                            else
                            {
                                v = x + 1;
                            }
                        }
                    }
                }
                return " + retVal + @";
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable[retVal];
        }
        private static double CorrNestedWhileTest(double x)
        {
            while(x != 10)
            {
                x = x + 1;
                while(x != 10)
                {
                    x = x + 1;
                }
            }
            return x;
        }
        private static double CorrNestedIfElseTest(double x)
        {
            if (x < 5)
            {
                if (x == 1)
                {
                    x = System.Math.Sin(x) * 1;
                }
                else
                {
                    if (x == 2)
                    {
                        x = System.Math.Sin(x) * 2;
                    }
                    else
                    {
                        if (x == 3)
                        {
                            x = System.Math.Sin(x) * 3;
                        }
                        else
                        {
                            x = System.Math.Sin(x) * 4;
                        }
                    }
                }
            }
            else
            {
                if (x == 5)
                {
                    x = System.Math.Sin(x) * 5;
                }
                else
                {
                    if (x == 6)
                    {
                        x = System.Math.Sin(x) * 6;
                    }
                    else
                    {
                        if (x == 7)
                        {
                            x = System.Math.Sin(x) * 7;
                        }
                        else
                        {
                            if (x == 8)
                            {
                                x = System.Math.Sin(x) * 8;
                            }
                            else
                            {
                                x = System.Math.Sin(x) * 9;
                            }
                        }
                    }
                }
            }
            return x;
        }
        private static double CorrWhileInIfElseTest(double x)
        {
            if (x < 5)
            {
                if (x == 1)
                {
                    x = x + 1;
                }
                else
                {
                    if (x == 2)
                    {
                        x = x + 2;
                    }
                    else
                    {
                        while (x != 7)
                        {
                            x = x + 1;
                        }
                    }
                }
            }
            else
            {
                if (x == 5)
                {
                    x = x + 5;
                }
                else
                {
                    if (x == 6)
                    {
                        x = x + 6;
                    }
                    else
                    {
                        if (x == 7)
                        {
                            x = x + 7;
                        }
                        else
                        {
                            if (x == 8)
                            {
                                x = x + 8;
                            }
                            else
                            {
                                x = x + 9;
                            }
                        }
                    }
                }
            }
            return x;
        }
        private static double CorrIfElseInWhileTest(double x)
        {
            while(x != 10)
            {
                if(x == 0)
                {
                    x = x + 5;
                }
                else
                {
                    if(x == 1)
                    {
                        x = x + 4;
                    }
                    else
                    {
                        if(x == 2)
                        {
                            x = x + 3;
                        }
                        else
                        {
                            x = x + 1;
                        }
                    }
                }
            }
            return x;
        }

        public static void RunTest()
        {
            Console.WriteLine("choose the test:");
            Console.WriteLine(" 1. nested while loop");
            Console.WriteLine(" 2. nested if-else statement");
            Console.WriteLine(" 3. while Loop in if-else statement");
            Console.WriteLine(" 4. if-else statement in while loop");

            int whichTest = Convert.ToInt32(Console.ReadLine());
            int keyLen = 64;
            int numericBitLen = 40;
            byte scaleBits = 21;
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);
            int attempts = 1;

            Random rnd = new Random(1);
            int corrCount = 0;

            for (int i = 0; i < attempts; i++)
            {
                //int x = (Math.Abs(rnd.Next()) % 10);
                double x = 6;
                //int x = 0;
                double err = 0;
                double corr = 0;
                Numeric res = null;
                switch(whichTest)
                {
                    case 1:
                        corr = CorrNestedWhileTest(x);
                        res = NestedWhileTest(x);
                        break;
                    case 2:
                        corr = CorrNestedIfElseTest(x);
                        res = NestedIfElseTest(x);
                        break;
                    case 3:
                        corr = CorrWhileInIfElseTest(x);
                        res = WhileInIfElseTest(x);
                        break;
                    case 4:
                        corr = CorrIfElseInWhileTest(x);
                        res = IfElseInWhileTest(x);
                        break;
                    default:
                        throw new ArgumentException();
                }
                
                err = Math.Abs(res.GetVal() - corr);
                if (err < 0.1)
                {
                    corrCount++;
                }
                
                Console.WriteLine("x: " + x  + ", corr: " + corr + ", computed: " + res.GetVal());

            }
            Console.WriteLine("accuracy: " + corrCount / attempts);

        }
        public static void Main(string[] args)
        {
            RunTest();
            Console.ReadKey();
        }
    }
}
