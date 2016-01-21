using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;

namespace Mapster
{
    public enum MapType
    {
        Map,
        InlineMap,
        MapToTarget,
        Projection,
    }

    public class TypeAdapterSettings
    {
        public readonly HashSet<string> IgnoreMembers = new HashSet<string>();
        public readonly HashSet<Type> IgnoreAttributes = new HashSet<Type>();
        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool? PreserveReference;
        public bool? ShallowCopyForSameType;
        public bool? IgnoreNullValues;
        public bool? NoInherit;
        public Type DestinationType;

        public readonly List<InvokerModel> Resolvers = new List<InvokerModel>();
        public LambdaExpression ConstructUsing;
        public Func<CompileArgument, LambdaExpression> ConverterFactory;
        public Func<CompileArgument, LambdaExpression> ConverterToTargetFactory;

        public void Apply(TypeAdapterSettings other)
        {
            if (this.NoInherit == null)
                this.NoInherit = other.NoInherit;

            if (!this.NoInherit.GetValueOrDefault())
            {
                if (this.PreserveReference == null)
                    this.PreserveReference = other.PreserveReference;
                if (this.ShallowCopyForSameType == null)
                    this.ShallowCopyForSameType = other.ShallowCopyForSameType;
                if (this.IgnoreNullValues == null)
                    this.IgnoreNullValues = other.IgnoreNullValues;

                this.IgnoreMembers.UnionWith(other.IgnoreMembers);
                this.IgnoreAttributes.UnionWith(other.IgnoreAttributes);
                this.DestinationTransforms.TryAdd(other.DestinationTransforms.Transforms);

                this.Resolvers.AddRange(other.Resolvers);
            }

            if (this.DestinationType == null 
                || other.DestinationType == null 
                || this.DestinationType.GetTypeInfo().IsAssignableFrom(other.DestinationType.GetTypeInfo()) 
                || other.DestinationType.GetTypeInfo().IsAssignableFrom(this.DestinationType.GetTypeInfo()))
            {
                if (!this.NoInherit.GetValueOrDefault())
                {
                    if (this.ConstructUsing == null)
                        this.ConstructUsing = other.ConstructUsing;
                }
                if (this.ConverterFactory == null)
                    this.ConverterFactory = other.ConverterFactory;
                if (this.ConverterToTargetFactory == null)
                    this.ConverterToTargetFactory = other.ConverterToTargetFactory;
            }
        }
    }

    public class CompileArgument
    {
        public Type SourceType;
        public Type DestinationType;
        public MapType MapType;
        public TypeAdapterSettings Settings;
        public CompileContext Context;
    }

    public class CompileContext
    {
        public readonly HashSet<TypeTuple> Running = new HashSet<TypeTuple>();
        public readonly TypeAdapterConfig Config;       
        
        public CompileContext(TypeAdapterConfig config)
        {
            this.Config = config;
        } 
    }
}