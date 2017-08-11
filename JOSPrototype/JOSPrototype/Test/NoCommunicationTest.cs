using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace JOSPrototype.Test
{
    class NoCommunicationTest
    {
        //public static Dictionary<int, int> oneMaskMap = new Dictionary<int, int>();
        //public static Dictionary<int, int> signMaskMap = new Dictionary<int, int>();
        private static Dictionary<int, BigInteger> signMaskMap = new Dictionary<int, BigInteger>();
        private static Dictionary<int, BigInteger> oneMaskMap = new Dictionary<int, BigInteger>();
        //public static Numeric Zero { get { return new Numeric(0); } }
        //public static Numeric One { get { return new Numeric(1); } }
        //public static Numeric Two { get { return new Numeric(2); } }
        static NoCommunicationTest()
        {
            BigInteger oneMask = BigInteger.One;
            BigInteger signMask = BigInteger.One;
            oneMaskMap.Add(1, oneMask);
            oneMaskMap.Add(0, 0);
            signMaskMap.Add(1 , signMask);
            for (int i = 1; i < keyBits; ++i)
            {
                oneMask = (oneMask << 1) + 1;
                oneMaskMap.Add(i + 1, oneMask);
                signMask <<= 1;
                signMaskMap.Add(i + 1, signMask);
            }
        }
        private const int keyBits = 64;
        private const int intBits = 60;
        private const byte scaleBits = 17;
        private static BigInteger scale = BigInteger.One << scaleBits;
        private static Random rnd = new Random(1);
        private static BigInteger GenerateInt()
        {           
            var buffer = new byte[(keyBits - 1) / 8 + 1];
            rnd.NextBytes(buffer);
            BigInteger re = new BigInteger(buffer) & oneMaskMap[keyBits];
            return re;
        }
        private static double GetVal(BigInteger num, int len = keyBits)
        {
            if ((num & signMaskMap[len]) != 0)
            {
                return ((double)((num & oneMaskMap[len]) - oneMaskMap[len] - 1)) / (double)scale;
            }
            else
            {
                return ((double)(num & oneMaskMap[len])) / (double)scale; 
            }
        }
        private static BigInteger GetIntVal(BigInteger num, int len = keyBits)
        {
            if ((num & signMaskMap[len]) != 0)
            {
                return (((num & oneMaskMap[len]) - oneMaskMap[len] - 1));
            }
            else
            {
                return ((num & oneMaskMap[len]));
            }
        }
        public static BigInteger PadSign(BigInteger num, int len)
        {
            if ((num & signMaskMap[len]) != 0)
            {
                return (num & oneMaskMap[len]) - oneMaskMap[len] - 1;
            }
            else
            {
                return num & oneMaskMap[len];
            }
        }
        public static string ToString(BigInteger num)
        {
            var base2 = new StringBuilder();
            BigInteger one = BigInteger.One << (keyBits - 1);
            for (int i = 0; i < keyBits; ++i)
            {
                base2.Append(((num & one) == 0) ? 0 : 1);
                one >>= 1;
            }
            return "val: " + GetVal(num) + ", bits: " + base2.ToString() + ", intVal: " + (((num & signMaskMap[keyBits]) == 0) ? (num & oneMaskMap[keyBits]) : ((num & oneMaskMap[keyBits]) - oneMaskMap[keyBits] - 1));
        }

        public static void Sin()
        {
            int ncorr = 0;
            int attempts = 100;
            double maxerr = 0;

            for (int i = 0; i < attempts; i++)
            {
                //get random parameters                
                var a = PadSign(GenerateInt() & oneMaskMap[intBits] , intBits) & oneMaskMap[keyBits];
                
                Console.WriteLine("a: " + ToString(a));
                //int sign = ra.Next() & 1;
                //a = sign == 1 ? -a : a;
                var k = PadSign(GenerateInt() & oneMaskMap[keyBits - 1], keyBits - 1) & oneMaskMap[keyBits];
                Console.WriteLine("k: " + ToString(k));

                var encka = (a + k) & oneMaskMap[keyBits];
                //if(k > encka)
                //{
                //    encka += (1 << effectiveBits);
                //}
                var sk = (BigInteger)(Math.Sin(GetVal(k)) * (double)scale) & oneMaskMap[keyBits];
                Console.WriteLine("sk: " + ToString(sk));
                var kp = GenerateInt() & oneMaskMap[keyBits];
                var kpp = (-2 * k + kp) & oneMaskMap[keyBits];
                var t0 = (BigInteger)(2 * Math.Sin(GetVal(encka) / 2) * (double)scale) & oneMaskMap[keyBits];
                Console.WriteLine("a + k: " + ToString(encka));
                Console.WriteLine("a + k corr: " + (GetVal(a) + GetVal(k)));
                Console.WriteLine("t0: " + ToString(t0));
                Console.WriteLine("t0 corr: " + 2 * Math.Sin((GetVal(a) + GetVal(k)) / 2));
                var EncMinusKPlusKpa = (encka + kpp) & oneMaskMap[keyBits];
                var EncMinusKa = (EncMinusKPlusKpa - kp) & oneMaskMap[keyBits];
                var t1 = (BigInteger)(Math.Cos(GetVal(EncMinusKa) / 2) * (double)scale) & oneMaskMap[keyBits];
                Console.WriteLine("a - k: " + ToString(EncMinusKa));
                Console.WriteLine("a - k corr: " + (GetVal(a) - GetVal(k)));
                Console.WriteLine("t1: " + ToString(t1));
                Console.WriteLine("t1 corr: " + Math.Cos((GetVal(a) - GetVal(k)) / 2));
                //int kpppp = ra.Next();
                //int Enckppppt1 = t1 + kpppp;
                //int kppp = ra.Next();
                //int Enckpppt0 = t0 + kppp;


                var enckf = ((BigInteger)(GetVal(t1) * GetVal(t0) * (double)scale)) & oneMaskMap[keyBits];
                Console.WriteLine("enckf: "+ ToString(enckf));
                Console.WriteLine("enckf corr: " + 2 * Math.Sin((GetVal(a) + GetVal(k)) / 2) * Math.Cos((GetVal(a) - GetVal(k)) / 2));
                var kf =  sk;
                Console.WriteLine("kf: " + ToString(kf));
                Console.WriteLine("kf corr: " + Math.Sin(GetVal(k)));

                //sin(a)=2*sin((a+k)/2)*cos((a-k)/2)
                double res = GetVal(enckf - kf);
                double corr = Math.Sin(GetVal(a));
                Console.WriteLine("res: " + ToString(enckf - kf));
                Console.WriteLine("res corr: " + corr);
                double err = Math.Abs(corr - res);
                maxerr = Math.Max(err, maxerr);
                if (err > 0.01)
                    Console.WriteLine("ERR:" + err + "  Res: " + res + "  Corr:" + corr + " scale:" + scale + " k: " + GetVal(k) + "  a: " + GetVal(a));
                else
                {
                    //if (err < 1e-10) err = 0;
                    //Console.WriteLine("    x:" + x*1.0 / scale + "    Serr:" + err);

                    ncorr++;
                }
                Console.WriteLine();
            }
            System.Console.Out.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
        }
        private static int GetMSBNonzero(BigInteger val)
        {
            var mask = signMaskMap[intBits];
            int count = 0;
            while((val & mask) == 0)
            {
                count++;
                val <<= 1;
            }
            return count;
        }
        private static int TaylorLog(int x, int at)
        {
            double re = Math.Log(at);
            for(int i = 1; i <= 7; ++i)
            {
                re += ((i & 1) == 0 ? -1.0 : 1.0) / Math.Pow(at, i) / i * Math.Pow(x - at, i);
            }
            return (int)re;
        }
        //public static void Log()
        //{
        //    uint at = (uint)((1 << (effectiveBits - 1)) + (1 << (effectiveBits - 2)));
        //    double[] d = new double[8];
        //    d[0] = 1.0 / at;
        //    for (int j = 1; j <= 7; ++j) { d[j] = ((j & 1) == 0 ? -1.0 : 1.0) * d[j - 1] / at; } 
        //    int ncorr = 0;
        //    int attempts = 100;
        //    var ra = new Random(123);
        //    double maxerr = 0;
        //    for (int i = 0; i < attempts; i++)
        //    {
        //        uint a = (uint)(ra.Next() & oneMaskMap[effectiveBits]) % 1024 + 1;

        //        int shiftBits = GetMSBNonzero(a);
        //        uint ap = a << shiftBits;
        //        //int kpp = ra.Next();
        //        //int enckppap = ap + kpp;
        //        uint[] et = new uint[8], kt = new uint[8];
        //        et[0] = 1; kt[0] = 0;
        //        double elogapt = Math.Log(at), klogapt = 0;
                
        //        for(int j = 1; j <= 7;++j)
        //        {
        //            kt[j] = (uint)ra.Next();
        //            et[j] = et[j - 1] * (ap - at) + kt[j];
        //            elogapt += d[j - 1] / j * et[j];
        //            klogapt += d[j - 1] / j * kt[j];
        //        }

        //        int elogap = (int)elogapt, klogap = (int)klogapt;


        //        double res = (int)(elogap - klogap - Math.Log(Math.Pow(2, shiftBits)));
        //        double corr = Math.Log(a);
        //        double err = Math.Abs(corr - res);
        //        maxerr = Math.Max(err, maxerr);
        //        if (err > 1)
        //            Console.WriteLine("ERR:" + err + "  Res: " + res + "  Corr:" + corr + " scale:" + scale   + "  a: " + a);
        //        else
        //        {
        //            //if (err < 1e-10) err = 0;
        //            //Console.WriteLine("    x:" + x*1.0 / scale + "    Serr:" + err);

        //            ncorr++;
        //        }
        //    }
        //    System.Console.Out.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
        //}
        private static double TaylorInverse(int x, int at)
        {
            double re = 0;
            for (int i = 0; i <= 7; ++i)
            {
                re += ((i & 1) == 0 ? 1.0 : -1.0) / Math.Pow(at, i + 1) * Math.Pow(x - at, i);
            }
            return re;
        }
        public static void Inverse()
        {
            // int at = 1;
            int magnificationBits = intBits / 2;
            double magnificationFactor = 1 << magnificationBits;
            int reductionBits = intBits - 9;
            int reductionFactor = 1 << reductionBits;
            long at = (1 << (intBits - 2)) + (1 << (intBits - 1));
            double[] d = new double[8];
            d[0] = 1.0 / at;
            for (int j = 1; j <= 7; ++j) { d[j] = (-1) * d[j - 1] / at * reductionFactor; }
            int ncorr = 0;
            int attempts = 1;
            var ra = new Random(123);
            double maxerr = 0;
            //var temp = TaylorInverse(at, at);
            for (int i = 0; i < attempts; i++)
            {
                long a = 1000;
                //long a = (ra.Next() & oneMaskMap[effectiveBits]) + 1;

                int shiftBits = GetMSBNonzero(a);
                long ap = a << shiftBits;
                long[] et = new long[8], kt = new long[8];
                et[0] = 1; kt[0] = 0;
                double elogapt = d[0], klogapt = 0;

                for (int j = 1; j <= 7; ++j)
                {
                    //kt[j] = ra.Next() & oneMaskMapLong[32];
                    kt[j] = 0;
                    et[j] = et[j - 1] * ((ap - at) >> reductionBits);
                    elogapt += (d[j] * (et[j] + kt[j]));
                    klogapt += (d[j] * kt[j]);
                }

                long elogap = (long)(elogapt * magnificationFactor), 
                    klogap = (long)(klogapt * magnificationFactor);


                double res = ((int)((elogap - klogap) << shiftBits)) / magnificationFactor;
                double corr = 1.0 / a;
                double err = Math.Abs(corr - res);
                maxerr = Math.Max(err, maxerr);
                if (err > 0.00000001)
                {
                    Console.WriteLine("ERR:" + err + "  Res: " + res + "  Corr:" + corr + "  a: " + a);
                }    
                else
                {
                    //if (err < 1e-10) err = 0;
                    //Console.WriteLine("    x:" + x*1.0 / scale + "    Serr:" + err);

                    ncorr++;
                    
                }
            }
            Console.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
        }
        //public static void getMultiplicationScale()
        //{
        //    int ncorr = 0;
        //    int attempts = 100;
        //    var ra = new Random(123);
        //    double maxerr = 0;
        //    for (int i = 0; i < attempts; i++)
        //    {
        //        int a = ra.Next();
        //        int b = ra.Next();
        //        int ka = ra.Next();
        //        int kb = ra.Next();
        //        int enckaa = a + ka;
        //        int enckbb = b + kb;
        //        int k2 = ra.Next();
        //        int EncKbPlusK2b = enckbb + k2;
        //        int k6 = ra.Next();
        //        int EncK6a = ka + k6;

        //        int enckf = (enckaa * enckbb) / (int)scale;
        //        int kf = (enckaa * kb + enckbb * ka - ka * kb) / (int)scale;

        //        double res = GetVal(enckf - kf);
        //        double corr = GetVal(a * b);
        //        double err = Math.Abs(corr - res);
        //        maxerr = Math.Max(err, maxerr);
        //        if (err > 0.01)
        //            Console.WriteLine("ERR:" + err + "  Res: " + res + "  Corr:" + corr + " scale:" + scale + " ka: " + ka + " kb: " + kb + "  a: " + a + "  b: " + b);
        //        else
        //        {
        //            //if (err < 1e-10) err = 0;
        //            //Console.WriteLine("    x:" + x*1.0 / scale + "    Serr:" + err);

        //            ncorr++;
        //        }
        //    }
        //}
        public static void MutiplicationWithScaling()
        {
            int ncorr = 0;
            int attempts = 100;
            double maxerr = 0;

            for (int i = 0; i < attempts; i++)
            {
                //get random parameters                
                var a = PadSign(GenerateInt() & oneMaskMap[intBits / 2], intBits / 2) & oneMaskMap[keyBits];
                //Console.WriteLine("a: " + ToString(a));
                var b = PadSign(GenerateInt() & oneMaskMap[intBits / 2], intBits / 2) & oneMaskMap[keyBits];
                //Console.WriteLine("b: " + ToString(b));
                //Console.WriteLine("corr: " + GetVal(a) * GetVal(b));
                // Console.WriteLine("a: " + ToString(a));
                var kf = PadSign(GenerateInt() & oneMaskMap[intBits], intBits) & oneMaskMap[keyBits];
                
                var EncKfab = (((a * b) & oneMaskMap[keyBits]) + kf) & oneMaskMap[keyBits];
                //Console.WriteLine("a * b before scaling: " + ToString((a * b) & oneMaskMapLong[keyBits]));
                //Console.WriteLine("kf before scaling: " + ToString(kf));
                //Console.WriteLine("EncKfab before scaling: " + ToString(EncKfab));
                kf = ((BigInteger)((double)GetIntVal(kf) / (double)scale)) & oneMaskMap[keyBits];
                EncKfab = ((BigInteger)((double)GetIntVal(EncKfab) / (double)scale)) & oneMaskMap[keyBits];
                //Console.WriteLine("a * b after scaling: " + ToString((EncKfab - kf) & oneMaskMapLong[keyBits]));
                //Console.WriteLine("kf after scaling: " + ToString(kf));
                //Console.WriteLine("EncKfab after scaling: " + ToString(EncKfab));
                //sin(a)=2*sin((a+k)/2)*cos((a-k)/2)
                double res = GetVal((EncKfab - kf) & oneMaskMap[keyBits]);
                //double res = GetVal((((a * b) & oneMaskMapLong[keyBits]) >> scaleBits) );
                double corr = GetVal(a) * GetVal(b);
                double err = Math.Abs(corr - res);
                maxerr = Math.Max(err, maxerr);
                if (err > 0.001)
                    Console.WriteLine("ERR:" + err + "  Res: " + ToString((EncKfab - kf) & oneMaskMap[keyBits]) + "  Corr:" + corr + " scale:" + scale + " a: " + ToString(a) + "  b: " + ToString(b));
                else
                {
                    //if (err < 1e-10) err = 0;
                    //Console.WriteLine("    x:" + x*1.0 / scale + "    Serr:" + err);

                    ncorr++;
                }
                //Console.WriteLine();
            }
            System.Console.Out.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
        }

        public static void shuffleArr(long[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = Utility.NextInt(n--);
                long temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public static void permuteAndOff(ref long off, long[] vals, long[] pevals, long[] pkvals, long[] eivals)
        {
            //encrypt x with random offset o1
            off = Utility.NextInt();
            long ncases = vals.Length;
            long[] kc1 = new long[ncases];
            long[] ekc1 = new long[ncases];
            long[] ivals = new long[ncases];
            //Prepate permutation and double encrypt encrypted values 
            for (int j = 0; j < ncases; j++)
            {
                ivals[j] = j; //prepare array for permutation       

                long k = Utility.NextInt();  //double encrypt   
                kc1[j] = k;
                ekc1[j] = vals[j] + k;
            }
            shuffleArr(ivals); //permute indexvals, eg. 0,1,2,3,4...  => 4,1,3,2,0                
            for (int j = 0; j < ncases; j++) //permute casevals with same permutation, eg. 100, 101, 102, 103... using 4,1,3,2,0... => 104,101,103,102,100...
            {
                pevals[j] = ekc1[ivals[j]];
                pkvals[j] = kc1[ivals[j]];
                eivals[j] = (ivals[j] + off) % ncases;
                // Console.WriteLine(" Dec: eivals" + eivals[j]+ "  " + (pevals[j] - pkvals[j]));
            }
        }

        public static void helper(long ex, long k, long[] pevals, long[] pkvals, long[] eivals, ref long rese1, ref long resk1)
        {
            long xo1 = ex - k;  //HE gets key and encrypted value
            long ncases = pevals.Length;
            xo1 = xo1 % ncases;
            int pos;
            for (pos = 0; pos < ncases; pos++) //find index of  case value                
                if (eivals[pos] == xo1) break;
            rese1 = pevals[pos]; //return encrypted case value to EVH
            resk1 = pkvals[pos]; //return encrypted key to KH
        }

        public static void getSwitch2()
        {
            long[] casevals = new long[] { 100, 101, 102, 103 };
            int ncases = casevals.Length;
            // switch(x) {
            // case 0: result= casevals[0]; break;
            // case 1: result= casevals[1]; break;          
            int ncorr = 0;
            int attempts = 1000;
            long maxerr = 0;
            for (int i = 0; i < attempts; i++)
            {
                //get random parameters  
                //get casevalues              
                long[] kcasevals = new long[ncases];
                long[] ecasevals = new long[ncases];
                for (int j = 0; j < ncases; j++)
                {
                    long k = Utility.NextInt();
                    kcasevals[j] = k;
                    ecasevals[j] = casevals[j] + k;
                }
                // Get x to  compute switch(x)
                long x = Utility.NextInt(ncases - 1);
                long kx = Utility.NextInt();
                long ex = x + kx;

                //helping data structures
                long[] eivals = new long[ncases];
                long[] pevals = new long[ncases];
                long[] pkvals = new long[ncases];

                //Get encrypted value for EVH
                //ALL BY EVH
                long o1 = 0;
                permuteAndOff(ref o1, ecasevals, pevals, pkvals, eivals);
                long kxo1 = Utility.NextInt();
                long exo1 = ex + o1 + kxo1;
                //Send to HE: exo1, pevals,pkvals,eivals
                //Send to KH: kxo1
                //KH: Sends kxo1+kx to HE

                //HE
                long rese1 = 0, resk1 = 0;
                helper(exo1, kx + kxo1, pevals, pkvals, eivals, ref rese1, ref resk1);
                //HE sends:  rese to EVH, resk to KH


                //Get encrypted key for KH
                //ALL BY KH (exactly the same as for EVH) but for keys...
                long o2 = 0;
                permuteAndOff(ref o2, kcasevals, pevals, pkvals, eivals);
                long kx2 = Utility.NextInt();
                long kxo2 = kx - o2 + kx2;
                //Send to HE: kxo2, pevals,pkvals,eivals
                //Send kx2 to EVH
                //EVH send ex+kx2 to HE

                //HE
                long rese2 = 0, resk2 = 0;
                helper(ex + kx2, kxo2, pevals, pkvals, eivals, ref rese2, ref resk2);
                //HE sends:  resk2 to EVH, rese1 to KH  => ATTENTION opposite to before

                //Compute final result
                long rese = rese1 + resk2; //by EVH: final result for case 
                long resk = resk1 + rese2; //by KH: final result for case 

                long res = rese - resk;
                long corr = casevals[x];
                long err = Math.Abs(corr - res);
                maxerr = Math.Max(err, maxerr);
                if (err > 0)
                {
                    // Console.WriteLine("ERR:" + err + "  Res: " + (res) + "  Corr:" + corr + " k: " + k + "  x: " + x + "  sRes: " + (sres * 1.0 / (1 << shiftSize)));
                    Console.WriteLine("  Res: " + (res) + "  Corr:" + corr + " x:" + x);
                }
                else ncorr++;
            }
            System.Console.Out.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
            System.Console.Out.WriteLine(ncorr + "  " + attempts + "  maxerr:" + maxerr);
        }
        public static void Main(string[] args)
        {
            getSwitch2();
            Console.ReadKey();
        }
    }
}
