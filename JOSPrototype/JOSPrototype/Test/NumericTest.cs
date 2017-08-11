using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    class NumericTest
    {
        public static void Main(string[] args)
        {
            Config.SetGlobalParameters(32, 20, 2, false);
            //Console.WriteLine(Numeric.Sin(new Numeric(15, 6)));
            //for (int i = 0; i < 20; ++i)
            //{
            //    var temp = EnDec.randGen.NextSignedNumeric(14, 16);
            //    Console.WriteLine(new Numeric(2, 0) * Numeric.Cos(Numeric.DevideBy2(temp)));
            //    Console.WriteLine(2 * Math.Cos((double)temp.GetVal() / 2));
            //}
            byte scale = 12;
            Console.WriteLine(new Numeric(-100, scale));
            Console.WriteLine(new Numeric(-75, scale));
            Console.WriteLine(new Numeric(-1000, scale) * new Numeric(-75, scale));
            
            Console.ReadKey();
        }
    }
}
