using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JOSPrototype.Runtime.Operation;
using System.Threading;
using JOSPrototype.Runtime.Network;
using System.Runtime.Remoting;
using JOSPrototype.Components;
using JOSPrototype.Optimization;

namespace JOSPrototype.Runtime
{
    class KH: Party
    {
        public KH(Program program)
            : base(PartyType.KH)
        {
            this.program = program;
        }
     
        public override void RunParty()
        {
            isOptimized = Config.isOptimized;
            if (isOptimized)
            {
                Optimizer optimizer = new Parallelizer();
                optimizer.Optimize(program);
                int icNum = program.icList.Count;
                while (icNum > program.evaluatedIC.Count)
                {
                    //for (int i = program.icList.Count - 1; i >= 0; --i)
                    //{
                    //    if (program.IsIndependent(program.icList[i]))
                    //    {
                    //        //code.hasBeenOrIsBeeningEvaluated = true;
                    //        ThreadPool.QueueUserWorkItem(Evaluate, program.icList[i]);
                    //        program.icList.RemoveAt(i);
                    //    }
                    //}
                    foreach (var code in program.icList.GetCodes())
                    {
                        if (code.hasBeenOrIsBeingEvaluated)
                        {
                            continue;
                        }
                        else
                        {
                            if (program.IsIndependent(code))
                            {
                                
                                code.hasBeenOrIsBeingEvaluated = true;
                                ThreadPool.QueueUserWorkItem(Evaluate, code);
                                //Thread thread = new Thread(() => Evaluate(code));
                                //thread.Name = "KH_" + code.index;
                                //thread.Start();
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var code in program.icList.GetCodes())
                {
                    //if (code is ICAssignment && ((ICAssignment)code).op != OperationType.Return)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("KH: " + code);
                    //}
                    Evaluate(code);
                    while (!program.evaluatedIC.ContainsKey(code)) { Thread.Sleep(1); }
                    System.Diagnostics.Debug.WriteLineIf(program.vTable.ContainsKey(Config.watchVar), "k: " + program.vTable[Config.watchVar]);
                }
            }
        }

        private void Evaluate(object obj)
        {
            var code = (IntermediateCode)obj;
            if (code is ICAssignment)
            {
                ICAssignment ica = (ICAssignment)code;
                // create OperationImpl instance
                ObjectHandle handle = Activator.CreateInstance("JOSPrototype", "JOSPrototype.Runtime.Operation." + ica.op + "OnKH", false, 0, null, new object[] { this, code, program }, null, null);
                Operation.Operation opImpl = (Operation.Operation)handle.Unwrap();
                // execute the operation on KH
                opImpl.Run();
            }
            else if(code is ICWhile)
            {
                ICWhile icw = (ICWhile)code;
                WhileOnKH op = new WhileOnKH(this, icw, program);
                op.Run();
            }
            else
            {
                ICIfElse icie = (ICIfElse)code;
                IfElseOnKH op = new IfElseOnKH(this, icie, program);
                op.Run();
            }
        }
        private Program program;
        private bool isOptimized;
    }
}
