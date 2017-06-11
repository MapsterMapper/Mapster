using ExpressionDebugger;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Mapster.Diagnostics
{
    public class DebugInfoInjectorEx: DebugInfoInjector
    {
        public DebugInfoInjectorEx(string filename): base (filename) { }

        public DebugInfoInjectorEx(TextWriter writer): base(writer) { }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            node = (ConstantExpression)base.VisitConstant(node);

            if (CanEmitConstant(node.Value, node.Type))
                return node;

            return GetNonPublicObject(node.Value, node.Type);
        }

        private static Expression GetNonPublicObject(object value, Type type)
        {
            var i = GlobalReference.GetIndex(value);
            return Expression.Convert(
                Expression.Call(
                    typeof(GlobalReference).GetMethod(nameof(GlobalReference.GetObject)),
                    Expression.Constant(i)),
                type);
        }

        private static bool CanEmitConstant(object value, Type type)
        {
            if (value == null
                || type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal))
                return true;

            if (value is Type t)
                return IsVisible(t);

            if (value is MethodBase mb)
                return IsVisible(mb);

            return false;
        }

        private static bool IsVisible(Type t)
        {
            return t is TypeBuilder
                || t.IsGenericParameter
                || t.IsVisible;
        }

        private static bool IsVisible(MethodBase mb)
        {
            if (mb is DynamicMethod || !mb.IsPublic)
                return false;

            Type dt = mb.DeclaringType;
            return dt == null || IsVisible(dt);
        }
    }

}
