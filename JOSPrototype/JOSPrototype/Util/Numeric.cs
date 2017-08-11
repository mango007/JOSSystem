using System;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;

namespace JOSPrototype
{
    [Serializable]
    sealed class Numeric: IEquatable<Numeric>
    {
        // Size of a Numeric = size of key + size of scaling bits(size of int) + size of EncType(1 byte)
        public static int Size() { return Config.KeyBytes + sizeof(byte) + sizeof(byte); }
        public static Dictionary<int, BigInteger> oneMaskMap;
        public static Dictionary<int, BigInteger> signMaskMap;
        public static void SetParameters()
        {
            oneMaskMap = new Dictionary<int, BigInteger>();
            signMaskMap = new Dictionary<int, BigInteger>();
            oneMask = BigInteger.One;
            signMask = BigInteger.One;
            oneMaskMap.Add(0, BigInteger.Zero);
            oneMaskMap.Add(1, oneMask);       
            signMaskMap.Add(1, signMask);
            for (int i = 1; i < Config.KeyBits; ++i)
            {
                oneMask = (oneMask << 1) + 1;
                oneMaskMap.Add(i + 1, oneMask);
                signMask <<= 1;
                signMaskMap.Add(i + 1, signMask);
            }
        }
        // example: 1       => new Numeric(1, 0)
        //          0.5     => new Numeric(1, 1)
        //          0.75    => new Numeric(3, 2)
        // Numeric value := val / 2^(scaleBits)
        // Numeric uses two's complement to encode negative number, 
        // the bit length of Numeric is key length
        public Numeric(int val, byte scaleBits)
        {
            this.scaleBits = scaleBits;
            storage = val & oneMask;
        }
        public Numeric(string val, byte scaleBits)
        {           
            this.scaleBits = scaleBits;
            BigInteger bigi;
            bool canParse = BigInteger.TryParse(val, out bigi);
            Debug.Assert(canParse);
            storage = bigi & oneMask;
        }
        public Numeric(BigInteger val, byte scaleBits)
        {
            this.scaleBits = scaleBits;
            this.storage = val & oneMask;
        }
        // generate Numeric from byte array
        public Numeric(byte[] val, byte scaleBits)
        {
            System.Diagnostics.Debug.Assert(val.Length == Config.KeyBytes);
            this.scaleBits = scaleBits;
            this.storage = (new BigInteger(val)) & oneMask;
        }
        // copy constructor
        public Numeric(Numeric val)
        {
            storage = val.storage;
            //Bits = val.Bits;
            scaleBits = val.scaleBits;
            encType = val.encType;
        }
        public static implicit operator Numeric(int val)
        {
            return new Numeric(val, 0);
        }

