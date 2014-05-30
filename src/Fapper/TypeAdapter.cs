using System;
using System.Collections.Generic;

using Fpr.Adapters;
using Fpr.Utils;

namespace Fpr
{
    public static class TypeAdapter
    {

        static readonly Dictionary<int, FastInvokeHandler> _cache = new Dictionary<int, FastInvokeHandler>();
        static readonly object _cacheLock = new object();

        public static TDestination Adapt<TDestination>(object source)
        {
            return (TDestination)GetAdapter(source.GetType(), typeof(TDestination))(null, new object[] { source });
        }

        public static TDestination Adapt<TSource, TDestination>(TSource source)
        {
            return (TDestination)GetAdapter<TSource, TDestination>()(null, new object[] { source });
        }

        public static TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            return (TDestination)GetAdapter<TSource, TDestination>(true)(null, new object[] { source, destination });
        }

        public static object Adapt(object source, Type sourceType, Type destinationType)
        {
            return GetAdapter(sourceType, destinationType)(null, new object[] { source });
        }

        public static object Adapt(object source, object destination, Type sourceType, Type destinationType)
        {
            return GetAdapter(sourceType, destinationType, true)(null, new object[] { source, destination });
        }

        public static void Reset()
        {
            _cache.Clear();
        }

        public static void Reset<TSource, TDestination>()
        {
            var hashCode = ReflectionUtils.GetHashKey<TSource, TDestination>();

            if (_cache.ContainsKey(hashCode))
            {
                lock (_cacheLock)
                {
                    _cache.Remove(hashCode);
                }
            }
        }

        private static FastInvokeHandler GetAdapter(Type sourceType, Type destinationType, bool hasDestination = false)
        {
            int hashCode = ReflectionUtils.GetHashKey(sourceType, destinationType);

            if (_cache.ContainsKey(hashCode))
            {
                return _cache[hashCode];
            }

            lock (_cacheLock)
            {
                if (_cache.ContainsKey(hashCode))
                {
                    return _cache[hashCode];
                }

                Type[] arguments = hasDestination ? new Type[] { sourceType, destinationType } : new Type[] { sourceType };

                FastInvokeHandler invoker;

                if (ReflectionUtils.IsPrimitive(sourceType) && ReflectionUtils.IsPrimitive(destinationType))
                {
                    invoker = FastInvoker.GetMethodInvoker(typeof(PrimitiveAdapter<,>).MakeGenericType(sourceType, destinationType).GetMethod("Adapt", arguments));
                }
                else if (ReflectionUtils.IsCollection(sourceType) && ReflectionUtils.IsCollection(destinationType))
                {
                    invoker = FastInvoker.GetMethodInvoker(typeof(CollectionAdapter<,,>).MakeGenericType(sourceType, ReflectionUtils.ExtractElementType(destinationType), destinationType).GetMethod("Adapt", arguments));
                }
                else
                {
                    invoker = FastInvoker.GetMethodInvoker(typeof(ClassAdapter<,>).MakeGenericType(sourceType, destinationType).GetMethod("Adapt", arguments));
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
