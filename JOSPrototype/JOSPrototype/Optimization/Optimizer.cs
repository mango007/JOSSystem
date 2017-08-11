using JOSPrototype.Components;

namespace JOSPrototype.Optimization
{
    abstract class Optimizer
    {
        public abstract Program Optimize(Program program);
    }
}