        // do not create a new instance of Numeric
        public void Copy(Numeric val)
        {
            storage = val.storage;
            //Bits = val.Bits;
            scaleBits = val.scaleBits;
            encType = val.encType;
        }
        // for operations with two operands, scaling bits of the two operands should be equal
        // otherwise, one of them is 0
        public static Numeric operator +(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            return new Numeric(a.storage + b.storage, (a.scaleBits == 0) ? b.scaleBits: a.scaleBits);       
        }
        public static Numeric operator -(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            return new Numeric(a.storage - b.storage, (a.scaleBits == 0) ? b.scaleBits : a.scaleBits);
        }
        // the case that two operands both have non-zero scale bits can not happen in protocol, only for testing  
        public static Numeric operator *(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            if(a.scaleBits == b.scaleBits && a.scaleBits != 0)
            {
                return new Numeric((BigInteger)(a.GetVal() * b.GetVal() * Math.Pow(2, a.scaleBits)), a.scaleBits);
            }
            else
            {
                return new Numeric(a.storage * b.storage, (a.scaleBits == 0) ? b.scaleBits : a.scaleBits);
            } 
        }
        public static Numeric operator ^(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            return new Numeric(a.storage ^ b.storage, (a.scaleBits == 0) ? b.scaleBits : a.scaleBits);
        }
        public static Numeric operator &(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            return new Numeric(a.storage & b.storage, (a.scaleBits == 0) ? b.scaleBits : a.scaleBits);
        }
        public static Numeric operator |(Numeric a, Numeric b)
        {
            System.Diagnostics.Debug.Assert(a.scaleBits == b.scaleBits || a.scaleBits == 0 || b.scaleBits == 0);
            return new Numeric(a.storage | b.storage, (a.scaleBits == 0) ? b.scaleBits : a.scaleBits);
        }
        public static Numeric operator ~(Numeric a)
        {           
            return new Numeric(~a.storage, a.scaleBits);
        }
        // the leading bits are fulfilled with 0
        public static Numeric operator >>(Numeric a, int shiftLen)
        {
            return new Numeric(a.storage >> shiftLen, a.scaleBits);
        }
        // the trailing bits are fulfilled with 0
        public static Numeric operator <<(Numeric a, int shiftLen)
        {
            return new Numeric(a.storage << shiftLen, a.scaleBits);
        }
        // for operations with two operands, scaleBits should be equal.
        // Scale method magnifies the operand with smaller scaleBits
        public static byte Scale(Numeric a, Numeric b)
        {
            if (a.scaleBits > b.scaleBits)
            {
                b.storage = (b.storage << (a.scaleBits - b.scaleBits)) & oneMask;
                b.scaleBits = a.scaleBits;
            }
            else if (a.scaleBits < b.scaleBits)
            {
                a.storage = (a.storage << (b.scaleBits - a.scaleBits)) & oneMask;
                a.scaleBits = b.scaleBits;
            }
            return a.GetScaleBits();
        }
        // cut out the least significant bits with specified length
        // if sighed is true, the leading bits equal to the (len - 1)th bits
        // else, the leading bits are 0
        public Numeric ModPow(int len, bool signed = false)
        {
            System.Diagnostics.Debug.Assert(len <= Config.KeyBits);
            if (!signed)
            {
                return new Numeric(
                    storage & oneMaskMap[len],
                    scaleBits);
            }
            else
            {
                var bi = storage & oneMaskMap[len];
                if ((bi & signMaskMap[len]) == 0)
                {
                    return new Numeric(
                        bi,
                        scaleBits);
                }
                else
                {
                    return new Numeric(
                        bi - oneMaskMap[len] - 1,
                        scaleBits);
                }
                
            }
        }
        // sum up the least significant bits with specified length
        public Numeric SumBits(int le)
        {
            BigInteger bi = storage, mask = BigInteger.One;
            int re = 0;
            for (int i = 0; i < le; ++i)
            {
                re += ((bi & mask) == 0) ? 0 : 1;
                mask <<= 1;
            }
            return new Numeric(re, 0);
        }
        public byte GetScaleBits() { return scaleBits; }
        public void SetScaleBits(byte scaleBits) { this.scaleBits = scaleBits; }
        public void ResetScaleBits() { scaleBits = 0; }
        public EncryptionType GetEncType() { return encType; }
        public void SetEncType(EncryptionType encType) { this.encType = encType; }
        // decode all bits(bit length of Numeric is Config.KeyBitLength)
        // the (Config.KeyBitLength - 1)th bit is the sign bit
        // interpret Numeric as a integer encoded with two's complement => val
        // return (val / 2 ^ scaleBits)
        public double GetVal()
        {
            if ((storage & signMaskMap[Config.KeyBits]) != 0)
            {
                return ((double)((storage & oneMaskMap[Config.KeyBits]) - oneMaskMap[Config.KeyBits] - 1)) / (Math.Pow(2, scaleBits));
            }
            else { return ((double)(storage & oneMaskMap[Config.KeyBits])) / (Math.Pow(2, scaleBits)); }
        }
        // decode the least significant bits with specified length
        // the (length - 1)th bit is the sign bit
        // interpret Numeric as a integer encoded with two's complement => val
        // return (val / 2 ^ scaleBits)
        public double GetVal(int length)
        {
            if ((storage & signMaskMap[length]) != 0)
            {
                return ((double)((storage & oneMaskMap[length]) - oneMaskMap[length] - 1)) / (Math.Pow(2, scaleBits));
            }
            else { return ((double)(storage & oneMaskMap[length])) / (Math.Pow(2, scaleBits)); }
        }
        public override string ToString() { return "Val: " + GetVal() + " ,Binary String: " + storage.ToBinaryString(Config.KeyBits) + ", Enc Type: " + encType; }
        public BigInteger GetUnsignedBigInteger() { return storage; } 
        public BigInteger GetSignedBigInteger()
        {
            if ((storage & signMask) != 0) { return storage - oneMask - 1; }
            else { return storage; }
        }
        public byte[] GetBytes()
        {
            byte[] re = new byte[Config.KeyBytes];
            var val = storage.ToByteArray();
            Array.Copy(val, 0, re, 0, Math.Min(val.Length, re.Length));
            return re;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Numeric);
        }

        public bool Equals(Numeric other)
        {
            return GetVal() == other.GetVal();
        }

        public static bool operator ==(Numeric lhs, Numeric rhs)
        {
            // Check for null on left side.
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            else if (Object.ReferenceEquals(rhs, null))
            {
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.GetVal() ==  rhs.GetVal();
        }

        public static bool operator !=(Numeric lhs, Numeric rhs)
        {
            return !(lhs == rhs);
        }
        //public Numeric PadSignBit(int length)
        //{
        //    if ((storage & signMaskMap[length]) != 0)
        //    {
        //        return new Numeric((storage & oneMaskMap[length]) - oneMaskMap[length] - 1, scaleBits);
        //    }
        //    else { return new Numeric (storage & oneMaskMap[length], scaleBits); }
        //}

        private static BigInteger oneMask;
        private static BigInteger signMask;
        private BigInteger storage;
        private byte scaleBits = 0;
        //public int Bits { get; set; } = Config.KeyBits;
        private EncryptionType encType = Config.DefaultEnc;
    }
}
