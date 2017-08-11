using JOSPrototype.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Frontend
{
    class Parser
    {
        public Parser(string code)
        {
            this.code = code;
        }
        internal Program GetProgram()
        {
            tokens = Tokenizer.Tokenize(code);
            NextToken();
            program.AddStatement(Statement());
            return program;
        }

        private Program program = new Program();
        private Dictionary<string, EVariable> vars = new Dictionary<string, EVariable>();
        private string code;
        private List<Token> tokens;

        int pos = 0;
        Token currToken;
        void Error(string s)
        {
            Console.WriteLine(s);
            Console.ReadKey();
        }
        void NextToken()
        {
            currToken = tokens[pos++];
            //if(pos % 10 == 0)                {                    Console.Write(".");                }
        }
        bool Accept(Symbol s)
        {
            if (currToken.sym == s)
            {
                NextToken();
                return true;
            }
            return false;
        }
        bool Expect(Symbol s)
        {
            if (Accept(s))
                return true;
            Error("expect: unexpected symbol:" + s);
            return false;
        }
        void AddDOMForLiteral(ENumericLiteral num)
        {
            if(Accept(Symbol.S_LParen))
            {
                Expect(Symbol.S_None);
                num.needEnc = false;
                Expect(Symbol.S_RParen);
            }
        }
        bool IsNumeric(out ENumericLiteral num)
        {
            switch (currToken.sym)
            {
                case Symbol.S_True:
                    num = new ENumericLiteral(1, 0);
                    NextToken();
                    AddDOMForLiteral(num);
                    return true;
                case Symbol.S_False:
                    num = new ENumericLiteral(0, 0);
                    NextToken();
                    AddDOMForLiteral(num);
                    return true;
                case Symbol.S_Num:
                    BigInteger resultInt;                   
                    if (BigInteger.TryParse(currToken.sequence, out resultInt))
                    {
                        num = new ENumericLiteral(resultInt, 0);
                        NextToken();
                        AddDOMForLiteral(num);
                        return true;
                    }
                    else
                    {
                        double resultDouble;
                        double.TryParse(currToken.sequence, out resultDouble);
                        num = new ENumericLiteral((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        NextToken();
                        AddDOMForLiteral(num);
                        return true;
                    }
                default:
                    num = null;
                    return false;
            }                  
        }
        Expression NumericFactor()
        {
            ENumericLiteral num;
            // next symbol is a variable
            if (vars.ContainsKey(currToken.sequence))
            {
                var temp = currToken.sequence;
                NextToken();
                return vars[temp];
            }
            // next symbol is a number
            else if (IsNumeric(out num))
            {
                return num;
            }
            else if (Accept(Symbol.S_LParen))
            {
                var temp = NumericExpression();
                Expect(Symbol.S_RParen);
                return temp;
            }
            else if (Accept(Symbol.S_Sin))
            {
                Expect(Symbol.S_LParen);
                var temp = NumericExpression();
                Expect(Symbol.S_RParen);
                return new EUnaryOperation(temp, OperationType.Sin);
            }
            else {
                Error("factor: syntax error");
                return null;
            }
        }
        Expression NumericTerm()
        {
            Expression term = NumericFactor();
            while (currToken.sym == Symbol.S_Times || currToken.sym == Symbol.S_Divide || currToken.sym == Symbol.S_BAND)
            {
                if(currToken.sym == Symbol.S_Times)
                {
                    NextToken();
                    term = new EBinaryOperation(term, NumericFactor(), OperationType.Multiplication);
                }
                else if(currToken.sym == Symbol.S_Divide)
                {
                    NextToken();
                    term = new EBinaryOperation(term, new EUnaryOperation(NumericFactor(), OperationType.Inverse), OperationType.Multiplication);
                }
                else
                {
                    NextToken();
                    term = new EBinaryOperation(term, NumericFactor(), OperationType.AND);
                }
            }
            return term;
        }
        Expression NumericExpression()
        {
            Expression exp = new ENumericLiteral(0, 0);
            ((ENumericLiteral)exp).needEnc = false;
            if (currToken.sym == Symbol.S_Plus)
            {
                NextToken();
            }
            else if(currToken.sym == Symbol.S_Minus)
            {
                NextToken();
                ENumericLiteral temp;
                if(IsNumeric(out temp))
                {
                    Numeric val = temp.GetValue();
                    exp = new ENumericLiteral(new Numeric(0, val.GetScaleBits()) - val);
                }
                else
                {
                    exp = new EBinaryOperation(exp, NumericTerm(), OperationType.Substraction);
                }
                  
            }
            else
            {
                exp = NumericTerm();
            }
            while (currToken.sym == Symbol.S_Plus || currToken.sym == Symbol.S_Minus || currToken.sym == Symbol.S_BXOR)
            {
                if (currToken.sym == Symbol.S_Plus)
                {
                    NextToken();
                    exp = new EBinaryOperation(exp, NumericTerm(), OperationType.Addition);
                }
                else if(currToken.sym == Symbol.S_Minus)
                {
                    NextToken();
                    exp = new EBinaryOperation(exp, NumericTerm(), OperationType.Substraction);
                }
                else
                {
                    NextToken();
                    exp = new EBinaryOperation(exp, NumericTerm(), OperationType.XOR);
                }
            }
            return exp;
        }
        Expression BooleanFactor()
        {
            if (Accept(Symbol.S_True))
            {
                var num = new ENumericLiteral(1, 0);
                AddDOMForLiteral(num);
                return num;
            }
            else if (Accept(Symbol.S_False))
            {
                var num = new ENumericLiteral(0, 0);
                AddDOMForLiteral(num);
                return num;
            }
            else if (Accept(Symbol.S_LParen))
            {
                var exp = BooleanExpression();
                Expect(Symbol.S_RParen);
                return exp;
            }
            //else if(Accept("!"))
            //{
            //    var exp = BooleanExpression();
            //}
            else
            {
                var operand1 = NumericExpression();
                Expression result = null;
                if (Accept(Symbol.S_Equal))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EBinaryOperation(operand1, operand2, OperationType.Substraction), OperationType.EqualZero);
                }
                else if (Accept(Symbol.S_NotEqual))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EUnaryOperation(new EBinaryOperation(operand1, operand2, OperationType.Substraction), OperationType.EqualZero), OperationType.NOT);
                }
                else if (Accept(Symbol.S_EqualLess))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EUnaryOperation(new EBinaryOperation(operand2, operand1, OperationType.Substraction), OperationType.LessZero), OperationType.NOT);
                }
                else if (Accept(Symbol.S_EqualGreater))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EUnaryOperation(new EBinaryOperation(operand1, operand2, OperationType.Substraction), OperationType.LessZero), OperationType.NOT);
                }
                else if (Accept(Symbol.S_Less))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EBinaryOperation(operand1, operand2, OperationType.Substraction), OperationType.LessZero);
                }
                else if (Accept(Symbol.S_Greater))
                {
                    var operand2 = NumericExpression();
                    result = new EUnaryOperation(new EBinaryOperation(operand2, operand1, OperationType.Substraction), OperationType.LessZero);
                }
                else
                {
                    Error("BooleanFactor: expect ==, !=, <=, >=, < or >");
                }
                return result;
            }
        }
        Expression BooleanTerm()
        {
            var result = BooleanFactor();
            while(Accept(Symbol.S_AND))
            {
                result = new EBinaryOperation(result, BooleanFactor(), OperationType.AND);
            }
            return result;
        }
        Expression BooleanExpression()
        {
            var result = BooleanTerm();
            while (Accept(Symbol.S_OR))
            {
                result = new EBinaryOperation(result, BooleanTerm(), OperationType.OR);
            }
            return result;
        }

 
        Statement DeclarationStatement()
        {
            List<Statement> re = new List<Statement>();
            if(vars.ContainsKey(currToken.sequence))
            {
                Error("DeclarationStatement: variable has been declared");
            }
            else
            {
                var currVar = new EVariable(program, currToken.sequence);
                vars.Add(currToken.sequence, currVar);
                NextToken();
                if (Accept(Symbol.S_Assign))
                {
                    var exp = NumericExpression();
                    re.Add(new SAssignment(currVar, exp));
                }
                else
                {
                    re.Add(new SAssignment(currVar, new ENumericLiteral(0, 0)));
                }
                if(Accept(Symbol.S_LBracket))
                {
                    ENumericLiteral num;
                    if(IsNumeric(out num))
                    {
                        currVar.leftBoundary = num.GetValue();
                    }
                    else
                    {
                        Error("DeclarationStatement: left boundary should be a number");
                    }
                    //double resultDouble = 2;
                    //if (!double.TryParse(currToken.sequence, out resultDouble))
                    //{
                    //    Error("DeclarationStatement: left boundary should be a number");
                    //}
                    //currVar.leftBoundary = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                    //NextToken();
                    Expect(Symbol.S_Comma);
                    if (IsNumeric(out num))
                    {
                        currVar.rightBoundary = num.GetValue();
                    }
                    else
                    {
                        Error("DeclarationStatement: right boundary should be a number");
                    }
                    // use STATDOM
                    if (Accept(Symbol.S_Comma))
                    {
                        double resultDouble;
                        if (!double.TryParse(currToken.sequence, out resultDouble))
                        {
                            Error("DeclarationStatement: probability should be a number");
                        }
                        if (resultDouble < 0 || resultDouble > 1)
                        {
                            Error("DeclarationStatement: probability is out of range");
                        }
                        currVar.prob = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                        NextToken();
                    }
                    Expect(Symbol.S_RBracket);
                }
            }
            while(!Accept(Symbol.S_Semicolon))
            {
                Expect(Symbol.S_Comma);
                if (vars.ContainsKey(currToken.sequence))
                {
                    Error("SDeclaration: variable has been declared");
                }
                else
                {
                    var currVar = new EVariable(program, currToken.sequence);
                    vars.Add(currToken.sequence, currVar);
                    NextToken();
                    if (Accept(Symbol.S_Assign))
                    {
                        var exp = NumericExpression();
                        re.Add(new SAssignment(currVar, exp));
                    }
                    else
                    {
                        re.Add(new SAssignment(currVar, new ENumericLiteral(0, 0)));
                    }
                    if (Accept(Symbol.S_LBracket))
                    {
                        ENumericLiteral num;
                        if (IsNumeric(out num))
                        {
                            currVar.leftBoundary = num.GetValue();
                        }
                        else
                        {
                            Error("DeclarationStatement: left boundary should be a number");
                        }
                        Expect(Symbol.S_Comma);
                        if (IsNumeric(out num))
                        {
                            currVar.rightBoundary = num.GetValue();
                        }
                        else
                        {
                            Error("DeclarationStatement: right boundary should be a number");
                        }
                        // use STATDOM
                        if (Accept(Symbol.S_Comma))
                        {
                            double resultDouble;
                            if (!double.TryParse(currToken.sequence, out resultDouble))
                            {
                                Error("DeclarationStatement: probability should be a number");
                            }
                            if (resultDouble < 0 || resultDouble > 1)
                            {
                                Error("DeclarationStatement: probability is out of range");
                            }
                            currVar.prob = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                            NextToken();
                        }
                        Expect(Symbol.S_RBracket);
                    }
                }
            }
            if(re.Count == 0)
            {
                Error("DeclarationStatement: need to declare at least one variable");
                return null;
            }
            else if(re.Count == 1)
            {
                return re[0];
            }
            else
            {
                return new SSequence(re.ToArray());
            }
        }
        Statement AssignmentStatement()
        {
            Statement re = null;
            if(vars.ContainsKey(currToken.sequence))
            {
                var result = vars[currToken.sequence];
                NextToken();
                if (Accept(Symbol.S_Assign))
                {
                    var exp = NumericExpression();
                    re = new SAssignment(result, exp);
                }
                Expect(Symbol.S_Semicolon);
            }
            else
            {
                Error("AssignmentStatement: variable is not declared");
            }
            return re;
        }
        Statement IfElseStatement()
        {
            Expect(Symbol.S_LParen);
            var condition = BooleanExpression();
            Expect(Symbol.S_RParen);
            Numeric revealCond = null, prob = null;
            if (Accept(Symbol.S_LBracket))
            {              
                if (Accept(Symbol.S_True))
                {
                    revealCond = new Numeric(1, 0);
                }
                else if (Accept(Symbol.S_False))
                {
                    revealCond = new Numeric(0, 0);
                }
                else
                {
                    Error("IfElseStatement: reveal condition should be true or false");
                }
                
                // use STATDOM
                if (Accept(Symbol.S_Comma))
                {
                    double resultDouble = 2;
                    if (!double.TryParse(currToken.sequence, out resultDouble))
                    {
                        Error("IfElseStatement: probability should be a number");
                    }
                    if (resultDouble < 0 || resultDouble > 1)
                    {
                        Error("IfElseStatement: probability is out of range");
                    }
                    prob = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                    NextToken();
                }                
                //var prob = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.FractionBitLength)), Config.FractionBitLength);
                
                Expect(Symbol.S_RBracket);
            }

            List<Statement> ifState = new List<Statement>();
            ifState.Add(Statement());
            List<Statement> elseState = new List<Statement>();
            if (Accept(Symbol.S_Else))
            {
                elseState.Add(Statement());
            }
            switch(ifState.Count)
            {
                case 0:
                    switch (elseState.Count)
                    {
                        case 0:
                            return new SIfElse(condition, null, null, revealCond, prob);
                        default:
                            return new SIfElse(condition, null, new SSequence(elseState.ToArray()), revealCond, prob);
                    }
                default:
                    switch (elseState.Count)
                    {
                        case 0:
                            return new SIfElse(condition, new SSequence(ifState.ToArray()), null, revealCond, prob);
                        default:
                            return new SIfElse(condition, new SSequence(ifState.ToArray()), new SSequence(elseState.ToArray()), revealCond, prob);
                    }
            }

        }

        Statement CaseDefaultStatement(SSwitch switchStat)
        {
            //var ret =  new SSequence(new SAssignment(vars["min"], new EUnaryOperation(vars["tempCurrentValue"], OperationType.Switch)),
            //    new SAssignment(vars["max"], new EUnaryOperation(vars["tempCurrentValue"], OperationType.Switch)),
            //    new SAssignment(vars["increasing_or_decreasing_factor"], new EUnaryOperation(vars["tempCurrentValue"], OperationType.Switch)));   
            //while(currToken != "}")
            //{
            //    NextToken();
            //}
            //return ret;
            if (Accept(Symbol.S_Default))
            {
                List<Statement> stats = new List<Statement>();
                Expect(Symbol.S_Colon);
                while (!Accept(Symbol.S_Break))
                {
                    stats.Add(Statement());
                }
                Expect(Symbol.S_Semicolon);
                switchStat.defaultStat = new SSequence(stats.ToArray());
                return new SSequence(stats.ToArray());
            }
            else
            {
                Expect(Symbol.S_Case);
                Numeric caseKey = new Numeric(currToken.sequence, 0);
                Expression condition = new EUnaryOperation(new EBinaryOperation(switchStat.dice, new ENumericLiteral(currToken.sequence, 0), OperationType.Substraction), OperationType.EqualZero);
                NextToken();
                Expect(Symbol.S_Colon);
                List<Statement> ifStats = new List<Statement>();
                while (!Accept(Symbol.S_Break))
                {
                    ifStats.Add(Statement());
                }
                Expect(Symbol.S_Semicolon);
                switchStat.caseStat.Add(caseKey, new SSequence(ifStats.ToArray()));
                var elseStats = CaseDefaultStatement(switchStat);
                return new SIfElse(condition, new SSequence(ifStats.ToArray()), elseStats, null, null);
            }
        }
        Statement SwitchStatement()
        {
            Expect(Symbol.S_LParen);
            Expression dice = NumericExpression();
            Expect(Symbol.S_RParen);

            Numeric revealCond = null, prob = null;
            if (Accept(Symbol.S_LBracket))
            {
                
                if (Accept(Symbol.S_True))
                {
                    revealCond = new Numeric(1, 0);
                }
                else if (Accept(Symbol.S_False))
                {
                    revealCond = new Numeric(0, 0);
                }
                else
                {
                    Error("IfElseStatement: reveal condition should be true or false");
                }
                Expect(Symbol.S_Comma);

                double resultDouble;
                double.TryParse(currToken.sequence, out resultDouble);
                prob = new Numeric((BigInteger)(resultDouble * Math.Pow(2, Config.ScaleBits)), Config.ScaleBits);
                NextToken();

                Expect(Symbol.S_RBracket);
            }


            Expect(Symbol.S_LBrace);
            SSwitch switchStat = new SSwitch(dice, revealCond, prob);
            var ifelseStat = CaseDefaultStatement(switchStat);
            Expect(Symbol.S_RBrace);
            switchStat.ifelseStat = ifelseStat;
            return switchStat;
        }
        Statement WhileStatement()
        {
            Expect(Symbol.S_LParen);
            var condition = BooleanExpression();
            Expect(Symbol.S_RParen);
            var stats = Statement();
            return new SWhile(condition, stats);
        }
        Statement ReturnStatement()
        {
            var exp = NumericExpression();
            Expect(Symbol.S_Semicolon);
            return new SReturn(new Expression[] { exp });           
        }
        Statement Statement()
        {                
            if(Accept(Symbol.S_LBrace))
            {
                List<Statement> stats = new List<Statement>();
                while (!Accept(Symbol.S_RBrace))
                {
                    if (Accept(Symbol.S_Type))
                    {
                        stats.Add(DeclarationStatement());
                    }
                    else if (Accept(Symbol.S_If))
                    {
                        stats.Add(IfElseStatement());
                    }
                    else if (Accept(Symbol.S_Switch))
                    {
                        stats.Add(SwitchStatement());
                    }
                    else if (Accept(Symbol.S_While))
                    {
                        stats.Add(WhileStatement());
                    }
                    else if (Accept(Symbol.S_Return))
                    {
                        stats.Add(ReturnStatement());
                    }
                    else if (Accept(Symbol.S_Semicolon)) { }
                        
                    else
                    {
                        stats.Add(AssignmentStatement());
                    }
                }
                return new SSequence(stats.ToArray());
            }
            else
            {
                Statement stat = null;
                if (Accept(Symbol.S_Type))
                {
                    stat = DeclarationStatement();
                }
                else if (Accept(Symbol.S_If))
                {
                    stat = IfElseStatement();
                }
                else if (Accept(Symbol.S_Switch))
                {
                    stat = SwitchStatement();
                }
                else if (Accept(Symbol.S_While))
                {
                    stat = WhileStatement();
                }
                else if (Accept(Symbol.S_Return))
                {
                    stat =  ReturnStatement();
                }
                else if (Accept(Symbol.S_Semicolon)) { }
                else
                {
                    stat = AssignmentStatement();
                }
                return stat;
            }                
        }
    } 
}
