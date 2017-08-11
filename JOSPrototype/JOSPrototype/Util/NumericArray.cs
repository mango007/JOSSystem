using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype
{
    // Numeric array wrapper
    class NumericArray
    {
        public NumericArray() { }
        public NumericArray(int size)
        {
            numericArray = new Numeric[size];
        }
        public NumericArray(params Numeric[] array)
        {
            numericArray = array;
        }
        public void SetArray(Numeric[] array)
        {
            // if array elements have been instantiated 
            if(!ReferenceEquals(numericArray, null))
            {
                System.Diagnostics.Debug.Assert(array.Length == numericArray.Length);
                for(int i = 0; i < array.Length; ++i)
                {
                    // copy Numeric but do not create a new instance
                    numericArray[i].Copy(array[i]);
                }
            }
            else
            {
                numericArray = array;
            }       
        }
        public Numeric[] GetArray()
        {
            return numericArray;
        }
        public int Length {
            get { return numericArray.Length; }
        }
        public Numeric this[int i]
        {
            get { return numericArray[i]; }
            set { numericArray[i] = value; }
        }
        private Numeric[] numericArray;
    }
}
