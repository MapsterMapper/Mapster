using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class RecordTypeAdapter : BaseClassAdapter
    {
        public override int? Priority(Type sourceType, Type destinationType, MapType mapType)
        {
            if (sourceType == typeof (string) || sourceType == typeof (object))
                return null;

            if (!destinationType.IsRecordType())
                return null;

            return -151;
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (arg.Context.Config.RequireExplicitMapping
                && !arg.Context.Config.Dict.ContainsKey(new TypeTuple(arg.SourceType, arg.DestinationType)))
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists for the following mapping: TSource: {arg.SourceType} TDestination: {arg.DestinationType}");
            }

            return base.CreateExpressionBody(source, destination, arg);
        }

        protected override Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.Settings.ConstructUsing != null)
                return base.CreateInstantiationExpression(source, arg);

            var classConverter = CreateClassConverter(source, null, arg);
            var properties = classConverter.Members;

            var arguments = new List<Expression>();
            foreach (var property in properties)
            {
                var parameterInfo = (ParameterInfo) property.SetterInfo;
                var defaultValue = parameterInfo.IsOptional ? parameterInfo.RawDefaultValue : parameterInfo.ParameterType.GetDefault();

                Expression getter;
                if (property.Getter == null)
                {
                    getter = Expression.Constant(defaultValue, property.Setter.Type);
                }
                else
                {
                    getter = CreateAdaptExpression(property.Getter, property.Setter.Type, arg);

                    if (arg.Settings.IgnoreNullValues == true && (!property.Getter.Type.GetTypeInfo().IsValueType || property.Getter.Type.IsNullable()))
                    {
                        var condition = Expression.NotEqual(property.Getter, Expression.Constant(null, property.Getter.Type));
                        getter = Expression.Condition(condition, getter, Expression.Constant(defaultValue, property.Setter.Type));
                    }
                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo, arguments);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, arg);
        }

        protected override ClassModel GetClassModel(Type destinationType)
        {
            var props = destinationType.GetPublicFieldsAndProperties();
            var names = props.Select(p => p.Name).ToHashSet();
            return (from ctor in destinationType.GetConstructors()
                    let ps = ctor.GetParameters()
                    where ps.Length > 0 && names.IsSupersetOf(ps.Select(p => p.Name.ToProperCase()))
                    orderby ps.Length descending
                    select new ClassModel
                    {
                        ConstructorInfo = ctor,
                        Members = ps.Select(ReflectionUtils.CreateModel).ToList()
                    }).First();
        }

    }

}
