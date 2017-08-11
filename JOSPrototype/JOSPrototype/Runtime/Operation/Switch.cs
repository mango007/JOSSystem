//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using JOSPrototype.Components;
//using JOSPrototype.Runtime.Encryption;
//using JOSPrototype.Runtime.Network;
//using System.Numerics;

//namespace JOSPrototype.Runtime.Operation
//{
//    class Switch : Operation
//    {
//        public Switch() : base(EncryptionType.AddMod, EncryptionType.AddMod) { }
//        public override void OnEVH(Party party, ICAssignment code, Program program)
//        {
//            Numeric
//                enckaa = program.GetValue(code.operand1);
//            var encVal = new Numeric[] { TransformEncType(enckaa, party, code.index) };
//            var enckf = OnEVHSwitch(party, encVal[0], code.index, code.result.name);
//            enckf.SetEncType(resultEncType);
//            program.SetValue(code.result, enckf);
//        }

//        public override void OnHelper(Party party, int line)
//        {
//            OnHelperSwitch(party, line);
//        }

//        public override void OnKH(Party party, ICAssignment code, Program program)
//        {
//            Numeric
//                ka = program.GetValue(code.operand1);
//            var key = new Numeric[] { TransformEncType(ka, party, code.index) };
//            var kf = OnKHSwitch(party, key[0], code.index, code.result.name);
//            kf.SetEncType(resultEncType);
//            program.SetValue(code.result, kf);
//        }

//        private static void shuffleArr(long[] array)
//        {
//            int n = array.Length;
//            while (n > 1)
//            {
//                int k = Utility.NextInt(n--);
//                var temp = array[n];
//                array[n] = array[k];
//                array[k] = temp;
//            }
//        }

//        private static void permuteAndOff(out Numeric off, Numeric[] vals, Numeric[] pevals, Numeric[] pkvals, Numeric[] eivals)
//        {
//            //encrypt x with random offset o1
//            off = Utility.NextUnsignedNumeric(0);
//            long ncases = vals.Length;
//            Numeric[] kc1 = new Numeric[ncases];
//            Numeric[] ekc1 = new Numeric[ncases];
//            long[] ivals = new long[ncases];
//            //Prepate permutation and double encrypt encrypted values 
//            for (int j = 0; j < ncases; j++)
//            {
//                ivals[j] = j; //prepare array for permutation       

//                Numeric k = Utility.NextUnsignedNumeric(0);  //double encrypt   
//                kc1[j] = k;
//                ekc1[j] = vals[j] + k;
//            }
//            shuffleArr(ivals); //permute indexvals, eg. 0,1,2,3,4...  => 4,1,3,2,0                
//            for (int j = 0; j < ncases; j++) //permute casevals with same permutation, eg. 100, 101, 102, 103... using 4,1,3,2,0... => 104,101,103,102,100...
//            {
//                pevals[j] = ekc1[ivals[j]];
//                pkvals[j] = kc1[ivals[j]];
//                //eivals[j] = new Numeric(((ivals[j] + off.ToUnsignedBigInteger()) & Numeric.oneMaskMap[GlobalSetting.KeyBitLength]) % ncases, 0);
//                eivals[j] = off + new Numeric(ivals[j], 0);
//                // Console.WriteLine(" Dec: eivals" + eivals[j]+ "  " + (pevals[j] - pkvals[j]));
//            }
//        }

//        public static Numeric OnEVHSwitch(Party party, Numeric ex, int line, string name)
//        {
//            Numeric[] ecasevals;
//            if (name == "min"){ ecasevals = ecasevalsMin;}
//            else if(name == "max"){ ecasevals = ecasevalsMax; }
//            else { ecasevals = ecasevalsF; }

//            Numeric[] eivals = new Numeric[ncases];
//            Numeric[] pevals = new Numeric[ncases];
//            Numeric[] pkvals = new Numeric[ncases];

//            Numeric o1;
//            permuteAndOff(out o1, ecasevals, pevals, pkvals, eivals);
//            Numeric kxo1 = Utility.NextUnsignedNumeric(0);
//            Numeric exo1 = ex + o1 + kxo1;
            

//            List<Numeric> temp = new List<Numeric>();
//            temp.Add(exo1);temp.AddRange(pevals); temp.AddRange(pkvals); temp.AddRange(eivals);
//            var toHelper = Message.AssembleMessage(line, OperationType.Switch, true, temp.ToArray());
//            party.sender.SendTo(PartyType.Helper, toHelper);

//            var toKH = Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { kxo1 });
//            party.sender.SendTo(PartyType.KH, toKH);

//            var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
//            var kx2 = Message.DisassembleMessage(fromKH)[0];

//            var ex_plus_kx2 = ex + kx2;
//            toHelper = Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { ex_plus_kx2 });
//            party.sender.SendTo(PartyType.Helper, toHelper);

//            var fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
//            var rese1 = Message.DisassembleMessage(fromHelper)[0];

//            fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
//            var resk2 = Message.DisassembleMessage(fromHelper)[0];

//            return rese1 + resk2;
//        }

//        private static void helper(Numeric ex, Numeric k, Numeric[] pevals, Numeric[] pkvals, Numeric[] eivals, out Numeric rese1, out Numeric resk1)
//        {
//            Numeric xo1 = ex - k;  //HE gets key and encrypted value
//            long ncases = pevals.Length;
//            //xo1 = xo1 % ncases;
//            int pos;
//            for (pos = 0; pos < ncases; pos++) //find index of  case value                
//                if (eivals[pos].GetUnsignedBigInteger() == xo1.GetUnsignedBigInteger()) break;
//            rese1 = pevals[pos]; //return encrypted case value to EVH
//            resk1 = pkvals[pos]; //return encrypted key to KH
//        }

