using JOSPrototype.Components;
using JOSPrototype.Frontend;
using JOSPrototype.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    
    class ControlAlgorithmTest
    {
        
        static int[] a = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        static Numeric Test(int v, SubdomainPrivacy sdp, double statdomProb)
        {
            var code =
            @"
            {
                int v = " + v + ((sdp == SubdomainPrivacy.None) ? "" : ((sdp == SubdomainPrivacy.STATDOM) ? (@"[50, 1000, " + statdomProb + @"]") : @"[50, 1000]")) + @";
                double x;
                if(v > 50(none))
                {
                    if(v > 700)
                    {
                        x = " + a[7] + @" * System.Math.Sin(v);
                    }
                    else
                    {
                        if(v > 600)
                        {
                            x = " + a[6] + @" * System.Math.Sin(v);
                        }
                        else
                        {
                            if(v > 500)
                            {
                                x = " + a[5] + @" * System.Math.Sin(v);
                            }
                            else
                            {
                                if(v > 400)
                                {
                                    x = " + a[4] + @" * System.Math.Sin(v);
                                }
                                else
                                {
                                    if (v > 300)
                                    {
                                        x = " + a[3] + @" * System.Math.Sin(v);
                                    }
                                    else
                                    {
                                        if (v > 200)
                                        {
                                            x = " + a[2] + @" * System.Math.Sin(v);
                                        }
                                        else
                                        {
                                            x = " + a[1] + @" * System.Math.Sin(v);
                                        }
                                    }
                                }
                            }
                        }
                    }                   
                }
                else
                {
                    x = " + a[0] + @" * System.Math.Sin(v);
                }
                return x;
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable["x"];
        }
        static double CorrTest(int v)
        {
            double x;
            if(v > 50)
            {
                if (v > 700)
                {
                    x = a[7] * System.Math.Sin(v);
                }
                else
                {
                    if (v > 600)
                    {
                        x = a[6] * System.Math.Sin(v);
                    }
                    else
                    {
                        if (v > 500)
                        {
                            x = a[5] * System.Math.Sin(v);
                        }
                        else
                        {
                            if (v > 400)
                            {
                                x = a[4] * System.Math.Sin(v);
                            }
                            else
                            {
                                if (v > 300)
                                {
                                    x = a[3] * System.Math.Sin(v);
                                }
                                else
                                {
                                    if (v > 200)
                                    {
                                        x = a[2] * System.Math.Sin(v);
                                    }
                                    else
                                    {
                                        x = a[1] * System.Math.Sin(v);
                                    }
                                }
                            }
                        }
                    }
                }
                
            }
            else
            {
                 x = a[0] * Math.Sin(v);
            }
            return x;
        }
        
        static void RunTest()
        {
            string path = @"figure1.txt";
            string logPath = @"control_algorithm_log1.txt";
            using (StreamWriter file = new StreamWriter(path)) { }
            using (StreamWriter file = new StreamWriter(logPath)) { }
            int keyLen = 64;
            int numericBitLen = 60;
            byte scaleBits = 21;
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);
            int attempts = 10;
            double[] abnormalRate = new double[] {0, 100 };
            long[] timeNoneAvg = new long[2], timeDOMAvg = new long[2], timeSTATDOMAvg = new long[2];
            for(int rateIter = 0; rateIter < 2; ++rateIter)
            {
                var rate = abnormalRate[rateIter];
                var statdomProb = 0.95;
                Random rnd = new Random(1);
                int corrCountNone = 0, corrCountSTATDOM = 0, corrCountDOM = 0;
                long timeNone = 0, timeSTATDOM = 0, timeDOM = 0;
                Stopwatch timePerExe;
                double computed;
                //OutputResult(path, "Normal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
                WriteToFile(logPath, "Abnormal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits + ", STATDOM probability: " + statdomProb);
                //OutputResult(path, "");
                WriteToFile(logPath, "");
                for (int attmpIter = 0; attmpIter < attempts; attmpIter++)
                {
                    //int v = 98;
                    int v;
                    // abnormal data
                    if (rnd.NextDouble() < (rate / 100))
                    {
                        v = rnd.Next(50, 1000);
                    }
                    // normal data
                    else
                    {
                        v = Math.Abs(rnd.Next()) % 50;
                    }

                    var corr = CorrTest(v);
                    WriteToFile(logPath, "v = " + v + ", correctResult =  " + corr);

                    if(rateIter == 0)
                    {
                        Utility.ResetSeed();
                        timePerExe = Stopwatch.StartNew();
                        computed = Test(v, SubdomainPrivacy.None, statdomProb).GetVal();
                        timePerExe.Stop();
                        timeNone += timePerExe.ElapsedMilliseconds;
                        corrCountNone += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                        WriteToFile(logPath, "    None:   computedResult = " + computed);
                    }
                    
                    //Utility.ResetSeed();
                    //timePerExe = Stopwatch.StartNew();
                    //computed = Test(v, SubdomainPrivacy.DOM, statdomProb).GetVal();
                    //timePerExe.Stop();
                    //timeDOM += timePerExe.ElapsedMilliseconds;
                    //corrCountDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                    //WriteToFile(logPath, "    DOM:    computedResult = " + computed);

                    Utility.ResetSeed();
                    timePerExe = Stopwatch.StartNew();
                    computed = Test(v, SubdomainPrivacy.STATDOM, statdomProb).GetVal();
                    timePerExe.Stop();
                    timeSTATDOM += timePerExe.ElapsedMilliseconds;
                    corrCountSTATDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                    WriteToFile(logPath, "    STATDOM:computedResult = " + computed);
                    WriteToFile(logPath, "");
                }
                if (rateIter == 0)
                {
                    timeNoneAvg[rateIter] = timeNone / attempts;
                }
                else
                {
                    timeNoneAvg[rateIter] = timeNoneAvg[0];
                } 
                timeDOMAvg[rateIter] = timeDOM / attempts;
                timeSTATDOMAvg[rateIter] = timeSTATDOM / attempts;
                WriteToFile(logPath, "");
                WriteToFile(logPath, "None:    accuracy: " + corrCountNone / attempts + ", time: " + timeNone / attempts + " ms");
                //WriteToFile(logPath, "DOM:     accuracy: " + corrCountDOM / attempts + ", average time: " + timeDOM / attempts + " ms");
                WriteToFile(logPath, "STATDOM: accuracy: " + corrCountSTATDOM / attempts + ", average time: " + timeSTATDOM / attempts + " ms");
                WriteToFile(logPath, "");
            }
            abnormalRate = new double[] { 0, 0.25, 0.5, 1, 2, 4, 8, 16, 32, 64, 100};
            OutputResult(path, "AbnormalRate None  DOM  STATDOM");
            foreach(var rate in abnormalRate)
            {
                double
                    timeNone = ((1 - rate / 100) * timeNoneAvg[0] + rate / 100 * timeNoneAvg[1]),
                    timeDOM = ((1 - rate / 100) * timeDOMAvg[0] + rate / 100 * timeDOMAvg[1]),
                    timeSTATDOM = ((1 - rate / 100) * timeSTATDOMAvg[0] + rate / 100 * timeSTATDOMAvg[1]);
                //OutputResult(path, "Abnormal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
                //OutputResult(path, "");
                OutputResult(path, rate + " " + 1 + " " + timeDOM / timeNone + " " + timeSTATDOM / timeNone);
                //OutputResult(path, "None:    average time: " + ((1 - rate / 100) * timeNoneAvg[0] + rate / 100 * timeNoneAvg[1]) + " ms");
                //OutputResult(path, "DOM:     average time: " + ((1 - rate / 100) * timeDOMAvg[0] + rate / 100 * timeDOMAvg[1]) + " ms");
                //OutputResult(path, "STATDOM: average time: " + ((1 - rate / 100) * timeSTATDOMAvg[0] + rate / 100 * timeSTATDOMAvg[1]) + " ms");
                //OutputResult(path, "");
            }
        }
        
        static void RunTest2()
        {
            string path = @"figure2.txt";
            string logPath = @"control_algorithm_log2.txt";
            using (StreamWriter file = new StreamWriter(path)) { }
            using (StreamWriter file = new StreamWriter(logPath)) { }
            int keyLen = 64;
            int numericBitLen = 60;
            byte scaleBits = 21;
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);
            int attempts = 10;
            double[] abnormalRate = new double[] { 0, 100 };
            long[] timeNoneAvg = new long[2], timeSTATDOMAvg = new long[2];
            // first, measure the average running time for 10 normal inputs(t0) and 10 abnormal inputs(t1) with p = 1(always reveal v if v is normal)
            for (int rateIter = 0; rateIter < 2; ++rateIter)
            {
                var rate = abnormalRate[rateIter];
                Random rnd = new Random(1);
                int corrCountNone = 0, corrCountSTATDOM = 0;
                long timeNone = 0, timeSTATDOM = 0;
                Stopwatch timePerExe;
                double computed;
                //OutputResult(path, "Normal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
                WriteToFile(logPath, "Abnormal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
                //OutputResult(path, "");
                WriteToFile(logPath, "");
                for (int attmpIter = 0; attmpIter < attempts; attmpIter++)
                {
                    int v;
                    // abnormal data
                    if (rnd.NextDouble() < (rate / 100))
                    {
                        v = rnd.Next(50, 1000);
                    }
                    // normal data
                    else
                    {
                        v = Math.Abs(rnd.Next()) % 50;
                    }

                    var corr = CorrTest(v);
                    WriteToFile(logPath, "v = " + v + ", correctResult =  " + corr);

                    if (rateIter == 0)
                    {
                        Utility.ResetSeed();
                        timePerExe = Stopwatch.StartNew();
                        computed = Test(v, SubdomainPrivacy.None, 1).GetVal();
                        timePerExe.Stop();
                        timeNone += timePerExe.ElapsedMilliseconds;
                        corrCountNone += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                        WriteToFile(logPath, "    None:   computedResult = " + computed);
                    }

                    Utility.ResetSeed();
                    timePerExe = Stopwatch.StartNew();
                    // always reveal v if v is normal
                    computed = Test(v, SubdomainPrivacy.STATDOM, 1).GetVal();
                    timePerExe.Stop();
                    timeSTATDOM += timePerExe.ElapsedMilliseconds;
                    corrCountSTATDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                    WriteToFile(logPath, "    STATDOM:computedResult = " + computed);
                    WriteToFile(logPath, "");
                }               
                if(rateIter == 0)
                {
                    timeNoneAvg[0] = timeNone / attempts;
                }
                else
                {
                    timeNoneAvg[1] = timeNoneAvg[0];
                }
                timeSTATDOMAvg[rateIter] = timeSTATDOM / attempts;
                WriteToFile(logPath, "");
                WriteToFile(logPath, "None:    accuracy: " + corrCountNone / attempts + ", average time: " + timeNone / attempts + " ms");
                WriteToFile(logPath, "STATDOM: accuracy: " + corrCountSTATDOM / attempts + ", average time: " + timeSTATDOM / attempts + " ms");
                WriteToFile(logPath, "");
            }
            // 99% of the inputs are normal, 
            // reveal1Time is the average time when p = 1
            // reveal0Time is the average time when p = 0, essentially  reveal0Time == t1
            var reveal1Time = 0.99 * timeSTATDOMAvg[0] + 0.01 * timeSTATDOMAvg[1];
            var reveal0Time = (double)timeSTATDOMAvg[1];
            var revealProb = new double[] { 0, 0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1 };
            OutputResult(path, "p None STATDOM");
            foreach (var prob in revealProb)
            {
                var time = (prob * reveal1Time + (1 - prob) * reveal0Time);
                // normalize by computing time for 0%
                OutputResult(path, prob + " 1 " + time / timeNoneAvg[0]);
            }
        }
        static void OutputResult(string path, string output)
        {
            Console.WriteLine(output);
            using (StreamWriter file = File.AppendText(path))
            {
                file.WriteLine(output);
            }
        }
        static void WriteToFile(string path, string output)
        {
            using (StreamWriter file = File.AppendText(path))
            {
                file.WriteLine(output);
            }
        }
        public static void Main(string[] args)
        {
            RunTest();
            //RunTest2();
            Console.ReadKey();
        }
    }
}
