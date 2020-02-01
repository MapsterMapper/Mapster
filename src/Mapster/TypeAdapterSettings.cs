using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster
{
    public class TypeAdapterSettings : SettingStore
    {
        public IgnoreDictionary Ignore
        {
            get => Get("Ignore", () => new IgnoreDictionary());
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
        public bool? IgnoreNonMapped
        {
            get => Get("IgnoreNonMapped");
            set => Set("IgnoreNonMapped", value);
        }
        public bool? AvoidInlineMapping
        {
            get => Get("AvoidInlineMapping");
            set => Set("AvoidInlineMapping", value);
        }
        public int? MaxDepth
        {
            get => Get<int?>("MaxDepth");
            set => Set("MaxDepth", value);
        }
        public bool? Unflattening
        {
            get => Get("Unflattening");
            set => Set("Unflattening", value);
        }
        public bool? SkipDestinationMemberCheck
        {
            get => Get("SkipDestinationMemberCheck");
            set => Set("SkipDestinationMemberCheck", value);
        }

        public List<Func<IMemberModel, MemberSide, bool?>> ShouldMapMember
        {
            get => Get("ShouldMapMember", () => new List<Func<IMemberModel, MemberSide, bool?>>());
        }
        public List<Func<Expression, IMemberModel, CompileArgument, Expression?>> ValueAccessingStrategies
        {
            get => Get("ValueAccessingStrategies", () => new List<Func<Expression, IMemberModel, CompileArgument, Expression?>>());
        }
        public List<InvokerModel> Resolvers
        {
            get => Get("Resolvers", () => new List<InvokerModel>());
        }
        public List<object> ExtraSources
        {
            get => Get("ExtraSources", () => new List<object>());
        }
        public List<Func<CompileArgument, LambdaExpression>> BeforeMappingFactories
        {
            get => Get("BeforeMappingFactories", () => new List<Func<CompileArgument, LambdaExpression>>());
        }
        public List<Func<CompileArgument, LambdaExpression>> AfterMappingFactories
        {
            get => Get("AfterMappingFactories", () => new List<Func<CompileArgument, LambdaExpression>>());
        }
        public List<TypeTuple> Includes
        {
            get => Get("Includes", () => new List<TypeTuple>());
        }
        public List<Func<IMemberModel, string?>> GetMemberNames
        {
            get => Get("GetMemberNames", () => new List<Func<IMemberModel, string?>>());
        }
        public List<Func<IMemberModel, bool>> UseDestinationValues
        {
            get => Get("UseDestinationValues", () => new List<Func<IMemberModel, bool>>());
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
        public object? MapToConstructor
        {
            get => Get<object?>("MapToConstructor");
            set => Set("MapToConstructor", value);
        }

        internal bool Compiled { get; set; }

        public TypeAdapterSettings Clone()
        {
            var settings = new TypeAdapterSettings();
            settings.Apply(this);
            return settings;
        }
    }
}