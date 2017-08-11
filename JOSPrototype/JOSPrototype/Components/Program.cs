using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace JOSPrototype.Components
{
    [Serializable]
    class Program
    {
        [NonSerialized]
        private SSequence stats = new SSequence();
        /// <summary>
        /// translated intermediate code
        /// </summary>
        public ICSequence icList = new ICSequence();
        /// <summary>
        /// store the variables' name and their values
        /// </summary>
        public ConcurrentDictionary<string, Object> vTable = new ConcurrentDictionary<string, Object>();
        /// <summary>
        /// names of return variables
        /// </summary>
        public List<string> vReturn = new List<string>();
        /// <summary>
        /// a collection store the component which has been evaluated
        /// </summary>
        public ConcurrentDictionary<IntermediateCode, object> evaluatedIC = new ConcurrentDictionary<IntermediateCode, object>();

        public void AddStatement(Statement stat)
        {
            stats.AddStatement(stat);
        }
        public List<Statement> GetStatementsList() { return stats.GetStatementsList(); }
        public Numeric GetValue(ETerminal terminal)
        {
            if (terminal is ENumericLiteral)
            {
                // literal can be accessed only once, no need to copy
                return ((ENumericLiteral)terminal).GetValue();
            }
            else
            {
                // for variable, make a copy of the numeric, such that modification of the numeric in operations does not affect the original date
                return new Numeric((Numeric)vTable[((EVariable)terminal).name]);
            }
        }

        public void SetValue(EVariable variable, object value)
        {
            vTable.AddOrUpdate(variable.name, value, (k, v) => value);
        }

        /// <summary>
        /// for Output, Anti, Flow dependency, only need to check if the dependent statement is evaluated
        /// for Loop dependency, need further thinking
        /// </summary>
        /// <returns></returns>
        public bool IsIndependent(IntermediateCode ic)
        {
            foreach (var entry in ic.inEdge)
            {
                foreach (var type in entry.Value)
                {
                    switch (type)
                    {
                        case DependencyType.Output:
                        case DependencyType.Anti:
                        case DependencyType.Flow:
                        case DependencyType.Any:
                            if (!evaluatedIC.ContainsKey(entry.Key))
                            {
                                return false;
                            }
                            break;
                        default:
                            break;
                    }
                }

            }
            return true;
        }

        public void Translate()
        {
            IntermediateCode.ResetCount();
            EVariable.ResetCount();
            foreach (var stat in stats.GetStatementsList())
            {
                icList.AddRange(TranslateStatement(stat, this));
            }
            icList.Add(new ICAssignment(OperationType.Return, null, null, null));
        }

        public Program[] EncProgram()
        {
            Program toEVH = new Program(), toKH = new Program();
            foreach(var name in vTable.Keys)
            {
                toEVH.vTable.TryAdd(name, null);
                toKH.vTable.TryAdd(name, null); 
            }

            foreach(var entry in vReturn)
            {
                toEVH.vReturn.Add(entry);
                toKH.vReturn.Add(entry);
            }
            var encCode = EncBlock(icList);
            toEVH.icList = encCode[0];
            toKH.icList = encCode[1];
            return new Program[] { toEVH, toKH };
        } 

        private ICSequence[] EncBlock(ICSequence codes)
        {
            var toKH = new ICSequence();
            var toEVH = new ICSequence();
            foreach (var ic in codes.GetCodes())
            {
                if (ic is ICAssignment)
                {
                    ICAssignment ica = (ICAssignment)ic;
                    Numeric key1 = null, key2 = null;
                    toKH.Add(new ICAssignment(ica, ref key1, ref key2, PartyType.KH));
                    toEVH.Add(new ICAssignment(ica, ref key1, ref key2, PartyType.EVH));
                }
                else if(ic is ICWhile)
                {
                    ICWhile icw = (ICWhile)ic, icwEVH = new ICWhile(), icwKH = new ICWhile();
                    icwEVH.index = icw.index;
                    icwKH.index = icw.index;
                    if (icw.condition is ENumericLiteral)
                    {
                        var cond = (ENumericLiteral)icw.condition;
                        if(!cond.needEnc)
                        {
                            var val = new Numeric(cond.GetValue());
                            val.SetEncType(EncryptionType.None);
                            icwEVH.condition = new ENumericLiteral(val);
                            ((ENumericLiteral)icwEVH.condition).needEnc = false;
                            icwKH.condition = new ENumericLiteral(val);
                            ((ENumericLiteral)icwKH.condition).needEnc = false;
                        }
                        else
                        {
                            var keyTemp = Utility.NextUnsignedNumeric(0, 1);
                            icwEVH.condition = new ENumericLiteral(GetValue(icw.condition) ^ keyTemp);
                            icwKH.condition = new ENumericLiteral(keyTemp);
                        }                       
                    }
                    else
                    {
                        icwEVH.condition = icw.condition;
                        icwKH.condition = icw.condition;
                    }
                    var encCondCodes = EncBlock(icw.conditionCodes);
                    icwEVH.conditionCodes.AddRange(encCondCodes[0]);
                    icwKH.conditionCodes.AddRange(encCondCodes[1]);
                    var encCodes = EncBlock(icw.codes);
                    icwEVH.codes.AddRange(encCodes[0]);
                    icwKH.codes.AddRange(encCodes[1]);
                    toEVH.Add(icwEVH);
                    toKH.Add(icwKH);
                }
                else
                {
                    ICIfElse icie = (ICIfElse)ic, icieEVH = new ICIfElse(), icieKH = new ICIfElse();
                    icieEVH.revealCond = icie.revealCond;
                    icieKH.revealCond = icie.revealCond;
                    icieEVH.prob = icie.prob;
                    icieKH.prob = icie.prob;
                    icieEVH.index = icie.index;
                    icieKH.index = icie.index;
                    if (icie.condition is ENumericLiteral)
                    {
                        var cond = (ENumericLiteral)icie.condition;
                        if (!cond.needEnc)
                        {
                            var val = new Numeric(cond.GetValue());
                            val.SetEncType(EncryptionType.None);
                            icieEVH.condition = new ENumericLiteral(val);
                            ((ENumericLiteral)icieEVH.condition).needEnc = false;
                            icieKH.condition = new ENumericLiteral(val);
                            ((ENumericLiteral)icieKH.condition).needEnc = false;
                        }
                        else
                        {
                            var keyTemp = Utility.NextUnsignedNumeric(0, 1);
                            icieEVH.condition = new ENumericLiteral(GetValue(icie.condition) ^ keyTemp);
                            icieKH.condition = new ENumericLiteral(keyTemp);
                        }                        
                    }
                    else
                    {
                        icieEVH.condition = icie.condition;
                        icieKH.condition = icie.condition;
                    }
                    var encCondCodes = EncBlock(icie.conditionCodes);
                    icieEVH.conditionCodes.AddRange(encCondCodes[0]);
                    icieKH.conditionCodes.AddRange(encCondCodes[1]);
                    var encIfCodes = EncBlock(icie.codesIf);
                    icieEVH.codesIf = encIfCodes[0];
                    icieKH.codesIf = encIfCodes[1];
                    var encElseCodes = EncBlock(icie.codesElse);
                    icieEVH.codesElse = encElseCodes[0];
                    icieKH.codesElse = encElseCodes[1];
                    toEVH.Add(icieEVH);
                    toKH.Add(icieKH);
                }
            }
            return new ICSequence[]{ toEVH, toKH };
        }

        public static ICSequence TranslateExpression(Expression exp, EVariable place, Program program)
        {
            System.Diagnostics.Debug.Assert(!(exp is ETerminal));
            ICSequence re = new ICSequence();
            if (exp is EBinaryOperation)
            {
                EBinaryOperation ebo = (EBinaryOperation)exp;
                ETerminal place1, place2;
                ICSequence code1 = null, code2 = null;
                if (!(ebo.Operand1 is ETerminal))
                {
                    place1 = new EVariable(program);
                    code1 = TranslateExpression(ebo.Operand1, (EVariable)place1, program);
                }
                else
                {
                    place1 = (ETerminal)ebo.Operand1;
                }
                if (!(ebo.Operand2 is ETerminal))
                {
                    place2 = new EVariable(program);
                    code2 = TranslateExpression(ebo.Operand2, (EVariable)place2, program);
                }
                else
                {
                    place2 = (ETerminal)ebo.Operand2;
                }
                IntermediateCode lastIC = new ICAssignment(ebo.Operation, place, place1, place2);
                if (!ReferenceEquals(code1, null))
                {
                    re.AddRange(code1);
                    //addDependency(code1[code1.Count - 1], lastIC, DependencyType.Flow);
                }
                if (!ReferenceEquals(code2, null))
                {
                    re.AddRange(code2);
                    //addDependency(code2[code2.Count - 1], lastIC, DependencyType.Flow);
                }
                re.Add(lastIC);
            }
            else if (exp is EUnaryOperation)
            {
                EUnaryOperation euo = (EUnaryOperation)exp;
                ETerminal place1;
                ICSequence code1 = null;
                if (!(euo.Operand is ETerminal))
                {
                    place1 = new EVariable(program);
                    code1 = TranslateExpression(euo.Operand, (EVariable)place1, program);
                }
                else
                {
                    place1 = (ETerminal)euo.Operand;
                }
                IntermediateCode lastIC = new ICAssignment(euo.Operation, place, place1, null);
                if (!ReferenceEquals(code1, null))
                {
                    re.AddRange(code1);
                    //addDependency(code1[code1.Count - 1], lastIC, DependencyType.Flow);
                }
                re.Add(lastIC);
            }
            return re;
        }

        public static ICSequence TranslateStatement(Statement stat, Program program)
        {
            ICSequence re = new ICSequence();
            if (stat is SSequence)
            {
                SSequence ss = (SSequence)stat;
                foreach(var entry in ss.GetStatementsList())
                {
                    re.AddRange(TranslateStatement(/*condition, */entry, program));
                }
            }
            else if(stat is SAssignment)
            {
                SAssignment sa = (SAssignment)stat;
                System.Diagnostics.Debug.Assert(sa.result is EVariable);
                EVariable result = (EVariable)sa.result;
                //if(condition != null)
                //{
                //    Expression
                //        notExp = new EBinaryOperation(new ENumericLiteral("1", 0), condition, OperationType.Substraction),
                //        term1Exp = new EBinaryOperation(condition, sa.value, OperationType.Multiplication),
                //        term2Exp = new EBinaryOperation(notExp, result, OperationType.Multiplication),
                //        newExp = new EBinaryOperation(term1Exp, term2Exp, OperationType.Addition);
                //    re.AddRange(TranslateExpression(newExp, result));
                //}
                //else
                //{
                    if(sa.value is ETerminal)
                    {
                        re.Add(new ICAssignment(OperationType.None, result, (ETerminal)sa.value, null));
                    }
                    else
                    {
                        re.AddRange(TranslateExpression(sa.value, result, program));
                    }
                //}
            }
            else if(stat is SIfElse)
            {
                SIfElse sie = (SIfElse)stat;
                ETerminal cond = null;
                ICIfElse icie = new ICIfElse();
                icie.prob = sie.prob;
                icie.revealCond = sie.revealCond;
                if(sie.condition is ETerminal)
                {
                    cond = (ETerminal)sie.condition;
                }
                else
                {
                    cond = new EVariable(program);
                    icie.conditionCodes.AddRange(TranslateExpression(sie.condition, (EVariable)cond, program));
                }
                icie.condition = cond;               
                if (!ReferenceEquals(sie.statIf, null))
                {
                    icie.codesIf.AddRange(TranslateStatement(sie.statIf, program));
                }
                if(!ReferenceEquals(sie.statElse, null))
                {
                    icie.codesElse.AddRange(TranslateStatement(sie.statElse, program));
                }
                re.Add(icie);
            }
            else if(stat is SReturn)
            {
                SReturn sr = (SReturn)stat;
                foreach(var exp in sr.exps)
                {
                    if(exp is EVariable)
                    {
                        program.vReturn.Add(((EVariable)exp).name);               
                    }
                    else
                    {
                        var place = new EVariable(program);
                        re.AddRange(TranslateExpression(exp, place, program));
                        program.vReturn.Add(place.name);
                    }
                }            
            }
            else if(stat is SWhile)
            {
                SWhile sw = (SWhile)stat;
                ICWhile icw = new ICWhile();
                ETerminal cond = null;
                if(sw.condition is ETerminal)
                {
                    cond = (ETerminal)sw.condition;
                }
                else
                {
                    cond = new EVariable(program);
                    icw.conditionCodes.AddRange(TranslateExpression(sw.condition, (EVariable)cond, program));
                }
                icw.condition = cond;
                icw.codes.AddRange(TranslateStatement(/*cond, */sw.stat, program));
                re.Add(icw);
            }
            return re;
        }
        // tranform intermediate code at runtime
        public static ICSequence TransformIntermediateCode(ETerminal condition, IntermediateCode code, Program program)
        {
            ICSequence re = new ICSequence();
            if (code is ICAssignment)
            {
                ICAssignment ica = (ICAssignment)code;
                if(ReferenceEquals(condition, null))
                {
                    re.Add(code);
                }
                else
                {
                    EVariable
                        result = ica.result,
                        temp1 = new EVariable(program, "$ICA_1_" + code.index),
                        temp2 = new EVariable(program, "$ICA_2_" + code.index),
                        temp3 = new EVariable(program, "$ICA_3_" + code.index),
                        temp4 = new EVariable(program, "$ICA_4_" + code.index);
                    ICAssignment
                        // condition * newResult + !condition * oldResult
                        // Command index is the same as the old command 
                        // such that EVH and KH can receive the correct message.
                        // Commands generated at runtime cannot be run in parallel.
                        newCode = new ICAssignment(ica.op, temp1, ica.operand1, ica.operand2, code.index),
                        notCondition = new ICAssignment(OperationType.NOT, temp2, condition, null, code.index),
                        term1Exp = new ICAssignment(OperationType.Multiplication, temp3, condition, temp1, code.index),
                        term2Exp = new ICAssignment(OperationType.Multiplication, temp4, temp2, result, code.index),
                        term3Exp = new ICAssignment(OperationType.Addition, result, temp3, temp4, code.index);

                    re.AddRange(new ICAssignment[] { newCode, notCondition, term1Exp, term2Exp, term3Exp });
                }
            }
            else if (code is ICIfElse)
            {
                // for if-else statement, simply set the outerCondition
                ICIfElse icie = (ICIfElse)code;
                icie.outerCondition = condition;
                re.Add(icie);
            }
            else if (code is ICWhile)
            {
                ICWhile icw = (ICWhile)code;
                // outerCondition && while condition. This command will be executed very time condition codes are evaluated
                if(!ReferenceEquals(condition, null))
                {
                    EVariable temp1 = new EVariable(program, "&ICW_1_" + code.index);           
                    icw.conditionCodes.Add(new ICAssignment(OperationType.AND, temp1, icw.condition, condition, icw.conditionCodes[icw.conditionCodes.Count - 1].index));
                    icw.condition = temp1;
                }
                ICSequence icwCodes = new ICSequence();
                foreach(var entry in icw.codes.GetCodes())
                {
                    // if statement is Assignment and result is a temporarily variable, no need to transform
                    if (entry is ICAssignment && ((ICAssignment)entry).result.isTemporary)
                    {
                        icwCodes.Add(entry);
                        continue;
                    }
                    icwCodes.AddRange(TransformIntermediateCode(icw.condition, entry, program));
                }
                icw.codes = icwCodes;
                re.Add(icw);
            }
            return re;
        }

        public static ICSequence TransformICIfElse(ICIfElse icie, Program program)
        {
            ICSequence codesToExe = new ICSequence();
            if (icie.codesIf.Count != 0)
            {
                ETerminal
                    temp1 = icie.condition;
                // add (icie.condition & icie.outerCondition) command
                if (!ReferenceEquals(icie.outerCondition, null))
                {
                    temp1 = new EVariable(program, "$ICIE_1_" + icie.index);
                    codesToExe.Add(new ICAssignment(OperationType.AND, (EVariable)temp1, icie.condition, icie.outerCondition, icie.index));
                }
                foreach (var entry in icie.codesIf.GetCodes())
                {
                    // if statement is Assignment and result is a temporarily variable, no need to transform
                    if(entry is ICAssignment && ((ICAssignment)entry).result.isTemporary)
                    {
                        codesToExe.Add(entry);
                        continue;
                    }
                    codesToExe.AddRange(TransformIntermediateCode(temp1, entry, program));
                }
            }
            if (icie.codesElse.Count != 0)
            {
                EVariable temp2 = new EVariable(program, "$ICIE_2_" + icie.index);
                // add (!icie.condition) comamnd
                codesToExe.Add(new ICAssignment(OperationType.NOT, temp2, icie.condition, null, icie.index));
                var temp3 = temp2;
                // add (!icie.condition) && icie.outerCondition
                if (!ReferenceEquals(icie.outerCondition, null))
                {
                    temp3 = new EVariable(program, "$ICIE_3_" + icie.index);
                    codesToExe.Add(new ICAssignment(OperationType.AND, temp3, temp2, icie.outerCondition, icie.index));
                }
                foreach (var entry in icie.codesElse.GetCodes())
                {
                    // if statement is Assignment and result is a temporarily variable, no need to transform
                    if (entry is ICAssignment && ((ICAssignment)entry).result.isTemporary)
                    {
                        codesToExe.Add(entry);
                        continue;
                    }
                    codesToExe.AddRange(TransformIntermediateCode(temp3, entry, program));
                }
            }
            return codesToExe;
        }
    }
}
