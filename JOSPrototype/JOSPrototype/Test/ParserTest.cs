using JOSPrototype.Components;
using JOSPrototype.Frontend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    class ParserTest
    {
        public static void Main(string[] args)
        {
            string code = @"        {
            int amplitude = 0;
            double counter = 0;
            CalcLong vMyCalcExMonth.CurrentValue = 0;
            bool calc_month.Invalid = true;
            int mCounter = 1800;

            CalcLong calc_month = vMyCalcExMonth.CurrentValue;
            if (calc_month.Invalid == true)
                vMyCalcExMonth.CurrentValue = 8;

            mCounter=mCounter+1;
            if (mCounter >= 1800)
            {
                mCounter = 0;
                if (vMyCalcExMonth.CurrentValue != 12)
                    vMyCalcExMonth.CurrentValue = calc_month + 1;
                else
                    vMyCalcExMonth.CurrentValue = 1;
            }
            int min = 0;
			int	max = 0;          
            switch (vMyCalcExMonth.CurrentValue)
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
            if ((vMyCalcExMonth.CurrentValue >= 8 && vMyCalcExMonth.CurrentValue <= 12) || vMyCalcExMonth.CurrentValue == 1 || vMyCalcExMonth.CurrentValue == 2)
                increasing_or_decreasing_factor = -1;
			double temperature = ((max + min) / 2 + amplitude * (System.Math.Sin(counter) / 3 + System.Math.Sin(counter * 25 / 10) / 3 + System.Math.Sin(counter / 10) / 3)) + ((increasing_or_decreasing_factor) * (mCounter * 0.001667));	
            return temperature;
        }";
            Parser p = new Parser(code);
            Program pro = p.GetProgram();
            pro.Translate();
            Console.ReadKey();
        }
    }
}
