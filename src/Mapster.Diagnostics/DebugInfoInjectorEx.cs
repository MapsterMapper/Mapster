using ExpressionDebugger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Mapster.Diagnostics
{
    public class DebugInfoInjectorEx: DebugInfoInjector
    {
        public DebugInfoInjectorEx(string filename): base (filename) { }

        public DebugInfoInjectorEx(TextWriter writer): base(writer) { }

        bool onNonPublicAssignment;
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var needTransform = false;
            MemberExpression access = null;
            if (node.NodeType == ExpressionType.Assign && node.Left.NodeType == ExpressionType.MemberAccess)
            {
                access = (MemberExpression)node.Left;
                if (!IsVisible(access.Member, true))
                {
                    onNonPublicAssignment = true;
                    needTransform = true;
                }
            }
            node = (BinaryExpression)base.VisitBinary(node);

            if (!needTransform)
                return node;

            return SetNonPublicMember(access.Member, node.Left, node.Right);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            node = (ConstantExpression)base.VisitConstant(node);

            if (CanEmitConstant(node.Value, node.Type))
                return node;

            return GetNonPublicObject(node.Value, node.Type);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var needTransform = false;
            if (onNonPublicAssignment)
            {
                onNonPublicAssignment = false;
                needTransform = false;
            }
            else if (!IsVisible(node.Member, false))
                needTransform = true;

            node = (MemberExpression)base.VisitMember(node);

            if (!needTransform)
                return node;

            return GetNonPublicMember(node.Member, node.Expression);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            node = (MemberInitExpression)base.VisitMemberInit(node);

            var pub = new List<MemberBinding>();
            var nonPub = new List<MemberAssignment>();

            foreach (var item in node.Bindings)
            {
                if (item.BindingType == MemberBindingType.Assignment && !IsVisible(item.Member, true))
                    nonPub.Add((MemberAssignment)item);
                else
                    pub.Add(item);
            }

            if (nonPub.Count == 0)
                return node;

            var p = Expression.Variable(node.Type);
            var list = new List<Expression>
            {
                Expression.Assign(p, node.Update(node.NewExpression, pub))
            };
            list.AddRange(
                nonPub.Select(ma =>
                    SetNonPublicMember(ma.Member, p, ma.Expression)));
            list.Add(p);
            return Expression.Block(node.Type, new[] { p }, list);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            node = (MethodCallExpression)base.VisitMethodCall(node);

            if (IsVisible(node.Method))
                return node;

            return CallNonPublicMethod(node.Method, node.Object, node.Arguments);
        }

        private static Expression CallNonPublicMethod(MethodInfo method, Expression instance, IEnumerable<Expression> parameters)
        {
            if (instance == null)
                instance = Expression.Constant(null, typeof(object));
            var methodExpr = GetNonPublicObject(method, typeof(MethodInfo));
            var invoke = typeof(MethodInfo).GetMethod(nameof(MethodInfo.Invoke), new[] { typeof(object), typeof(object[]) });
            var call = Expression.Call(
                methodExpr,
                invoke,
                instance.To(typeof(object)),
                Expression.NewArrayInit(
                    typeof(object),
                    parameters.Select(p => p.To(typeof(object)))));
            if (method.ReturnType != typeof(void) && IsVisible(method.ReturnType))
                return call.To(method.ReturnType);
            else
                return call;
        }

        private static Expression SetNonPublicMember(MemberInfo member, Expression instance, Expression value)
        {
            return member is FieldInfo fi
                ? SetNonPublicField(fi, instance, value)
                : SetNonPublicProperty((PropertyInfo)member, instance, value);
        }

        private static Expression SetNonPublicField(FieldInfo field, Expression instance, Expression value)
        {
            if (instance == null)
                instance = Expression.Constant(null, typeof(object));
            var fieldExpr = GetNonPublicObject(field, typeof(FieldInfo));
            var setValue = typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new[] { typeof(object), typeof(object) });
            var call = Expression.Call(
                fieldExpr,
                setValue,
                instance.To(typeof(object)),
                value.To(typeof(object)));
            return call;
        }

        private static Expression SetNonPublicProperty(PropertyInfo property, Expression instance, Expression value)
        {
            if (instance == null)
                instance = Expression.Constant(null, typeof(object));
            var propertyExpr = GetNonPublicObject(property, typeof(PropertyInfo));
            var setValue = typeof(PropertyInfo).GetMethod(nameof(PropertyInfo.SetValue), new[] { typeof(object), typeof(object) });
            var call = Expression.Call(
                propertyExpr,
                setValue,
                instance.To(typeof(object)),
                value.To(typeof(object)));
            return call;
        }

        private static Expression GetNonPublicMember(MemberInfo member, Expression instance)
        {
            return member is FieldInfo fi
                ? GetNonPublicField(fi, instance)
                : GetNonPublicProperty((PropertyInfo)member, instance);
        }

        private static Expression GetNonPublicField(FieldInfo field, Expression instance)
        {
            if (instance == null)
                instance = Expression.Constant(null, typeof(object));
            var fieldExpr = GetNonPublicObject(field, typeof(FieldInfo));
            var getValue = typeof(FieldInfo).GetMethod(nameof(FieldInfo.GetValue));
            var call = Expression.Call(
                fieldExpr,
                getValue,
                instance.To(typeof(object)));
            if (IsVisible(field.FieldType))
                return call.To(field.FieldType);
            else
                return call;
        }

        private static Expression GetNonPublicProperty(PropertyInfo property, Expression instance)
        {
            if (instance == null)
                instance = Expression.Constant(null, typeof(object));
            var propertyExpr = GetNonPublicObject(property, typeof(PropertyInfo));
            var getValue = typeof(PropertyInfo).GetMethod(nameof(PropertyInfo.GetValue), new[] { typeof(object) });
            var call = Expression.Call(
                propertyExpr,
                getValue,
                instance.To(typeof(object)));
            if (IsVisible(property.PropertyType))
                return call.To(property.PropertyType);
            else
                return call;
        }

        private static Expression GetNonPublicObject(object value, Type type)
        {
            var i = GlobalReference.GetIndex(value);
            return Expression.Call(
                    typeof(GlobalReference).GetMethod(nameof(GlobalReference.GetObject)),
                    Expression.Constant(i)).To(type);
        }

        private static bool CanEmitConstant(object value, Type type)
        {
            if (value == null
                || type.GetTypeInfo().IsPrimitive
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

        private static bool IsVisible(MemberInfo mi, bool isSet)
        {
            if (mi is FieldInfo fi)
            {
                if (!fi.IsPublic)
                    return false;
            }
            else if (mi is PropertyInfo pi)
            {
                var m = isSet ? pi.GetSetMethod() : pi.GetGetMethod();
                if (m == null)
                    return false;
            }

            Type dt = mi.DeclaringType;
            return dt == null || IsVisible(dt);
        }
    }

}
