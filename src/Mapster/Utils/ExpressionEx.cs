using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Utils
{
    public static class ExpressionEx
    {
        public static Expression Assign(Expression left, Expression right)
        {
            var middle = left.Type == right.Type
                ? right
                : Expression.Convert(right, left.Type);
            return Expression.Assign(left, middle);
        }


        public static T CompileWithCode<T>(this Expression<T> exp, out string code)
        {
#if DEBUG
            var prop = typeof (Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            code = (string)prop.GetValue(exp);
#else
            code = null;
#endif
            return exp.Compile();
        }

    }
}
