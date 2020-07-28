using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Models;

namespace Mapster
{
    public class CompileContext
    {
        public HashSet<TypeTuple> Running { get; } = new HashSet<TypeTuple>();
        public Stack<TypeAdapterConfig> Configs { get; } = new Stack<TypeAdapterConfig>();
        public TypeAdapterConfig Config => Configs.Peek();
        public int? MaxDepth { get; set; }
        public int Depth { get; set; }
        public HashSet<ParameterExpression> ExtraParameters { get; } = new HashSet<ParameterExpression>();

        internal bool IsSubFunction()
        {
            return this.MaxDepth.HasValue || this.ExtraParameters.Count > 0;
        }

        public CompileContext(TypeAdapterConfig config)
        {
            Configs.Push(config);
        }
    }
}