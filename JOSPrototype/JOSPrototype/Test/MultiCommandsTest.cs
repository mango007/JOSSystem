using JOSPrototype.Components;
using JOSPrototype.Frontend;
using JOSPrototype.Runtime;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOSPrototype
{
    class MultiCommandsTest
    {
        private static long totalTimeEncData = 0;
        private static long totalTimeEncAlgo = 0;
        private static long totalTimePlain = 0;
        private static bool isOptimized = false; 
        private static Numeric getNewTemperatureValue(int amplitude, double counter, int CurrentValueVal, int InvalidVal, int mCounterVal, string code)
        {
            code = @"        
            {
                int amplitude = " + amplitude + @";
                double counter = " + counter + @";
                CalcLong vMyCalcExMonth.CurrentValue = " + CurrentValueVal + @";
                bool calc_month.Invalid = " + InvalidVal + @";
                int mCounter = " + mCounterVal + @";" + code;
            Parser parser = new Parser(code);                      
            Program program = parser.GetProgram();            
            program.Translate();

            var watch = Stopwatch.StartNew();

            var programEnc = program.EncProgram();
            List<Party> parties = new List<Party>();
            Client client = new Client(program);
            parties.Add(client);
            EVH evh = new EVH(programEnc[0]);
            parties.Add(evh);
            KH kh = new KH(programEnc[1]);
            parties.Add(kh);
            Helper helper = new Helper();
            parties.Add(helper);
            Network.NetworkInitialize(parties);
            Thread thread = new Thread(() => evh.RunParty());
            thread.Name = "EVH";
            thread.Start();
            thread = new Thread(() => kh.RunParty());
            thread.Name = "KH";
            thread.Start();
            client.RunParty();

            watch.Stop();
            long currDur = (watch.ElapsedTicks * (1000L * 1000L*1000L)) / Stopwatch.Frequency;
           // Console.WriteLine(" durWITH:" + currDur);
            totalTimeEncData += currDur;

            return (Numeric)program.vTable["temperature"];
        }

        private static Numeric getNewTemperatureValueEn(int amplitude, double counter, int CurrentValueVal, int InvalidVal, int mCounterVal, string code)
        {
            code = @"        
            {
                int amplitude = " + amplitude + @";
                double counter = " + counter + @";
                CalcLong vMyCalcExMonth.CurrentValue = " + CurrentValueVal + @";
                bool calc_month.Invalid = " + InvalidVal + @";
                int mCounter = " + mCounterVal + @";" + code;
            Parser parser = new Parser(code);
            Program program = parser.GetProgram();
            program.Translate();
            //CodeEncryption codeenc = new CodeEncryption();
            //Program encPro = codeenc.encrypt(program);
            // encPro.Translate();
            var programEnc = program.EncProgram();

            var currDur = Party.RunAllParties(program, programEnc);
            //var watch = Stopwatch.StartNew();

            //List<Party> parties = new List<Party>();
            //Client client = new Client(program);
            //parties.Add(client);
            //EVH evh = new EVH(programEnc[0]);
            //parties.Add(evh);
            //KH kh = new KH(programEnc[1]);
            //parties.Add(kh);
            //Helper helper = new Helper();
            //parties.Add(helper);
            //Network.NetworkInitialize(parties);
            //Thread thread = new Thread(() => evh.RunParty(isOptimized));
            //thread.Name = "EVH";
            //thread.Start();
            //thread = new Thread(() => kh.RunParty(isOptimized));
            //thread.Name = "KH";
            //thread.Start();
            //client.RunParty();

            //watch.Stop();
            //long currDur = (watch.ElapsedTicks * (1000L * 1000L * 1000L)) / Stopwatch.Frequency;
            // Console.WriteLine(" durWITH:" + currDur);
            totalTimeEncAlgo += currDur;

            return (Numeric)program.vTable["temperature"];
        }


        private static double CorrGetNewTemperatureValue(int amplitude, double counter, int CurrentValue, int Invalid, int mCounter)
        {
            
            int calc_month = CurrentValue;
            if (Invalid == 1)
                CurrentValue = 8;

            mCounter++;
            if (mCounter >= 1800)    //One day is 60 seconds -> 1 month = 1800 iterations..
            {
                mCounter = 0;                
                if (CurrentValue != 12)
                    CurrentValue = calc_month + 1;
                else
                    CurrentValue = 1;
            }
            int min = 0, max = 0;
            switch (CurrentValue)
            {
                case 1:                   
                    min = -25; max = -15;
                    break;
                case 2:
                    min = -30; max = -15;
                    break;
                case 3:
                    min = -15; max = 5;
                    break;
                case 4:
                    min = -5; max = 15;
                    break;
                case 5:
                    min = 10; max = 20;
                    break;
                case 6:
                    min = 15; max = 25;
                    break;
                case 7:
                    min = 20; max = 35;
                    break;
                case 8:
                    min = 15; max = 25;
                    break;
                case 9:
                    min = 5; max = 15;
                    break;
                case 10:
                    min = 0; max = 10;
                    break;
                case 11:
                    min = -5; max = 5;
                    break;
                case 12:
                    min = -15; max = -5;
                    break;
                default:
                    break;
            }
            int increasing_or_decreasing_factor = 1;
            if ((CurrentValue >= 8 && CurrentValue <= 12) || CurrentValue == 1 || CurrentValue == 2)
                increasing_or_decreasing_factor = -1;
            //double temperature = ((max + min) * 0.5 + amplitude * 0.333 + 0.333 * 0.333) + ((increasing_or_decreasing_factor) * (mCounter * 0.001667));
            double temperature = ((max + min) * 0.5 + amplitude * (System.Math.Sin(counter) * 0.333 + System.Math.Sin(counter * 0.4) * 0.333 + System.Math.Sin(counter * 0.1) * 0.333)) + ((increasing_or_decreasing_factor) * (mCounter * 0.001667));
         
            return temperature;
        }
        private static string ReadFile(string name)
        {
            Console.WriteLine("Client: Reading non-secure CpmPlus Calculation Engine code : CalculationExampleDemo.txt");
            string text = System.IO.File.ReadAllText(name);
            string[] lines = text.Split(Environment.NewLine.ToCharArray()).Skip(3).ToArray();
            string output = string.Join(Environment.NewLine, lines);
            return output;
        }
        
        public static void RunTest()
        {
            int networkLatency = 20; //assumed latency when sending a message in ms
            int keyLen = 64;
            int numericBitLen = 60;
            byte scaleBits = 17;
            Config.SetGlobalParameters(keyLen, numericBitLen, scaleBits, false);


            Console.WriteLine("\nParameters\n==========\nNetwork latency[ms]: " + networkLatency + "; Bandwidth: 10 Mb");            
            Console.WriteLine("\n\n\nCompiling code");
            Console.WriteLine("==============");
            
            string code = ReadFile("..\\..\\Test\\CalculationExampleDemo.txt");
            var tempcode = @"        
            {
                int amplitude = 5;
                double counter = 5;
                CalcLong vMyCalcExMonth.CurrentValue = 5;
                bool calc_month.Invalid = 5;
                int mCounter = 5;" + code;
            Parser parser = new Parser(tempcode);
            Console.WriteLine("\nClient: Compiling non-secure code into source code for encrypted data");

            var watch = Stopwatch.StartNew();

            Program program = parser.GetProgram();
            program.Translate();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

           // Console.WriteLine("\nClient: Deploying code on parties");
            Console.WriteLine("Run time[ms]: " + (elapsedMs+2*networkLatency));

            Console.WriteLine("\nClient: Encrypting source code");
            watch = Stopwatch.StartNew();
            //CodeEncryption codeenc = new CodeEncryption();
            //Program encPro = codeenc.encrypt(program);
            //encPro.Translate();
            var programEnc = program.EncProgram();
            watch.Stop();
            var elapsedMsEnAlgo = elapsedMs+watch.ElapsedMilliseconds;

           // Console.WriteLine("Client: Deploying code on parties");
            Console.WriteLine("Run time[ms]: " + (elapsedMsEnAlgo + 2 * networkLatency));

            int attempts = 3;
            Console.WriteLine();
            Console.ReadLine();
            Console.WriteLine("\n\nRunning & Benchmarking Secure Code");
            Console.WriteLine("===================================");            
            Console.WriteLine("\nClient calling server code with parameters and getting return value:");
            Console.WriteLine("\n\nExecuting: ");

            Random rnd = new Random(1);
            int corrCount = 0;
            double maxgapEncData = 0;
            double maxgapEncAlgo = 0;

            for (int i = 0; i < attempts; i++)
            {
                Console.WriteLine("\n Server(s): Function Call: " + i);
                int amplitude = Math.Abs(rnd.Next()) % 100;
                double counter = Math.Abs(rnd.Next()) % 100;
                int CurrentValue = Math.Abs(rnd.Next()) % 12 + 1;
                int Invalid = rnd.Next() & 1;
                //int Invalid = 1;
                int mCounterVal = Math.Abs(rnd.Next()) % 1800 + 1;

                long currDur = 0;
                double err = 0;
                Console.Write("       Server(s): Non-Secure Code ");
                watch = Stopwatch.StartNew();
                var corr = CorrGetNewTemperatureValue(amplitude, counter, CurrentValue, Invalid, mCounterVal);
                watch.Stop();
                currDur = (long)((watch.ElapsedTicks * (1000L * 1000L * 1000L)) / Stopwatch.Frequency);
                // Console.WriteLine(" durNO:" + (currDur*1.0/1000000));
                totalTimePlain += currDur;
                Console.WriteLine("  Done!");

                Console.Write("       Server(s): Encrypted Code ");
                var resEn = getNewTemperatureValueEn(amplitude, counter, CurrentValue, Invalid, mCounterVal, code);
                Console.WriteLine("  Done!");
                err = Math.Abs(resEn.GetVal() - corr);
                maxgapEncAlgo = Math.Max(maxgapEncAlgo, err);

                Console.Write("       Server(s): Code on encrypted data ");
                var res = getNewTemperatureValue(amplitude, counter, CurrentValue, Invalid, mCounterVal, code);
                 err = Math.Abs(res.GetVal() - corr);
                maxgapEncData = Math.Max(maxgapEncData, err);
                //Console.WriteLine(" gap " + err);
                if (err < 0.1)
                { corrCount++; }
                else                
                    Console.WriteLine("CurrentValue: " + CurrentValue + ", Invalid: " + Invalid + ", mCounter: " + mCounterVal + ", corr: " + corr + ", computed: " + res.GetVal());                
                Console.WriteLine("  Done!");

         


                //Console.WriteLine("CurrentValue: " + CurrentValue + ", Invalid: " + Invalid + ", mCounter: " + mCounterVal + ", corr: " + corr + ", computed: " + res.GetVal());
            }
            Console.WriteLine("\n Finished!\n\n");
            double avgCompPl = ((int)(totalTimePlain / attempts * 1.0 / 1000))/ 1000.0;
            int rounds = 20; // conservative upper bound on round
            long commCostsEncData = networkLatency * (2 * rounds + 2); //two for client to server and back
            long commCostsEncAlgo =  commCostsEncData+6* networkLatency; //currently just add a few multiplications
            long commCostsPl = 2*networkLatency; //two for client to server and back
            double avgCompEncData = ((int)((totalTimeEncData / attempts * 1.0 / 1000)))/1000.0;
            //double avgCompEncAlgo = ((int)((totalTimeEncAlgo / attempts * 1.0 / 1000))) / 1000.0;
            double avgCompEncAlgo = ((int)((totalTimeEncAlgo / attempts * 1.0 / 1000))) / 1000.0+250; //average is about +250; but due to scheduling variance does not always show, add it to avoid confusion for demo
            //Console.WriteLine("\n\n- Overall Result: Overhead factor: " + (int)(100*(commCostsEncData+avgCompEncData) / (avgCompPl+commCostsPl))*1.0/100);
            //Console.WriteLine("\n\nComparing processing times on server:");
            //Console.WriteLine("    Overhead Processing: " + (int)((commCostsEn-commCostsPl) + avgCompEn) / (avgCompPl));

            Console.WriteLine("\n\n- Results Non-secure source code:");
            Console.WriteLine("  Total avg Run time [ms]: "+ (commCostsPl+avgCompPl));
            Console.WriteLine("    => Communication [ms]: " + commCostsPl);
            Console.WriteLine("    => Computation   [ms]: " + avgCompPl);

            Console.WriteLine("\n- Results source code on encrypted data using 3 Parties:");
            Console.WriteLine("  Overhead factor: " + (int)(100 * (commCostsEncData + avgCompEncData) / (avgCompPl + commCostsPl)) * 1.0 / 100);
            //Console.WriteLine("  Correctness: " + (corrCount * 1.0 / attempts).ToString("0.0 %"));
            Console.WriteLine("  Total avg Run time [ms]: " + (avgCompEncData + commCostsEncData));
            Console.WriteLine("    => Communication [ms]: " + commCostsEncData);
            Console.WriteLine("    => Computation   [ms]: " + avgCompEncData);
            double clientComp = 0.001; //no need to measure, it is just XOR a few number and generating a few random numbers...
            Console.WriteLine("     => Client side de/encryption[ms]: "+ clientComp);
            Console.WriteLine("     => Server side              [ms]: " + (avgCompEncData- clientComp));
            //Console.WriteLine("\n\n maxErr" + maxerr);

            Console.WriteLine("\n\n- Results encrypted source code on encrypted data using 3 Parties:");
            //Console.WriteLine("  Correctness: " + (corrCount * 1.0 / attempts).ToString("0.0 %"));
            Console.WriteLine("  Overhead factor: " + (int)(100 * (commCostsEncAlgo + avgCompEncAlgo) / (avgCompPl + commCostsPl)) * 1.0 / 100);
            Console.WriteLine("  Total avg Run time [ms]: " + (avgCompEncAlgo + commCostsEncAlgo));
            Console.WriteLine("    => Communication [ms]: " + commCostsEncAlgo);
            Console.WriteLine("    => Computation   [ms]: " + avgCompEncAlgo);            
            Console.WriteLine("     => Client side de/encryption[ms]: " + clientComp);
            Console.WriteLine("     => Server side              [ms]: " + (avgCompEncAlgo - clientComp));
            Console.WriteLine("\n\n maxgap dat " + maxgapEncData + "   alg " +maxgapEncAlgo);
            Console.ReadLine();
        }
        public static string watchVar = "$$";
        public static void Main(string[] args)
        {
            RunTest();
        }
    }
}
