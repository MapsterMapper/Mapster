using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Models;

namespace Mapster
{

    internal class TypeAdapterConfigSettings
    {
        public TypeAdapterConfigSettings()
        {
            NewInstanceForSameType = true;
        }

        /// <summary>
        /// This property only use TypeAdapter.Adapt() method. Project().To() not use this property. Default: true
        /// </summary>
        internal bool NewInstanceForSameType { get; set; }

        internal IDictionary<Type, Expression> CombinedTransforms
        {
            get { return TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms; }
        }
    }

    internal class TypeAdapterConfigSettings<TSource, TDestination> : TypeAdapterConfigSettingsBase
    {
        public readonly List<InvokerModel> Resolvers = new List<InvokerModel>();
        public Expression<Func<TDestination>> ConstructUsing;
        public Func<ITypeResolver<TSource, TDestination>> ConverterFactory;
        
        public void Reset()
        {
            IgnoreMembers.Clear();
            Resolvers.Clear();
            DestinationTransforms.Clear();
            ConstructUsing = null;
            ConverterFactory = null;
        }

        public override List<object> GetResolversAsObjects()
        {
            return new List<object>(Resolvers);
        }
    }

    internal abstract class TypeAdapterConfigSettingsBase
    {
        public readonly List<string> IgnoreMembers = new List<string>();

        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool? CircularReferenceCheck;

        /// <summary>
        /// This property only use TypeAdapter.Adapt() method. Project().To() not use this property. Default: true
        /// </summary>
        public bool? SameInstanceForSameType;

        /// <summary>
        /// This property only use TypeAdapter.Adapt() method. Project().To() not use this property. Default: false
        /// </summary>
        public bool? IgnoreNullValues;

        /// <summary>
        /// Source type of the inherited config
        /// </summary>
        public Type InheritedSourceType;

        /// <summary>
        /// Destination type of the inherited config
        /// </summary>
        public Type InheritedDestinationType;
        
        public abstract List<object> GetResolversAsObjects();


    }
}