using JOSPrototype.Components;
using JOSPrototype.Frontend;
using JOSPrototype.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    class HealthData
    {
        static int[] a = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        static Numeric Test(BigInteger val, BigInteger[] patterns, int patternLen, SubdomainPrivacy sdp, double statdomProb, bool isAbnormal)
        {
            System.Diagnostics.Debug.Assert(patternLen <= Config.NumericBits);
            BigInteger mask = Numeric.oneMaskMap[patternLen];
            var code =
            @"
            {
                int v = " + val + ((sdp == SubdomainPrivacy.None) ? "" : 
                ((sdp == SubdomainPrivacy.STATDOM) ? (@"[0, " + ((BigInteger.One << (Config.NumericBits - 1)) - 1)+ @", " + statdomProb + @"]") :
                @"[0, " + ((BigInteger.One << (Config.NumericBits - 1)) - 1) + @"]")) + @";
                int x = 0(none);";
            if(sdp == SubdomainPrivacy.None || isAbnormal)
            {
                foreach (var ptn in patterns)
                {
                    var pattern = ptn;
                    for (int i = 0; i < Config.NumericBits - patternLen; ++i)
                    {
                        code +=
                            @"
                        if(v & " + mask + @"(none) == " + pattern + @"(none))
                        {
                            x = 1(none);
                        }
                    ";
                        pattern <<= 1;
                        mask <<= 1;
                    }
                }
            }
                     
            code += 
                @" 
                return x;
            }";
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            var programEnc = program.EncProgram();

            Party.RunAllParties(program, programEnc);

            return (Numeric)program.vTable["x"];
        }
        static double CorrTest(BigInteger val, BigInteger[] patterns, int patternLen)
        {
            int x = 0;
            BigInteger mask = Numeric.oneMaskMap[patternLen];
            foreach (var ptn in patterns)
            {
                var pattern = ptn;
                for (int i = 0; i < Config.NumericBits - patternLen; ++i)
                {
                    if ((val & mask) == pattern)
                        x = 1;
                    pattern <<= 1;
                    mask <<= 1;
                }
            } 
            return x;
        }

        static void RunTest()
        {
            string path = @"figure1.txt";
            string logPath = @"health_data_log1.txt";
            using (StreamWriter file = new StreamWriter(path)) { }
            using (StreamWriter file = new StreamWriter(logPath)) { }
            int keyLen = 512;
            int redundantBytes = 2;
            int numericBitLen = keyLen - 8 * redundantBytes;
            byte scaleBits = 21;
            var statdomProb = 0.95;
            BigInteger[] patterns = new BigInteger[] { 254235454 , 453534, 323425,67465,345345,2336547,45324668,45435435,45346,3423543};
            var patternLen = 32;
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);
            Config.DefaultEnc = EncryptionType.XOR;
            Config.watchVar = "v";
            int attempts = 1;
            double[] abnormalRate = new double[] { 0, 100 };
            long[] timeNoneAvg = new long[2], timeDOMAvg = new long[2], timeSTATDOMAvg = new long[2];
            for (int rateIter = 0; rateIter < abnormalRate.Count(); ++rateIter)
            {
                var rate = abnormalRate[rateIter];
                Random rnd = new Random();
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
                    byte[] buffer = new byte[Config.KeyBytes];
                    rnd.NextBytes(buffer);
                    // abnormal data
                    if (rnd.NextDouble() < (rate / 100))
                    {
                        buffer[Config.KeyBytes - redundantBytes - 1] = (byte)(buffer[Config.KeyBytes - redundantBytes - 1] & 0x7F);
                        for (int i = 0; i < redundantBytes; ++i)
                            buffer[Config.KeyBytes - i - 1] = 0;
                    }
                    // normal data
                    else
                    {
                        buffer[Config.KeyBytes - redundantBytes - 1] = (byte)(buffer[Config.KeyBytes - redundantBytes - 1] | 0x80);
                        for (int i = 0; i < redundantBytes; ++i)
                            buffer[Config.KeyBytes - i - 1] = 0xFF;
                    }
                    BigInteger v = new BigInteger(buffer);
                    //BigInteger v = -101;

                    timePerExe = Stopwatch.StartNew();
                    var corr = CorrTest(v, patterns, patternLen);
                    timePerExe.Stop();
                    var timeLocal = timePerExe.ElapsedMilliseconds;
                    WriteToFile(logPath, "v = " + v + ", binary: " + v.ToBinaryString(Config.KeyBits) + ", correctResult =  " + corr);

                    if (abnormalRate[rateIter] == 0)
                    {
                        Utility.ResetSeed();
                        timePerExe = Stopwatch.StartNew();
                        computed = Test(v, patterns, patternLen, SubdomainPrivacy.None, statdomProb, abnormalRate[rateIter] == 100).GetVal();
                        timePerExe.Stop();
                        timeNone += timePerExe.ElapsedMilliseconds;
                        corrCountNone += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                        WriteToFile(logPath, "    None:   computedResult = " + computed + " ,time = " + timePerExe.ElapsedMilliseconds);
                    }

                    Utility.ResetSeed();
                    timePerExe = Stopwatch.StartNew();
                    computed = Test(v, patterns, patternLen, SubdomainPrivacy.DOM, statdomProb, abnormalRate[rateIter] == 100).GetVal();
                    timePerExe.Stop();
                    corrCountDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                    if (abnormalRate[rateIter] == 0)
                    {
                        timeDOM = timeDOM + timePerExe.ElapsedMilliseconds + timeLocal;
                        WriteToFile(logPath, "    DOM:    computedResult = " + computed + " ,time = " + (timePerExe.ElapsedMilliseconds + timeLocal));
                    }
                    else
                    {
                        timeDOM = timeDOM + timePerExe.ElapsedMilliseconds;
                        WriteToFile(logPath, "    DOM:    computedResult = " + computed + " ,time = " + timePerExe.ElapsedMilliseconds);
                    }
                        
                    
                   
                    Utility.ResetSeed();
                    timePerExe = Stopwatch.StartNew();
                    computed = Test(v, patterns, patternLen, SubdomainPrivacy.STATDOM, statdomProb, abnormalRate[rateIter] == 100).GetVal();
                    timePerExe.Stop();
                    corrCountSTATDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
                    if (abnormalRate[rateIter] == 0)
                    {
                        timeSTATDOM = timeSTATDOM + timePerExe.ElapsedMilliseconds + timeLocal;
                        WriteToFile(logPath, "    STATDOM:computedResult = " + computed + " ,time = " + (timePerExe.ElapsedMilliseconds + timeLocal));
                    }
                        
                    else
                    {
                        timeSTATDOM = timeSTATDOM + timePerExe.ElapsedMilliseconds;
                        WriteToFile(logPath, "    STATDOM:computedResult = " + computed + " ,time = " + timePerExe.ElapsedMilliseconds);
                    }
                                     
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
                WriteToFile(logPath, "DOM:     accuracy: " + corrCountDOM / attempts + ", average time: " + timeDOM / attempts + " ms");
                WriteToFile(logPath, "STATDOM: accuracy: " + corrCountSTATDOM / attempts + ", average time: " + timeSTATDOM / attempts + " ms");
                WriteToFile(logPath, "");
            }

            abnormalRate = new double[] { 0, 0.25, 0.5, 1, 2, 4, 8, 16, 32, 64, 100 };
            OutputResult(path, "abnormalrate none  dom  statdom");
            foreach (var rate in abnormalRate)
            {
                double
                    timenone = ((1 - rate / 100) * timeNoneAvg[0] + rate / 100 * timeNoneAvg[1]),
                    timedom = ((1 - rate / 100) * timeDOMAvg[0] + rate / 100 * timeDOMAvg[1]),
                    timestatdom = ((1 - rate / 100) * timeSTATDOMAvg[0] + rate / 100 * timeSTATDOMAvg[1]);
                OutputResult(path, rate + " " + 1 + " " + timedom / timenone + " " + timestatdom / timenone);
            }
        }

        //static void RunTest2()
        //{
        //    string path = @"figure2.txt";
        //    string logPath = @"control_algorithm_log2.txt";
        //    using (StreamWriter file = new StreamWriter(path)) { }
        //    using (StreamWriter file = new StreamWriter(logPath)) { }
        //    int keyLen = 64;
        //    int numericBitLen = 60;
        //    byte scaleBits = 21;
        //    Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);
        //    int attempts = 10;
        //    double[] abnormalRate = new double[] { 0, 100 };
        //    long[] timeNoneAvg = new long[2], timeSTATDOMAvg = new long[2];
        //    // first, measure the average running time for 10 normal inputs(t0) and 10 abnormal inputs(t1) with p = 1(always reveal v if v is normal)
        //    for (int rateIter = 0; rateIter < 2; ++rateIter)
        //    {
        //        var rate = abnormalRate[rateIter];
        //        Random rnd = new Random(1);
        //        int corrCountNone = 0, corrCountSTATDOM = 0;
        //        long timeNone = 0, timeSTATDOM = 0;
        //        Stopwatch timePerExe;
        //        double computed;
        //        //OutputResult(path, "Normal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
        //        WriteToFile(logPath, "Abnormal Rate: " + rate + "%, Key Length: " + keyLen + ", Numeric Length: " + numericBitLen + ", Fraction Length: " + scaleBits);
        //        //OutputResult(path, "");
        //        WriteToFile(logPath, "");
        //        for (int attmpIter = 0; attmpIter < attempts; attmpIter++)
        //        {
        //            int v;
        //            // abnormal data
        //            if (rnd.NextDouble() < (rate / 100))
        //            {
        //                v = rnd.Next(50, 1000);
        //            }
        //            // normal data
        //            else
        //            {
        //                v = Math.Abs(rnd.Next()) % 50;
        //            }

        //            var corr = CorrTest(v);
        //            WriteToFile(logPath, "v = " + v + ", correctResult =  " + corr);

        //            if (rateIter == 0)
        //            {
        //                Utility.ResetSeed();
        //                timePerExe = Stopwatch.StartNew();
        //                computed = Test(v, SubdomainPrivacy.None, 1).GetVal();
        //                timePerExe.Stop();
        //                timeNone += timePerExe.ElapsedMilliseconds;
        //                corrCountNone += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
        //                WriteToFile(logPath, "    None:   computedResult = " + computed);
        //            }

        //            Utility.ResetSeed();
        //            timePerExe = Stopwatch.StartNew();
        //            // always reveal v if v is normal
        //            computed = Test(v, SubdomainPrivacy.STATDOM, 1).GetVal();
        //            timePerExe.Stop();
        //            timeSTATDOM += timePerExe.ElapsedMilliseconds;
        //            corrCountSTATDOM += ((Math.Abs(computed - corr) < 0.1) ? 1 : 0);
        //            WriteToFile(logPath, "    STATDOM:computedResult = " + computed);
        //            WriteToFile(logPath, "");
        //        }
        //        if (rateIter == 0)
        //        {
        //            timeNoneAvg[0] = timeNone / attempts;
        //        }
        //        else
        //        {
        //            timeNoneAvg[1] = timeNoneAvg[0];
        //        }
        //        timeSTATDOMAvg[rateIter] = timeSTATDOM / attempts;
        //        WriteToFile(logPath, "");
        //        WriteToFile(logPath, "None:    accuracy: " + corrCountNone / attempts + ", average time: " + timeNone / attempts + " ms");
        //        WriteToFile(logPath, "STATDOM: accuracy: " + corrCountSTATDOM / attempts + ", average time: " + timeSTATDOM / attempts + " ms");
        //        WriteToFile(logPath, "");
        //    }
        //    // 99% of the inputs are normal, 
        //    // reveal1Time is the average time when p = 1
        //    // reveal0Time is the average time when p = 0, essentially  reveal0Time == t1
        //    var reveal1Time = 0.99 * timeSTATDOMAvg[0] + 0.01 * timeSTATDOMAvg[1];
        //    var reveal0Time = (double)timeSTATDOMAvg[1];
        //    var revealProb = new double[] { 0, 0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1 };
        //    OutputResult(path, "p None STATDOM");
        //    foreach (var prob in revealProb)
        //    {
        //        var time = (prob * reveal1Time + (1 - prob) * reveal0Time);
        //        // normalize by computing time for 0%
        //        OutputResult(path, prob + " 1 " + time / timeNoneAvg[0]);
        //    }
        //}
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
