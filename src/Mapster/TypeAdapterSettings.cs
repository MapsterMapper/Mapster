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
        public HashSet<string> IgnoreMembers { get; protected set; } = new HashSet<string>();
        public HashSet<Type> IgnoreAttributes { get; protected set; } = new HashSet<Type>();
        public HashSet<string> IncludeMembers { get; protected set; } = new HashSet<string>();
        public HashSet<Type> IncludeAttributes { get; protected set; } = new HashSet<Type>(); 
        public TransformsCollection DestinationTransforms { get; protected set; } = new TransformsCollection();

        public bool? PreserveReference { get; set; }
        public bool? ShallowCopyForSameType { get; set; }
        public bool? IgnoreNullValues { get; set; }
        public bool? NoInherit { get; set; }
        public Type DestinationType { get; set; }

        public List<InvokerModel> Resolvers { get; protected set; } = new List<InvokerModel>();
        public LambdaExpression ConstructUsing { get; set; }
        public Func<CompileArgument, LambdaExpression> ConverterFactory { get; set; }
        public Func<CompileArgument, LambdaExpression> ConverterToTargetFactory { get; set; }

        internal bool Compiled { get; set; }

        public void Apply(TypeAdapterSettings other)
        {
            if (this.NoInherit == null)
                this.NoInherit = other.NoInherit;

            if (this.NoInherit == true)
            {
                if (this.DestinationType != null && other.DestinationType != null)
                    return;
            }

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

            if (this.ConstructUsing == null)
                this.ConstructUsing = other.ConstructUsing;
            if (this.ConverterFactory == null)
                this.ConverterFactory = other.ConverterFactory;
            if (this.ConverterToTargetFactory == null)
                this.ConverterToTargetFactory = other.ConverterToTargetFactory;
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