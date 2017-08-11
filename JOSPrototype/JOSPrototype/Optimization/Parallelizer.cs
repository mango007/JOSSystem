using System.Collections.Generic;
using System.Linq;
using JOSPrototype.Components;
using System;

namespace JOSPrototype.Optimization
{
    class Parallelizer: Optimizer
    {
        private void AddDependency(IntermediateCode from, IntermediateCode to, DependencyType dt)
        {
            if (!from.outEdge.ContainsKey(to))
            {
                from.outEdge.Add(to, new HashSet<DependencyType>());
            }
            from.outEdge[to].Add(dt);
            if (!to.inEdge.ContainsKey(from))
            {
                to.inEdge.Add(from, new HashSet<DependencyType>());
            }
            to.inEdge[from].Add(dt);
        }
        private bool IsDependent(IntermediateCode codei, IntermediateCode codej)
        {
            if(codei is ICAssignment && codej is ICAssignment)
            {
                ICAssignment assignmenti = (ICAssignment)codei, assignmentj = (ICAssignment)codej;
                if (ETerminal.IsEqual(assignmenti.result, assignmentj.result)   ||
                    ETerminal.IsEqual(assignmentj.result, assignmenti.operand1) ||
                    ETerminal.IsEqual(assignmentj.result, assignmenti.operand2) ||
                    ETerminal.IsEqual(assignmenti.result, assignmentj.operand1) ||
                    ETerminal.IsEqual(assignmenti.result, assignmentj.operand2))
                {
                    return true;
                }
                return false;
            }
            else if(codei is ICAssignment && codej is ICWhile)
            {
                ICAssignment assignmenti = (ICAssignment)codei;
                ICWhile whilej = (ICWhile)codej;
                if(ETerminal.IsEqual(assignmenti.result, whilej.condition))
                {
                    return true;
                }
                foreach(var entry in whilej.conditionCodes.GetCodes())
                {
                    if (IsDependent(assignmenti, entry))
                    {
                        return true;
                    }
                }
                foreach(var entry in whilej.codes.GetCodes())
                {
                    if(IsDependent(assignmenti, entry))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if(codei is ICWhile && codej is ICAssignment)
            {
                ICWhile whilei = (ICWhile)codei;
                ICAssignment assignmentj = (ICAssignment)codej;
                if (ETerminal.IsEqual(assignmentj.result, whilei.condition))
                {
                    return true;
                }
                foreach (var entry in whilei.conditionCodes.GetCodes())
                {
                    if (IsDependent(entry, assignmentj))
                    {
                        return true;
                    }
                }
                foreach (var entry in whilei.codes.GetCodes())
                {
                    if (IsDependent(entry, assignmentj))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (codei is ICWhile && codej is ICWhile)
            {
                ICWhile whilei = (ICWhile)codei, whilej = (ICWhile)codej;
                List<IntermediateCode>
                    codesi = new List<IntermediateCode>(),
                    codesj = new List<IntermediateCode>();
                codesi.AddRange(whilei.conditionCodes.GetCodes());
                codesi.AddRange(whilei.codes.GetCodes());
                codesj.AddRange(whilej.conditionCodes.GetCodes());
                codesj.AddRange(whilej.codes.GetCodes());
                foreach (var entryi in codesi)
                {
                    foreach(var entryj in codesj)
                    {
                        if(IsDependent(entryi, entryj))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (codei is ICAssignment && codej is ICIfElse)
            {
                ICAssignment assignmenti = (ICAssignment)codei;
                ICIfElse ifelsej = (ICIfElse)codej;
                if (ETerminal.IsEqual(assignmenti.result, ifelsej.condition))
                {
                    return true;
                }
                List<IntermediateCode> codesj = new List<IntermediateCode>();
                codesj.AddRange(ifelsej.conditionCodes.GetCodes());
                codesj.AddRange(ifelsej.codesIf.GetCodes());
                codesj.AddRange(ifelsej.codesElse.GetCodes());
                foreach (var entry in codesj)
                {
                    if (IsDependent(assignmenti, entry))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if(codei is ICIfElse && codej is ICAssignment)
            {
                ICIfElse ifelsei = (ICIfElse)codei;
                ICAssignment assignmentj = (ICAssignment)codej;
                if (ETerminal.IsEqual(assignmentj.result, ifelsei.condition))
                {
                    return true;
                }
                List<IntermediateCode> codesi = new List<IntermediateCode>();
                codesi.AddRange(ifelsei.conditionCodes.GetCodes());
                codesi.AddRange(ifelsei.codesIf.GetCodes());
                codesi.AddRange(ifelsei.codesElse.GetCodes());
                foreach (var entry in codesi)
                {
                    if (IsDependent(entry, assignmentj))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if(codei is ICWhile && codej is ICIfElse)
            {
                ICWhile whilei = (ICWhile)codei;
                ICIfElse ifelsej = (ICIfElse)codej;
                List<IntermediateCode>
                    codesi = new List<IntermediateCode>(),
                    codesj = new List<IntermediateCode>();
                codesi.AddRange(whilei.conditionCodes.GetCodes());
                codesi.AddRange(whilei.codes.GetCodes());
                codesj.AddRange(ifelsej.conditionCodes.GetCodes());
                codesj.AddRange(ifelsej.codesIf.GetCodes());
                codesj.AddRange(ifelsej.codesElse.GetCodes());
                foreach (var entryi in codesi)
                {
                    foreach (var entryj in codesj)
                    {
                        if (IsDependent(entryi, entryj))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (codei is ICIfElse && codej is ICWhile)
            {
                ICIfElse ifelsei = (ICIfElse)codei;
                ICWhile whilej = (ICWhile)codej;
                List<IntermediateCode>
                    codesi = new List<IntermediateCode>(),
                    codesj = new List<IntermediateCode>();
                codesi.AddRange(ifelsei.conditionCodes.GetCodes());
                codesi.AddRange(ifelsei.codesIf.GetCodes());
                codesi.AddRange(ifelsei.codesElse.GetCodes());
                codesj.AddRange(whilej.conditionCodes.GetCodes());
                codesj.AddRange(whilej.codes.GetCodes());
                foreach (var entryi in codesi)
                {
                    foreach (var entryj in codesj)
                    {
                        if (IsDependent(entryi, entryj))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                ICIfElse ifelsei = (ICIfElse)codei;
                ICIfElse ifelsej = (ICIfElse)codej;
                List<IntermediateCode>
                    codesi = new List<IntermediateCode>(),
                    codesj = new List<IntermediateCode>();
                codesi.AddRange(ifelsei.conditionCodes.GetCodes());
                codesi.AddRange(ifelsei.codesIf.GetCodes());
                codesi.AddRange(ifelsei.codesElse.GetCodes());
                codesj.AddRange(ifelsej.conditionCodes.GetCodes());
                codesj.AddRange(ifelsej.codesIf.GetCodes());
                codesj.AddRange(ifelsej.codesElse.GetCodes());
                foreach (var entryi in codesi)
                {
                    foreach (var entryj in codesj)
                    {
                        if (IsDependent(entryi, entryj))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        private void BuildDependencyGraph(List<IntermediateCode> icList, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                if(icList[i] is ICWhile)
                {
                    var temp = ((ICWhile)icList[i]).codes.GetCodes();
                    BuildDependencyGraph(temp, temp.Count);
                }
                for (int j = i + 1; j < count; ++j)
                {
                    if (IsDependent(icList[i], icList[j]))
                    {
                        AddDependency(icList[i], icList[i], DependencyType.Any);
                    }                   
                }
            }
        }

        public override Program Optimize(Program program)
        {
            BuildDependencyGraph(program.icList.GetCodes(), program.icList.Count - 1);
            for (int i = 0; i < program.icList.Count - 1; ++i)
            {
                AddDependency(program.icList[i], program.icList[program.icList.Count - 1], DependencyType.Any);
            }
            //program.initStatements.Add(program.statementsList[0]);
            //for (int i = 1; i < program.statementsList.Count(); ++i)
            //{
            //    if (program.IsIndependent(program.statementsList[i]))
            //    {
            //        program.initStatements.Add(program.statementsList[i]);
            //    }
            //}
            return program;
        }
    }
}
