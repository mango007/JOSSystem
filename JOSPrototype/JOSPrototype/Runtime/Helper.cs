using JOSPrototype.Components;
using JOSPrototype.Optimization;
using JOSPrototype.Runtime.Network;
using JOSPrototype.Runtime.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;

namespace JOSPrototype.Runtime
{
    class Helper: Party
    {
        public Helper(): base(PartyType.Helper){}

        public override void RunParty()
        {
            throw new NotImplementedException();
        }

        public void RunParty(int line, OperationType op)
        {
            ThreadPool.QueueUserWorkItem(Evaluate, new HelperThreadState(line, op));
            //Thread thread = new Thread(() => Evaluate(line, op));
            //thread.Name = "Helper_" + line;
            //thread.Start();
        }

        private class HelperThreadState
        {
            public int Line { get; private set; }
            public OperationType Operation { get; private set; }
            public HelperThreadState(int line, OperationType op)
            {
                Line = line;
                Operation = op;
            }
        }

        private void Evaluate(Object state)
        {
            var helperState = (HelperThreadState)state;
            // create OperationImpl instance
            ObjectHandle handle = Activator.CreateInstance("JOSPrototype", "JOSPrototype.Runtime.Operation." + helperState.Operation + "OnHelper", false, 0, null, new object[] { this, helperState.Line }, null, null);
            Operation.Operation opImpl = (Operation.Operation)handle.Unwrap();
            // execute the operation on Helper
            opImpl.Run();
        }
    }
}
