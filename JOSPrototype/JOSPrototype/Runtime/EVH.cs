using JOSPrototype.Runtime.Operation;
using JOSPrototype.Runtime.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using JOSPrototype.Components;
using JOSPrototype.Optimization;

namespace JOSPrototype.Runtime
{
    class EVH: Party
    {
        public EVH(Program program)
            : base(PartyType.EVH)
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
                // constantly scan all codes
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
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var code in program.icList.GetCodes())
                {
                    //if(code is ICAssignment && ((ICAssignment)code).op != OperationType.Return)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("EVH: " + code);
                    //}
                    
                    Evaluate(code);
                    while (!program.evaluatedIC.ContainsKey(code)) { Thread.Sleep(1); }                
                    System.Diagnostics.Debug.WriteLineIf(program.vTable.ContainsKey(Config.watchVar), "e:" + program.vTable[Config.watchVar]);
                }
            }
        }

        private void Evaluate(object obj)
        {
            var code = (IntermediateCode)obj;
            if (code is ICAssignment)
            {
                ICAssignment ica = (ICAssignment)code;
                // create Operation instance
                ObjectHandle handle = Activator.CreateInstance("JOSPrototype", "JOSPrototype.Runtime.Operation." + ica.op + "OnEVH", false, 0, null, new object[] {this, code, program }, null, null);
                Operation.Operation opImpl = (Operation.Operation)handle.Unwrap();
                // execute the operation on EVH
                opImpl.Run();
            }
            else if(code is ICWhile)
            {
                ICWhile icw = (ICWhile)code;
                WhileOnEVH op = new WhileOnEVH(this, icw, program);
                op.Run();
            }
            else
            {
                ICIfElse icie = (ICIfElse)code;
                IfElseOnEVH op = new IfElseOnEVH(this, icie, program);
                op.Run();
            }
        }

        private Program program;
        private bool isOptimized;
    }
}
