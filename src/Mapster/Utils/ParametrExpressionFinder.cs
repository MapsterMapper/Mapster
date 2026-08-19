using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Mapster.Utils
{
    sealed internal class ParametrExpressionFinder: ExpressionVisitor
    {
        private readonly List<ParameterExpression> _parameters = new();
              
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!_parameters.Contains(node))
                _parameters.Add(node);

            return base.VisitParameter(node);
        }

        public ReadOnlyCollection<ParameterExpression> Find(Expression expression)
        {
            _parameters.Clear();
            this.Visit(expression);
            return _parameters.AsReadOnly();
        }
    }

    internal static class ReplaceOvverideExpressionParam
    {
        readonly static ParametrExpressionFinder ParamFinder = new ();

        public static Expression Replace(Expression expression, params Expression[] to)
        {
           return new ParameterExpressionReplacer(ParamFinder.Find(expression), true, to).Visit(expression);
        }
    }
}
