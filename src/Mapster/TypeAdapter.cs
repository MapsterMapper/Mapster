using System;
using System.Collections.Generic;
using Mapster.Adapters;
using Mapster.Utils;

namespace Mapster
{
    public static class TypeAdapter
    {

        private static readonly Dictionary<long, FastInvokeHandler> _cache = new Dictionary<long, FastInvokeHandler>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(object source)
        {
            return (TDestination)GetAdapter(source.GetType(), typeof(TDestination))(null, new[] { source });
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(TSource source)
        {
            return (TDestination)GetAdapter<TSource, TDestination>()(null, new object[] { source });
        }

        /// <summary>
        /// Adapte the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            return (TDestination)GetAdapter<TSource, TDestination>(true)(null, new object[] { source, destination });
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(object source, Type sourceType, Type destinationType)
        {
            return GetAdapter(sourceType, destinationType)(null, new[] { source });
        }

        /// <summary>
        /// Adapte the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(object source, object destination, Type sourceType, Type destinationType)
        {
            return GetAdapter(sourceType, destinationType, true)(null, new[] { source, destination });
        }

        /// <summary>
        /// Returns an instance representation of the adapter, mainly for DI/IOC situations.
        /// </summary>
        /// <returns>Instance of the adapter.</returns>
        public static IAdapter GetInstance()
        {
            return new Adapter();
        }

        private static FastInvokeHandler GetAdapter(Type sourceType, Type destinationType, bool hasDestination = false)
        {
            FastInvokeHandler adapter;

            if (_cache.TryGetValue(ReflectionUtils.GetHashKey(sourceType, destinationType) * (hasDestination ? -1 : 1), out adapter))
            {
                return adapter;
            }

            lock (_cacheLock)
            {
                long hashCode = ReflectionUtils.GetHashKey(sourceType, destinationType) * (hasDestination ? -1 : 1);

                if (_cache.TryGetValue(hashCode, out adapter))
                {
                    return adapter;
                }

                Type[] arguments = hasDestination ? new[] { sourceType, destinationType } : new[] { sourceType };

                FastInvokeHandler invoker;

                if (sourceType.IsPrimitiveRoot() && destinationType.IsPrimitiveRoot())
                {
                    invoker = FastInvoker.GetMethodInvoker(
                                typeof (PrimitiveAdapter<,>).MakeGenericType(sourceType, destinationType)
                                    .GetMethod("Adapt", arguments));
                }
                else if (sourceType.IsCollection() && destinationType.IsCollection())
                {
                    invoker = FastInvoker.GetMethodInvoker(
                            typeof (CollectionAdapter<,,,>).MakeGenericType(sourceType.ExtractCollectionType(), sourceType,
                                destinationType.ExtractCollectionType(), destinationType)
                                .GetMethod("Adapt", arguments));
                }
                else
                {
                    invoker = FastInvoker.GetMethodInvoker(
                            typeof (ClassAdapter<,>).MakeGenericType(sourceType, destinationType)
                                .GetMethod("Adapt", arguments));
                }

                _cache.Add(hashCode, invoker);
                return invoker;
            }
        }


        private static FastInvokeHandler GetAdapter<TSource, TDestination>(bool hasDestination = false)
        {
            return GetAdapter(typeof(TSource), typeof(TDestination), hasDestination);
        }

    }
}
