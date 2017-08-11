using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using JOSPrototype.Runtime.Operation;

namespace JOSPrototype
{
    enum DependencyType { Flow, Output, Anti, Any }
    enum PartyType : byte { Client = 0, EVH = 1, KH = 2, Helper = 3 }
    enum OperationType: short { Return, None, Addition, Substraction, Multiplication, XOR, AND, OR, NOT, XORToAddMod, AddModToXOR, XORToAdd, AddModToAdd,
        HammingDistance, EqualZero, LessZero, MultiLessZero, Sin, IndexMSB, Inverse, FastEqualZero, Switch, IfElse, While, Reveal, Conceal }
    enum EncryptionType: byte { Any = 0x1, AddMod = 0x2, XOR = 0x4, None = 0x8}
    enum SubdomainPrivacy { None, STATDOM, DOM };
    enum Port { Client = 10000, EVH = 10001, KH = 10002, Helper = 10003 }
    static class Config
    {
        public static string watchVar;
        public const int Simulated_Network_Latency = 100;//in ms
        public static EncryptionType DefaultEnc = EncryptionType.AddMod;
        // max number of thread in thread pool
        public const int MaxThreads = 14;

        // key length (the maximum integer bit length as well), length must be a multiple of 8
        public static int KeyBits = 64;
        public static int EffectiveKeyBits = 61;
        // length of bits storing a fixed-point number
        public static int NumericBits = 50;
        public static byte ScaleBits =  10;
        //public static int IntegerBits = NumericBits - ScaleBits;       

        public static int KeyBytes = KeyBits / 8;

        public static bool isOptimized = true;
        // party sends massages when a certain number of messages is reached 
        // or the threshold time between two scannings is reached
        //public const int Runtime_Network_BufferMessageThresholdClient = 5;
        public static int Runtime_Network_BufferMessageThresholdEVH = 5;
        public static int Runtime_Network_BufferMessageThresholdKH = 5;
        public static int Runtime_Network_BufferMessageThresholdHelper = 5;
        public static int Runtime_Network_ScanPeriod = 5; // millisecond
        public static int Runtime_Network_TimeThreshold = 100; // millisecond

        public static Dictionary<PartyType, IPEndPoint> partyAddress = new Dictionary<PartyType, IPEndPoint>();

        static Config()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            partyAddress.Add(PartyType.Client, new IPEndPoint(ipAddress, (int)Port.Client));
            partyAddress.Add(PartyType.EVH, new IPEndPoint(ipAddress, (int)Port.EVH));
            partyAddress.Add(PartyType.KH, new IPEndPoint(ipAddress, (int)Port.KH));
            partyAddress.Add(PartyType.Helper, new IPEndPoint(ipAddress, (int)Port.Helper));
            ThreadPool.SetMaxThreads(MaxThreads, 0);
            Numeric.SetParameters();
            Inverse.SetParameters();
        }

        public static void SetGlobalParameters(int keyLen, int numericBits, byte scaleBits, bool isop)
        {
            // EffectiveKeyBits is always larger than NumericBitLength
            System.Diagnostics.Debug.Assert(keyLen % 8 == 0);
            //System.Diagnostics.Debug.Assert(keyLen >= numericBits + 4);
            KeyBits = keyLen;
            EffectiveKeyBits = keyLen - 3;
            KeyBytes = keyLen / 8;
            NumericBits = numericBits;
            ScaleBits = scaleBits;
            //IntegerBits = numericBits > scaleBits ? numericBits - scaleBits : 0;
            isOptimized = isop;
            if(!isOptimized)
            {
                Runtime_Network_BufferMessageThresholdEVH = 1;
                Runtime_Network_BufferMessageThresholdKH = 1;
                Runtime_Network_BufferMessageThresholdHelper = 1;
                Runtime_Network_TimeThreshold = 0;
            }
            Numeric.SetParameters();
            Inverse.SetParameters();
        }
    }


}
