using JOSPrototype.Frontend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Test
{
    class TokenizerTest
    {
        public static void Main(string[] args)
        {
            string code = @"
            CalcLong calc_month = vMyCalcExMonth.CurrentValue;

	        vMyCalcExMonth.CurrentValue =  8*calc_month.Invalid+(1-calc_month.Invalid)*vMyCalcExMonth.CurrentValue; 
            mCounter=mCounter+1;
            if (mCounter == 1800)
            {
                mCounter = 0;
                if (vMyCalcExMonth.CurrentValue != 12)
                    vMyCalcExMonth.CurrentValue = calc_month + 1;
                else
                    vMyCalcExMonth.CurrentValue = 1;
            }
            int min = 0;
	        int	max = 0;  
	        int increasing_or_decreasing_factor = 1;  
	        int tempCurrentValue = vMyCalcExMonth.CurrentValue - 1; 
            switch (tempCurrentValue)
            {
                case 1:
                    min = -25; max = -15;increasing_or_decreasing_factor = -1; 
                    break;
                case 2:
                    min = -30; max = -15;increasing_or_decreasing_factor = -1; 
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
                    min = 15; max = 25;increasing_or_decreasing_factor = -1; 
                    break;
                case 9:
                    min = 5; max = 15;increasing_or_decreasing_factor = -1; 
                    break;
                case 10:
                    min = 0; max = 10;increasing_or_decreasing_factor = -1; 
                    break;
                case 11:
                    min = -5; max = 5;increasing_or_decreasing_factor = -1; 
                    break;         
                default:
                    min = -15; max = -5;increasing_or_decreasing_factor = -1; 
                    break;
            }

            double temperature = ((max + min) * 0.5 + amplitude * (System.Math.Sin(counter) * 0.333 + System.Math.Sin(counter * 0.4) * 0.333 + System.Math.Sin(counter * 0.1) * 0.333)) + ((increasing_or_decreasing_factor) * (mCounter * 0.001667));
            return temperature;";

            var tokens = Tokenizer.Tokenize(code);
        }
    }
}
