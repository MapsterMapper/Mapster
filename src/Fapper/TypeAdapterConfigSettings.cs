using System.Collections.Generic;
using Fapper.Models;

namespace Fapper
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
    }

    internal class TypeAdapterConfigSettings<TSource>
    {
        public readonly List<string> IgnoreMembers = new List<string>();

        public readonly List<InvokerModel<TSource>> Resolvers = new List<InvokerModel<TSource>>();

        public void Reset()
        {
            IgnoreMembers.Clear();
            Resolvers.Clear();
        }

        public int MaxDepth { get; set; }

        /// <summary>
        /// This property only use TypeAdapter.Adapt() method. Project().To() not use this property. Default: true
        /// </summary>
        public bool? NewInstanceForSameType { get; set; }

        /// <summary>
        /// This property only use TypeAdapter.Adapt() method. Project().To() not use this property. Default: false
        /// </summary>
        public bool? IgnoreNullValues { get; set; }

    }
}