//        public static void OnHelperSwitch(Party party, int line)
//        {
//            var fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
//            var exo1_pevals_pkvals_eivals = Message.DisassembleMessage(fromEVH);
//            System.Diagnostics.Debug.Assert(exo1_pevals_pkvals_eivals.Length == 37);
//            var exo1 = exo1_pevals_pkvals_eivals[0];
//            var pevals = exo1_pevals_pkvals_eivals.SubArray(1, 12);
//            var pkvals = exo1_pevals_pkvals_eivals.SubArray(13, 12);
//            var eivals = exo1_pevals_pkvals_eivals.SubArray(25, 12);

//            var fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
//            var kxo1_plus_kx = Message.DisassembleMessage(fromKH)[0];

//            Numeric rese1 = 0, resk1 = 0;
//            helper(exo1, kxo1_plus_kx, pevals, pkvals, eivals, out rese1, out resk1);
//            party.sender.SendTo(PartyType.EVH, Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { rese1 }));
//            party.sender.SendTo(PartyType.KH, Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { resk1 }));

//            fromKH = party.receiver.ReceiveFrom(PartyType.KH, line);
//            var kxo2_pevals_pkvals_eivals = Message.DisassembleMessage(fromKH);

//            var kxo2 = kxo2_pevals_pkvals_eivals[0];
//            pevals = kxo2_pevals_pkvals_eivals.SubArray(1, 12);
//            pkvals = kxo2_pevals_pkvals_eivals.SubArray(13, 12);
//            eivals = kxo2_pevals_pkvals_eivals.SubArray(25, 12);

//            fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
//            var ex_plus_kx2 = Message.DisassembleMessage(fromEVH)[0];

//            Numeric rese2 = 0, resk2 = 0;
//            helper(ex_plus_kx2, kxo2, pevals, pkvals, eivals, out rese2, out resk2);

//            party.sender.SendTo(PartyType.EVH, Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { resk2 }));
//            party.sender.SendTo(PartyType.KH, Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { rese2 }));

//        }
//        public static Numeric OnKHSwitch(Party party, Numeric kx, int line, string name)
//        {
//            Numeric[] kcasevals;
//            if (name == "min") { kcasevals = kcasevalsMin; }
//            else if (name == "max") { kcasevals = kcasevalsMax; }
//            else { kcasevals = kcasevalsF; }

//            var fromEVH = party.receiver.ReceiveFrom(PartyType.EVH, line);
//            var kxo1 = Message.DisassembleMessage(fromEVH)[0];

//            var kxo1_plus_kx = kxo1 + kx;
//            var toHelper = Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { kxo1_plus_kx });
//            party.sender.SendTo(PartyType.Helper, toHelper);

//            Numeric[] eivals = new Numeric[ncases];
//            Numeric[] pevals = new Numeric[ncases];
//            Numeric[] pkvals = new Numeric[ncases];

//            Numeric o2 = 0;
//            permuteAndOff(out o2, kcasevals, pevals, pkvals, eivals);
//            Numeric kx2 = Utility.NextUnsignedNumeric(0);
//            Numeric kxo2 = kx - o2 + kx2;

//            List<Numeric> temp = new List<Numeric>();
//            temp.Add(kxo2); temp.AddRange(pevals); temp.AddRange(pkvals); temp.AddRange(eivals);
//            toHelper = Message.AssembleMessage(line, OperationType.Switch, false, temp.ToArray());
//            party.sender.SendTo(PartyType.Helper, toHelper);

//            var toEVH = Message.AssembleMessage(line, OperationType.Switch, false, new Numeric[] { kx2 });
//            party.sender.SendTo(PartyType.EVH, toEVH);

//            var fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
//            var resk1 = Message.DisassembleMessage(fromHelper)[0];

//            fromHelper = party.receiver.ReceiveFrom(PartyType.Helper, line);
//            var rese2 = Message.DisassembleMessage(fromHelper)[0];

//            return resk1 + rese2;
//        }

//        private static int ncases = 12;
//        private static Numeric[] casevalsMin, kcasevalsMin, ecasevalsMin, casevalsMax, kcasevalsMax, ecasevalsMax, casevalsF , kcasevalsF, ecasevalsF;
//        static Switch()
//        {
//            casevalsMin = new Numeric[] { -25, -30, -15, -5, 10, 15, 20, 15, 5, 0, -5, -15 };
//            casevalsMax = new Numeric[] { -15, -15, 5, 15, 20, 25, 35, 25, 15, 10, 5, -5};
//            casevalsF = new Numeric[] { -1, -1, 1, 1, 1, 1, 1, -1, -1, -1, -1, -1 };
//            kcasevalsMin = new Numeric[12];
//            ecasevalsMin = new Numeric[12];
//            kcasevalsMax = new Numeric[12];
//            ecasevalsMax = new Numeric[12];
//            kcasevalsF = new Numeric[12];
//            ecasevalsF = new Numeric[12];
//            for (int i = 0; i < 12; i++)
//            {
//                kcasevalsMin[i] = Utility.NextUnsignedNumeric(0);
//                ecasevalsMin[i] = casevalsMin[i] + kcasevalsMin[i];
//                kcasevalsMax[i] = Utility.NextUnsignedNumeric(0);
//                ecasevalsMax[i] = casevalsMax[i] + kcasevalsMax[i];
//                kcasevalsF[i] = Utility.NextUnsignedNumeric(0);
//                ecasevalsF[i] = casevalsF[i] + kcasevalsF[i];
//            }
//        }
//    }
//}
