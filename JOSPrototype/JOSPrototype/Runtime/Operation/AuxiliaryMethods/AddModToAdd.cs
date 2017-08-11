using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class AddModToAddOnEVH : OperationOnEVH
    {
        public AddModToAddOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.AddModToAdd)
        { }
        int parallism;
        NumericArray enc_minus_newKey_key = new NumericArray();
        protected override void OnEVH()
        {
            switch(step)
            {
                case 1:
                    parallism = encVal.Length;
                    party.receiver.ReceiveFrom(PartyType.KH, line, this, enc_minus_newKey_key);
                    break;
                case 2:
                    var enc_newKey_a = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        enc_newKey_a[p] = encVal[p] - enc_minus_newKey_key[p];
                        // System.Diagnostics.Debug.WriteLine(enc_newKey_a[p].GetSignedBigInteger());
                        System.Diagnostics.Debug.Assert(enc_newKey_a[p].GetSignedBigInteger() > 0);
                    }
                    result.SetArray(enc_newKey_a);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
            //parallism = encVal.Length;
            //var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
            //var enc_minus_paddedKey_key = Message.DisassembleMessage(fromKH);
            //var enc_paddedKey_a = new Numeric[parallism];
            //for (int p = 0; p < parallism; ++p)
            //{
            //    enc_paddedKey_a[p] = encVal[p] - enc_minus_paddedKey_key[p];
            //}
            //return enc_paddedKey_a;
        }
    }
    class AddModToAddOnKH : OperationOnKH
    {
        public AddModToAddOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
            : base(party, line, caller, operands, result, OperationType.AddModToAdd)
        {
            // isArray = true;
            // signedArray = signed;
        }

        //public AddModToAddOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result)
        //    : base(party, line, caller, operands, result)
        //{
        //    isArray = false;
        //    // this.signed = signed;
        //}
        //bool isArray;
        // int[] lenArray;
        // bool[] signedArray;
        // int len;
        // bool signed;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    int parallism = key.Length;
                    var newKey = new Numeric[parallism];
                    var enc_minus_newKey_key = new Numeric[parallism];
                    for (int p = 0; p < parallism; ++p)
                    {
                        newKey[p] = Utility.NextUnsignedNumericInRange(key[p].GetScaleBits(), Config.EffectiveKeyBits);
                        System.Diagnostics.Debug.Assert(newKey[p].GetSignedBigInteger() > 0);                        
                        //if (isArray)
                        //{
                        //    paddedKey[p] = Utility.NextUnsignedNumeric(key[p].GetScaleBits(), lenArray[p]);
                        //}
                        //else
                        //{
                        //    if (signed)
                        //        paddedKey[p] = Utility.NextSignedNumeric(key[p].GetScaleBits(), len);
                        //    else
                        //        paddedKey[p] = Utility.NextUnsignedNumeric(key[p].GetScaleBits(), len);
                        //}

                        enc_minus_newKey_key[p] = key[p] - newKey[p];
                    }
                    var toEVH = Message.AssembleMessage(line, opType, false, enc_minus_newKey_key);
                    party.sender.SendTo(PartyType.EVH, toEVH);
                    result.SetArray(newKey);
                    caller.Run();
                    break;
                default:
                    throw new Exception();
            }
            
        }
    }
}
