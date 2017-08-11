using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOSPrototype.Components
{
    abstract class Statement
    { }

    class SSequence: Statement
    {
        public SSequence(params Statement[] stats)
        {
            this.stats = new List<Statement>(stats);
        }
        public List<Statement> GetStatementsList() { return stats; }
        public void AddStatement(Statement stat) { stats.Add(stat); }
        private List<Statement> stats;
    }

    class SAssignment: Statement
    {
        public SAssignment(Expression result, Expression value)
        {
            this.result = result;
            this.value = value;
        }
        public Expression result;
        public Expression value;
    }

    class SIfElse : Statement
    {
        public SIfElse(Expression condition, Statement statIf, Statement statElse, Numeric revealCond, Numeric prob)
        {
            this.condition = condition;
            this.statIf = statIf;
            this.statElse = statElse;
            this.revealCond = revealCond;
            this.prob = prob;
        }
        public Expression condition;
        public Statement statIf;
        public Statement statElse;
        public Numeric revealCond;
        public Numeric prob;
    }

    class SSwitch: Statement
    {
        public SSwitch(Expression dice, Numeric revealCond, Numeric prob)
        {
            this.dice = dice;
            caseStat = new Dictionary<Numeric, Statement>();
            this.revealCond = revealCond;
            this.prob = prob;
        }
        public Expression dice;
        public Dictionary<Numeric, Statement> caseStat;
        public Statement defaultStat;
        public Numeric revealCond;
        public Numeric prob;
        public Statement ifelseStat;
    }

    class SWhile: Statement
    {
        public SWhile(Expression condition, Statement stat)
        {
            this.condition = condition;
            this.stat = stat;
        }
        public Expression condition;
        public Statement stat;
    }

    class SReturn : Statement
    {
        public SReturn(Expression[] exps)
        {
            this.exps = exps;
        }
        public Expression[] exps;
    }
}
