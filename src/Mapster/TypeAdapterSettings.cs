using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Models;

namespace Mapster
{
    public class TypeAdapterSettings: SettingStore
    {
        public List<Func<IMemberModel, bool>> Ignores
        {
            get => Get("Ignores", () => new List<Func<IMemberModel, bool>>());
        }
        public IgnoreIfDictionary IgnoreIfs
        {
            get => Get("IgnoreIfs", () => new IgnoreIfDictionary());
        }
        public TransformsCollection DestinationTransforms
        {
            get => Get("DestinationTransforms", () => new TransformsCollection());
        }
        public NameMatchingStrategy NameMatchingStrategy
        {
            get => Get("NameMatchingStrategy", () => new NameMatchingStrategy());
            set => Set("NameMatchingStrategy", value);
        }

        public bool? PreserveReference
        {
            get => Get("PreserveReference");
            set => Set("PreserveReference", value);
        }
        public bool? ShallowCopyForSameType
        {
            get => Get("ShallowCopyForSameType");
            set => Set("ShallowCopyForSameType", value);
        }
        public bool? IgnoreNullValues
        {
            get => Get("IgnoreNullValues");
            set => Set("IgnoreNullValues", value);
        }
        public bool? MapEnumByName
        {
            get => Get("MapEnumByName");
            set => Set("MapEnumByName", value);
        }

        public List<Func<Expression, IMemberModel, CompileArgument, Expression>> ValueAccessingStrategies
        {
            get => Get("ValueAccessingStrategies", () => new List<Func<Expression, IMemberModel, CompileArgument, Expression>>());
            internal set => Set("ValueAccessingStrategies", value);
        }
        public List<InvokerModel> Resolvers
        {
            get => Get("Resolvers", () => new List<InvokerModel>());
        }
        public List<Func<CompileArgument, LambdaExpression>> AfterMappingFactories
        {
            get => Get("AfterMappingFactories", () => new List<Func<CompileArgument, LambdaExpression>>());
        }
        public List<TypeTuple> Includes
        {
            get => Get("Includes", () => new List<TypeTuple>());
        }
        public Func<CompileArgument, LambdaExpression> ConstructUsingFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>("ConstructUsingFactory");
            set => Set("ConstructUsingFactory", value);
        }
        public Func<CompileArgument, LambdaExpression> ConverterFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>("ConverterFactory");
            set => Set("ConverterFactory", value);
        }
        public Func<CompileArgument, LambdaExpression> ConverterToTargetFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>("ConverterToTargetFactory");
            set => Set("ConverterToTargetFactory", value);
        }

        internal bool Compiled { get; set; }
    }
}