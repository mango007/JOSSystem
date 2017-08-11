//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using JOSPrototype.Components;
//namespace JOSPrototype.Runtime.Encryption
//{
//    class CodeEncryption
//    {
//        Program toSec;
//        Random rand;

//        EVariable getRandVar()
//        {
//            int nVars = toSec.vTable.Count;
//            EVariable newVar = new EVariable();
//            KeyValuePair<string, object> var;
//            do {
//                int rEle = rand.Next() % nVars;
//                var arr = toSec.vTable.ToArray();
//                var = arr[rEle];
//            } while (((string)(var.Key)).Contains("$"));
//            newVar.name = var.Key;
//            Console.Write(var.Key + " ");
//            return newVar;
//        }

//        Expression secureExpression(Expression expr)
//        {
//            if (expr is EUnaryOperation || expr is EBinaryOperation)
//            {

//                //OperationType currOp = expr is EBinaryOperation ? ((EBinaryOperation)expr).Operation : ((EUnaryOperation)expr).Operation;
//                //OperationType[] addMulsUn = new OperationType[] {  }; //use at least two commands
//                //OperationType[] andOrUn = new OperationType[] {}; // OperationType.NOT, OperationType.NOT  //use at least two commands
//                OperationType[] addMulsBin = new OperationType[] { OperationType.Addition, OperationType.Multiplication, OperationType.Substraction };//  OperationType.Addition, OperationType.Multiplication,OperationType.Substraction 
//                OperationType[] andOrBin = new OperationType[] { OperationType.OR, OperationType.AND };
//                OperationType[] comp = new OperationType[] {  OperationType.EqualZero, OperationType.LessZero };

//                ////Create a random operation
//                //Expression randVar1 = getRandVar();
//                //OperationType dummyType = OperationType.None;
//                ////if (andOrUn.Contains(currOp) || andOrBin.Contains(currOp))
//                ////    dummyType = andOrUn[rand.Next() % (andOrUn.Count() - 1)];                
//                ////else
//                ////{
//                ////    //if (addMulsUn.Contains(currOp) || addMulsBin.Contains(currOp))
//                ////      //  dummyType = addMulsUn[rand.Next() % (addMulsBin.Count() - 1)];
//                ////}

//                //Expression randOp1 = null;
//                //if (dummyType != OperationType.None)
//                //    randOp1 = new EUnaryOperation(randVar1, dummyType); //could also use sine

//                //Expression randVar2 = getRandVar();                
//                //dummyType = OperationType.None;
//                //if (addMulsUn.Contains(currOp) ||  addMulsBin.Contains(currOp))                    
//                //    dummyType = addMulsBin[rand.Next() % (addMulsBin.Count()-1)];
//                //else
//                //{
//                //    if (andOrBin.Contains(currOp))
//                //        dummyType = andOrBin[rand.Next() % (andOrBin.Count()-1)];                    
//                //}
//                //Expression randOp2 = null;
//                //if (dummyType != OperationType.None)
//                //{
//                //    //randOp2 = new EBinaryOperation(randVar1, randVar2, dummyType);
//                //    randOp2 = new EBinaryOperation (((EBinaryOperation)expr).Operand1, ((EBinaryOperation)expr).Operand2, OperationType.Addition);
//                //}
//                //int choice = rand.Next() % 2;
//                //if (randOp1 == null && randOp2 == null)
//                //    return expr;                
//                //Expression dummyOp = choice > 0 || randOp2 == null ? randOp1 : randOp2;
//                //Expression dummyOp = randOp1;

//                Expression dummyOp = null;
//                Expression newExpr = null;
//                OperationType dummyType = OperationType.None;
//                if ((expr is EBinaryOperation))
//                {

//                    EBinaryOperation currop = (EBinaryOperation)expr;                    
//                    if (addMulsBin.Contains(currop.Operation))
//                        dummyType = addMulsBin[rand.Next() % (addMulsBin.Count() - 1)];
//                    if (andOrBin.Contains(currop.Operation))
//                        dummyType = andOrBin[rand.Next() % (andOrBin.Count() - 1)];
//                    if (comp.Contains(currop.Operation))
//                        dummyType = comp[rand.Next() % (comp.Count() - 1)];
//                    if (dummyType == OperationType.None)
//                        return expr;
//                    dummyOp = new EBinaryOperation(currop.Operand1, currop.Operand2, dummyType);
//                    newExpr = new EBinaryOperation(secureExpression(currop.Operand1), secureExpression(currop.Operand2), currop.Operation);
//                }

//                if (expr is EUnaryOperation)
//                {
//                    EUnaryOperation currop = (EUnaryOperation)expr;
//                    if (comp.Contains(currop.Operation))
//                        dummyType = comp[rand.Next() % (comp.Count() - 1)];
//                    if (dummyType == OperationType.None)
//                        return expr;
//                    dummyOp = new EUnaryOperation(currop.Operand, dummyType);
//                    newExpr = new EUnaryOperation(secureExpression(currop.Operand), currop.Operation);
//                }

//                    //Add random operation to program (dummyOp): expr*1 + 0*dummyOp
//                    //Console.Write(" s" + dummyType.ToString());
//               int order = rand.Next() % 2;                
//                Expression mul1 = new EBinaryOperation(new ENumericLiteral(1, 0), newExpr, OperationType.Multiplication);
//                Expression mul2 = new EBinaryOperation(new ENumericLiteral(0, 0), dummyOp, OperationType.Multiplication);                
//                if (order ==1 )                
//                    return new EBinaryOperation(mul1, mul2, OperationType.Addition);                                    
//                else                
//                    return new EBinaryOperation(mul2, mul1, OperationType.Addition);                                    
                
//            }
//            // Do not change ELiteral, Eterminal, Evariable
//            return expr;
//        }

//        Statement secureStatement(Statement st)
//        {
//            if (st is SSequence)
//            {
//                List<Statement> secStats = new List<Statement>();
//                foreach (Statement iterst in ((SSequence)st).GetStatementsList())
//                    secStats.Add(secureStatement(iterst));
//                return new SSequence(secStats.ToArray());
//            }

//            if (st is SAssignment)
//            {
//                Expression secExpr = secureExpression(((SAssignment)st).value);
//                return new SAssignment(((SAssignment)st).result, secExpr);
//            }

//            if (st is SIfElse)
//            {
//                Expression secExpr = secureExpression(((SIfElse)st).condition);
//                Statement secStat1 = secureStatement(((SIfElse)st).statIf);
//                Statement secStat2 = secureStatement(((SIfElse)st).statElse);
//                return new SIfElse(secExpr,secStat1,secStat2);
//            }

//            if (st is SReturn)
//            {
//                List<Expression> secExpr = new List<Expression>();
//                foreach (Expression expr in ((SReturn)st).exps)
//                    secExpr.Add(secureExpression(expr));
//                return new SReturn(secExpr.ToArray());
//            }
//            return null;            
//        }

//        public Program encrypt(Program p)
//        {
//            toSec = p;
//            rand = new Random();
//            Program encP = new Program();            
            
//            foreach (Statement st in p.GetStatementsList())
//            {
//                encP.AddStatement(secureStatement(st));               
//            }
//            return encP;
//        }
//    }
//}
