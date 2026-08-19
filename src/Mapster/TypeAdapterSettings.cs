using Mapster.Enums;
using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
// ReSharper disable ArrangeAccessorOwnerBody

namespace Mapster
{
    [AdaptWith(AdaptDirectives.DestinationAsRecord)]
    public class TypeAdapterSettings : SettingStore
    {
        public Type? SourceType 
        {
            get => Get<Type>(nameof(SourceType));
            set => Set(nameof(SourceType), value);
        }
        public Type? DestinationType
        {
            get => Get<Type>(nameof(DestinationType));
            set => Set(nameof(DestinationType), value);
        }
        public IgnoreDictionary Ignore
        {
            get => Get(nameof(Ignore), () => new IgnoreDictionary());
        }
        public List<DestinationTransform> DestinationTransforms
        {
            get => Get(nameof(DestinationTransforms), () => new List<DestinationTransform>());
        }
        public NameMatchingStrategy NameMatchingStrategy
        {
            get => Get(nameof(NameMatchingStrategy), () => new NameMatchingStrategy());
            set => Set(nameof(NameMatchingStrategy), value);
        }

        public bool? PreserveReference
        {
            get => Get(nameof(PreserveReference));
            set => Set(nameof(PreserveReference), value);
        }
        public bool? DirectAssignmentForSameType
        {
            get => Get(nameof(DirectAssignmentForSameType));
            set => Set(nameof(DirectAssignmentForSameType), value);
        }

        public bool? ShallowCopyForSameType
        {
            get => Get(nameof(ShallowCopyForSameType));
            set => Set(nameof(ShallowCopyForSameType), value);
        }

        public bool? IgnoreNullValues
        {
            get => Get(nameof(IgnoreNullValues));
            set => Set(nameof(IgnoreNullValues), value);
        }
        public bool? MapEnumByName
        {
            get => Get(nameof(MapEnumByName));
            set => Set(nameof(MapEnumByName), value);
        }
        public bool? IgnoreNonMapped
        {
            get => Get(nameof(IgnoreNonMapped));
            set => Set(nameof(IgnoreNonMapped), value);
        }
        public bool? AvoidInlineMapping
        {
            get => Get(nameof(AvoidInlineMapping));
            set => Set(nameof(AvoidInlineMapping), value);
        }
        public bool? RequireDestinationMemberSource
        {
            get => Get(nameof(RequireDestinationMemberSource));
            set => Set(nameof(RequireDestinationMemberSource), value);
        }
        public int? MaxDepth
        {
            get => (int?) Get<object>(nameof(MaxDepth));
            set => Set(nameof(MaxDepth), value);
        }
        public bool? Unflattening
        {
            get => Get(nameof(Unflattening));
            set => Set(nameof(Unflattening), value);
        }
        public bool? SkipDestinationMemberCheck
        {
            get => Get(nameof(SkipDestinationMemberCheck));
            set => Set(nameof(SkipDestinationMemberCheck), value);
        }
        public bool? EnableNonPublicMembers
        {
            get => Get(nameof(EnableNonPublicMembers));
            set => Set(nameof(EnableNonPublicMembers), value);
        }
        public object? GenerateMapper
        {
            get => Get<object>(nameof(GenerateMapper));
            set => Set(nameof(GenerateMapper), value);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public bool? MapToPrimitive
        {
            get => Get(nameof(MapToPrimitive));
            set => Set(nameof(MapToPrimitive), value);
        }

        public bool? MapToTargetPrimitive
        {
            get => Get(nameof(MapToTargetPrimitive));
            set => Set(nameof(MapToTargetPrimitive), value);
        }

        public ProjectToTypeAutoMapping ProjectToTypeMapConfig
        {
            get => GetEnum(nameof(ProjectToTypeMapConfig), ()=> default(ProjectToTypeAutoMapping));
            set => Set(nameof(ProjectToTypeMapConfig), value);
        }

        public  Dictionary<string,UnaryExpression> ProjectToTypeResolvers
        {
            get => Get(nameof(ProjectToTypeResolvers), () => new Dictionary<string, UnaryExpression>());
            set => Set(nameof(ProjectToTypeResolvers), value);
        }


        public List<Func<IMemberModel, MemberSide, bool?>> ShouldMapMember
        {
            get => Get(nameof(ShouldMapMember), () => new List<Func<IMemberModel, MemberSide, bool?>>());
        }
        public List<Func<ResolverSourceInput, IMemberModel, CompileArgument, ResolverResult?>> ValueAccessingStrategies
        {
            get => Get(nameof(ValueAccessingStrategies), () => new List<Func<ResolverSourceInput, IMemberModel, CompileArgument, ResolverResult?>>());
        }
        public List<InvokerModel> Resolvers
        {
            get => Get(nameof(Resolvers), () => new List<InvokerModel>());
        }
        public List<ExtraSourceModel> ExtraSources
        {
            get => Get(nameof(ExtraSources), () => new List<ExtraSourceModel>());
        }
        public List<Func<CompileArgument, LambdaExpression>> BeforeMappingFactories
        {
            get => Get(nameof(BeforeMappingFactories), () => new List<Func<CompileArgument, LambdaExpression>>());
        }
        public List<Func<CompileArgument, LambdaExpression>> AfterMappingFactories
        {
            get => Get(nameof(AfterMappingFactories), () => new List<Func<CompileArgument, LambdaExpression>>());
        }
        public List<TypeTuple> Includes
        {
            get => Get(nameof(Includes), () => new List<TypeTuple>());
        }
        public List<Func<IMemberModel, MemberSide, string?>> GetMemberNames
        {
            get => Get(nameof(GetMemberNames), () => new List<Func<IMemberModel, MemberSide, string?>>());
        }
        public List<Func<IMemberModel, bool>> UseDestinationValues
        {
            get => Get(nameof(UseDestinationValues), () => new List<Func<IMemberModel, bool>>());
        }
        public Func<CompileArgument, LambdaExpression>? ConstructUsingFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>(nameof(ConstructUsingFactory));
            set => Set(nameof(ConstructUsingFactory), value);
        }
        public Func<CompileArgument, LambdaExpression>? ConverterFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>(nameof(ConverterFactory));
            set => Set(nameof(ConverterFactory), value);
        }
        public Func<CompileArgument, LambdaExpression>? ConverterToTargetFactory
        {
            get => Get<Func<CompileArgument, LambdaExpression>>(nameof(ConverterToTargetFactory));
            set => Set(nameof(ConverterToTargetFactory), value);
        }
        public bool? ApplyCustomConverterFactoryNullPropagation
        {
            get => Get(nameof(ApplyCustomConverterFactoryNullPropagation));
            set => Set(nameof(ApplyCustomConverterFactoryNullPropagation), value);
        }
        public object? MapToConstructor
        {
            get => Get<object>(nameof(MapToConstructor));
            set => Set(nameof(MapToConstructor), value);
        }
        public Action<TypeAdapterConfig>? Fork
        {
            get => Get<Action<TypeAdapterConfig>>(nameof(Fork));
            set => Set(nameof(Fork), value);
        }

        public List<string> UseDestinationMembers
        {
            get => Get(nameof(UseDestinationMembers), () => new List<string>());
        }

        public Expression? CustomDefaultValue 
        {
            get => Get<Expression>(nameof(CustomDefaultValue));
            set => Set(nameof(CustomDefaultValue), value);
        }

        public List<string> ReMapDestinationMembers
        {
            get => Get(nameof(ReMapDestinationMembers), () => new List<string>());
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