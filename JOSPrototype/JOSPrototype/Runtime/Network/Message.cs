using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Runtime.Network
{
    // a class helps to process the sent or received messages
    internal static class Message
    {
        // the leading one byte indicates the type of sender
        public const int SenderPartyTypeLength = 1;
        // (size of short) indicate message length for one instruction
        public const int MsgLengthLength = sizeof(int);
        // (size of int) indicate the line number of current processed instruction 
        public const int LineLength = sizeof(int);
        // indicate the type of Operation
        public const int OpTypeLength = sizeof(short);
        // indicate if helper is invoked
        public const int invokeHelperLength = sizeof(bool);

        public const int HeadLength = LineLength + OpTypeLength + invokeHelperLength;
        // EOF symble
        public const int EOFLength = 4;
        private static byte[] EOF = new byte[EOFLength] { 0xFF, 0xFF, 0xFF, 0xFF };

        // check if the received message is complete
        public static bool ContainEOF(List<byte> data)
        {
            if (data.Count() < EOFLength)
            {
                return false;
            }
            for (int idxEOF = EOFLength - 1, idxData = data.Count() - 1; idxEOF >= 0; --idxEOF, --idxData)
            {
                if (data[idxData] != EOF[idxEOF])
                {
                    return false;
                }
            }
            return true;
        }

        public static void RemoveEOF(List<byte> data)
        {
            data.RemoveRange(data.Count() - EOFLength, EOFLength);
        }
        public static byte[] RemoveEOF(byte[] data)
        {
            return data.SubArray(0, data.Count() - EOFLength);
        }

        public static void AddEOF(List<byte> data)
        {
            data.AddRange(EOF);
        }

        private static string _EOP = "END";
        public static byte[] EOP = Utility.GetBytes(_EOP);
        public static readonly int EOPLength = EOP.Length;

        public static bool IsEndOfProgram(byte[] data)
        {
            return data.SequenceEqual(EOP);
        }

        public static byte[] Pack(PartyType from, List<byte[]> msgs)
        {
            List<byte> re = new List<byte>();
            re.Add((byte)from);
            foreach (var msg in msgs)
            {
                re.AddRange(BitConverter.GetBytes(msg.Length));
                re.AddRange(msg);
            }
            AddEOF(re);
            return re.ToArray();
        }
        // return a dictionary, where key is the line number and value is the remaining bytes for one instruction
        public static Dictionary<int, List<byte[]>> Unpack(out PartyType from, byte[] input)
        {
            int count = 0;
            from = (PartyType)input[count];
            count++;
            Dictionary<int, List<byte[]>> re = new Dictionary<int, List<byte[]>>();
            while (count != input.Count())
            {
                int msgLen = BitConverter.ToInt32(input, count);
                count += MsgLengthLength;
                int line = BitConverter.ToInt32(input, count);
                count += LineLength;
                var temp = new byte[msgLen - LineLength];
                Array.Copy(input, count, temp, 0, msgLen - LineLength);
                if (re.ContainsKey(line))
                {
                    re[line].Add(temp);
                }
                else
                {
                    re.Add(line, new List<byte[]> { temp });
                }                
                count += msgLen - LineLength;
            }
            return re;
        }

        /// <summary>
        /// copy bytes from components to toParty, 
        /// total length of all components should be exactly the same as the length of toParty
        /// </summary>
        /// <param name="toParty">destination array</param>
        /// <param name="components">source arrays</param>
        public static byte[] AssembleMessage(int line, OperationType opType, bool invokeHelper, Numeric [] components)
        {
            int totalLen = HeadLength + Numeric.Size() * components.Count(), count = 0;
            byte[] re = new byte[totalLen];
            Array.Copy(BitConverter.GetBytes(line), 0, re, count, Message.LineLength);
            count += sizeof(int);
            Array.Copy(BitConverter.GetBytes((short)opType), 0, re, count, Message.OpTypeLength);
            count += sizeof(short);
            Array.Copy(BitConverter.GetBytes(invokeHelper), 0, re, count, Message.invokeHelperLength);
            count += sizeof(bool);
            foreach (var entry in components)
            {
                re[count] = entry.GetScaleBits();
                count += sizeof(byte);
                re[count] = (byte)entry.GetEncType();
                count += sizeof(byte);
                Array.Copy(entry.GetBytes(), 0, re, count, Config.KeyBytes);
                count += Config.KeyBytes;
            }
            return re;
        }

        public static Numeric [] DisassembleMessage(byte[] msg)
        {
            Numeric [] re = new Numeric [msg.Count() / Numeric.Size()];
            int count = 0;
            for(int i = 0; i < re.Count(); ++i)
            {
                byte scaleBits = msg[count];
                count += sizeof(byte);
                EncryptionType encType = (EncryptionType)msg[count];
                count += sizeof(byte);
                re[i] = new Numeric(msg.SubArray(count, Config.KeyBytes), scaleBits);
                re[i].SetEncType(encType);
                count += Config.KeyBytes;
            }
            return re;
        }
    }
}
