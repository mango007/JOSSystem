using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Components;
using JOSPrototype.Runtime.Network;

namespace JOSPrototype.Runtime.Operation
{
    class EqualZeroOnEVH : OperationOnEVH
    {
        public EqualZeroOnEVH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.EqualZero)
        {
            this.length = length;
        }
        public EqualZeroOnEVH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.XOR, OperationType.EqualZero)
        {
            // since EqualZero does not care about the input operand encrption type,
            // it will not call TransformEncType function. So we need to assign value to encVal/key explicitly here
            encVal = new NumericArray(program.GetValue(code.operand1));
        }
        public EqualZeroOnEVH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.Any, EncryptionType.XOR, caller, OperationType.EqualZero)
        {
            encVal = new NumericArray(program.GetValue(code.operand1));
        }
        int length, parallelism;
        NumericArray encH = new NumericArray();
        EncryptionType encType;
        protected override void OnEVH()
        {
            switch (step)
            {
                case 1:
                    parallelism = encVal.Length;
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric enckaa = program.GetValue(code.operand1);
                        encVal = new NumericArray(enckaa);
                        length = Config.NumericBits;
                        if(encVal[0].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            if (encVal[0].GetUnsignedBigInteger() == 0)
                            {                             
                                encH = new NumericArray(new Numeric(1, 0));
                            }
                            else
                            {
                                encH = new NumericArray(new Numeric(0, 0));
                            }
                            // jump to round 4
                            step = 3;
                            Run();
                            break;
                        }
                    }               
                    new HammingDistanceOnEVH(party, line, this, encVal, encH, length).Run();
                    break;
                case 2:
                    new FastEqualZeroOnEVH(party, line, this, encH, encH, (int)Math.Ceiling(Math.Log(length + 1, 2))).Run();
                    break;
                case 3:
                    // as subfunction, the result should be 0 if secret == 0
                    // however the result from FastEqualZero if 1 if secret == 0
                    if (ReferenceEquals(code, null))
                    {
                        for(int p = 0; p < parallelism; p++)
                        {
                            encH[p] ^= new Numeric(1, 0);
                        }
                    }
                    Run();
                    break;
                case 4:
                    SetResult(encType, encH.GetArray());
                    break;
                case 5:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
                    ////System.Diagnostics.Debug.Assert(le >= 2 || le == 0);
                    //if (le > 2)
                    //{
                    //    le = (int)Math.Ceiling(Math.Log(le + 1, 2));
                    //    new HammingDistanceOnEVH(party, line, this, encH, encH, le).Run();
                    //}
                    //else if (le == 2)
                    //{
                    //    var OROperand = new Numeric[2 * parallelism];
                    //    for (int p = 0; p < parallelism; ++p)
                    //    {
                    //        OROperand[2 * p] = encH[p] >> 1;
                    //        OROperand[2 * p + 1] = encH[p].ModPow(1);
                    //    }
                    //    le = 0;
                    //    new OROnEVH(party, line, this, new NumericArray(OROperand), encH).Run();
                    //}
                    //else
                    //{
                    //    Numeric[] enckf = new Numeric[parallelism];
                    //    for (int p = 0; p < parallelism; ++p)
                    //    {
                    //        enckf[p] = encH[p].ModPow(1);
                    //        //enckf[p].SetEncType(EncryptionType.XOR);
                    //    }
                    //    if(!ReferenceEquals(code, null))
                    //    {
                    //        // not as a subfunction
                    //        enckf[0] ^= new Numeric(1, 0);
                    //    }
                    //    SetResult(enckf);
                    //}
                    //break;
            }
        }
    }
    class EqualZeroOnKH: OperationOnKH
    {
        public EqualZeroOnKH(Party party, int line, Operation caller, NumericArray operands, NumericArray result, int length)
            : base(party, line, caller, operands, result, OperationType.EqualZero)
        {
            this.length = length;
        }
        public EqualZeroOnKH(Party party, ICAssignment code, Program program)
            : base(party, code, program, EncryptionType.Any, EncryptionType.XOR, OperationType.EqualZero)
        {
            key = new NumericArray(program.GetValue(code.operand1));
        }
        public EqualZeroOnKH(Party party, ICAssignment code, Program program, Operation caller)
            : base(party, code, program, EncryptionType.Any, EncryptionType.XOR, caller, OperationType.EqualZero)
        {
            key = new NumericArray(program.GetValue(code.operand1));
        }
        int length, parallelism;
        NumericArray keyH = new NumericArray();
        EncryptionType encType;
        protected override void OnKH()
        {
            switch(step)
            {
                case 1:
                    encType = resultEncType;
                    if (!ReferenceEquals(code, null))
                    {
                        Numeric ka = program.GetValue(code.operand1);
                        key = new NumericArray(ka);
                        length = Config.NumericBits;
                        if(key[0].GetEncType() == EncryptionType.None)
                        {
                            encType = EncryptionType.None;
                            if (key[0].GetEncType() == EncryptionType.None)
                            {
                                encType = EncryptionType.None;
                                if (key[0].GetUnsignedBigInteger() == 0)
                                {
                                    keyH = new NumericArray(new Numeric(1, 0));
                                }
                                else
                                {
                                    keyH = new NumericArray(new Numeric(0, 0));
                                }
                                // jump to round 3
                                step = 2;
                                Run();
                                break;
                            }
                        }
                    }
                    parallelism = key.Length;
                    //Console.WriteLine("KH: " + key[0]);
                    new HammingDistanceOnKH(party, line, this, key, keyH, length).Run();
                    break;
                case 2:
                    new FastEqualZeroOnKH(party, line, this, keyH, keyH, (int)Math.Ceiling(Math.Log(length + 1, 2))).Run();
                    break;
                case 3:
                    SetResult(encType, keyH.GetArray());
                    break;
                case 4:
                    InvokeCaller();
                    break;
                default:
                    throw new Exception();
                    ////System.Diagnostics.Debug.Assert(le >= 2 || le == 0);
                    //if (le > 2)
                    //{
                    //    le = (int)Math.Ceiling(Math.Log(le + 1, 2));
                    //    new HammingDistanceOnKH(party, line, this, keyH, keyH, le).Run();
                    //}
                    //else if (le == 2)
                    //{
                    //    var OROperand = new Numeric[2 * parallelism];
                    //    for (int p = 0; p < parallelism; ++p)
                    //    {
                    //        OROperand[2 * p] = keyH[p] >> 1;
                    //        OROperand[2 * p + 1] = keyH[p].ModPow(1);
                    //    }
                    //    le = 0;
                    //    new OROnKH(party, line, this, new NumericArray(OROperand), keyH).Run();
                    //}
                    //else
                    //{
                    //    Numeric[] kf = new Numeric[parallelism];
                    //    for (int p = 0; p < parallelism; ++p)
                    //    {
                    //        kf[p] = keyH[p].ModPow(1);
                    //        //kf[p].SetEncType(EncryptionType.XOR);
                    //    }
                    //    SetResult(kf);
                    //}
                    //break;
            }
        }
    }
}
