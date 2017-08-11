namespace JOSPrototype
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Text;
    using System.Linq;

    struct Interval<T> where T : IComparable<T>
    {
        public Interval(T min, T max) { Min = min; Max = max; }
        public T Min { set; get; }
        public T Max { set; get; }
    }

    /// <summary>
    /// We assume all intervals do not overlap
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Range<T> where T : IComparable<T>
    {
        public Range(List<Interval<T>> intervals)
        {
            this.intervals = intervals;
            this.intervals.Sort(new RangeComparer());
        }
        /// <summary>
        /// intervals of the range
        /// </summary>
        public List<Interval<T>> intervals = new List<Interval<T>>();

        /// <summary>
        /// Presents the Range in readable format
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var entry in intervals)
            {
                sb.Append(String.Format("[{0} - {1}], ", entry.Min, entry.Max));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public Boolean IsValid()
        {
            foreach (var entry in intervals)
            {
                if (entry.Min.CompareTo(entry.Max) > 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Determines if the provided value is inside the range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public Boolean ContainsValue(T value)
        {
            return intervals.BinarySearch(new Interval<T>(value, value), new ValueInRangeComparer()) >= 0;
        }

        class ValueInRangeComparer : IComparer<Interval<T>>
        {
            public int Compare(Interval<T> x, Interval<T> y)
            {
                if (x.Max.CompareTo(y.Min) < 0)
                    return -1;
                else if (y.Max.CompareTo(x.Min) < 0)
                    return 1;
                else
                    return 0;
            }
        }
        class RangeComparer : IComparer<Interval<T>>
        {
            public int Compare(Interval<T> x, Interval<T> y)
            {
                if (x.Max.CompareTo(y.Min) <= 0)
                    return -1;
                else if (y.Max.CompareTo(x.Min) <= 0)
                    return 1;
                else
                    return 0;
            }
        }
    }

    static class Utility 
    {
        // generate random integer
        public static int NextInt()
        {
            return rnd.Next();
        }
        // generate random double
        public static double NextDouble()
        {
            return rnd.NextDouble();
        }
        // generate random integer between [0, max - 1] 
        public static int NextInt(int max)
        {
            return rnd.Next(max);
        }
        // generate random Numeric with key length
        public static Numeric  NextUnsignedNumeric(byte scaleBits)
        {
            return rnd.NextUnsignedNumeric(scaleBits);
        }
        public static Numeric NextUnsignedNumeric(this Random rnd, byte scaleBits)
        {
            var buffer = new byte[Config.KeyBytes];
            rnd.NextBytes(buffer);
            Numeric re = new Numeric(buffer, scaleBits);
            //Console.WriteLine(re.GetUnsignedBigInteger());
            return re;
        }
        // generate random Numeric with specified length(leading bits are 0)
        public static Numeric NextUnsignedNumeric(byte scaleBits, int bitLen)
        {
            return rnd.NextUnsignedNumeric(scaleBits, bitLen);
        }
        // generate random Numeric within specified range(leading bits are 0)
        // i.e. logMin = 4, key:[2 ^ 4, 2 ^ 5)
        public static Numeric NextUnsignedNumericInRange(byte scaleBits, int logMin)
        {
            return rnd.NextUnsignedNumeric(scaleBits, logMin) + new Numeric(BigInteger.Pow(2, logMin), scaleBits);
        }
        public static Numeric NextUnsignedNumeric(this Random rnd, byte scaleBits, int bitLen)
        {
            var buffer = new byte[Config.KeyBytes];
            rnd.NextBytes(buffer);
            Numeric re = new Numeric(buffer, scaleBits).ModPow(bitLen);
            //Console.WriteLine(re.GetUnsignedBigInteger());
            return re;
        }
        // generate random Numeric with specified length(leading bits are 0 if sign bit is 0, otherwise 1)
        public static Numeric NextSignedNumeric(byte scaleBits, int bitLen)
        {
            return rnd.NextSignedNumeric(scaleBits, bitLen);
        }
        public static Numeric NextSignedNumeric(this Random rnd, byte scaleBits, int bitLen)
        {
            var randomNum = rnd.NextUnsignedNumeric(scaleBits);
            return randomNum.ModPow(bitLen, true);
        }

        // convert string to bytes stream
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        // convert bytes stream to string
        public static string ToString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        /// <summary>
        /// extention method for Array<> object to get subarray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">input array</param>
        /// <param name="startIndex">the start index from which to start copying</param>
        /// <returns>if startIndex is out of range, return null, otherwise reuturn a subarray from the 
        /// startIndex to the end</returns>
        public static T[] SubArray<T>(this T[] source, int startIndex)
        {
            if (startIndex < 0 || startIndex >= source.Length)
                return null;
            if (startIndex == 0)
                return source;
            T[] result = new T[source.Length - startIndex];
            Array.Copy(source, startIndex, result, 0, source.Length - startIndex);
            return result;
        }

        /// <summary>
        /// extention method for Array<> object to get subarray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">input array</param>
        /// <param name="startIndex">the index from which to start copying</param>
        /// <param name="length">the length of subarray</param>
        /// <returns>if startIndex is out of range or lenght is less than zero or startIndex + length is out of range, return null, 
        /// otherwise reuturn a subarray from the startIndex to the end</returns>
        public static T[] SubArray<T>(this T[] source, int startIndex, int length)
        {
            if (startIndex < 0 || length < 0 || startIndex + length > source.Length)
                return null;
            if (length == source.Length)
                return source;
            T[] result = new T[length];
            Array.Copy(source, startIndex, result, 0, length);
            return result;
        }
        // get the binary representation of a big integer
        public static string ToBinaryString(this BigInteger bigint, int length)
        {
            // Create a StringBuilder having appropriate capacity.
            var base2 = new StringBuilder();
            BigInteger one = BigInteger.Pow(2, length - 1);
            for (int i = 0; i < length; ++i)
            {
                base2.Append((bigint & one).IsZero ? 0 : 1);
                one >>= 1;
            }

            return base2.ToString();
        }

        public static void ResetSeed()
        {
            Random rnd = new Random();
            Utility.rnd = new Random(seeds[rnd.Next() % seeds.Length]);
        }

        static Utility()
        {
            if (noSeed)
            {
                rnd = new Random(0);
            }
            else
            {
                Random rnd = new Random();
                Utility.rnd = new Random(seeds[rnd.Next() % seeds.Length]);
            }
        }
        private static readonly int[] seeds = new int[] { 47111, 939971, 904371, 17211, 7431 };
        private static bool noSeed = true;
        private static Random rnd = null;
    }
}
