using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Mapster.Adapters;
using Mapster.Utils;

namespace Mapster
{
    public static class TypeAdapter
    {
        private static readonly ConcurrentDictionary<ulong, Func<object, object>> _adaptDict = new ConcurrentDictionary<ulong, Func<object, object>>();
        private static readonly ConcurrentDictionary<ulong, Func<object, object, object>> _adaptTargetDict = new ConcurrentDictionary<ulong, Func<object, object, object>>();

        private static Func<object, object> CreateAdaptFunc(Type sourceType, Type destinationType)
        {
            var typeAdapterType = typeof (TypeAdapter<,>).MakeGenericType(sourceType, destinationType);
            var func = typeAdapterType.GetMethod("Adapt", new[] {sourceType});

            var p = Expression.Parameter(typeof (object));
            var convert = Expression.Convert(p, sourceType);
            var adapt = Expression.Call(func, convert);
            var body = Expression.Convert(adapt, typeof (object));
            return Expression.Lambda<Func<object, object>>(body, p).Compile();
        }

        private static Func<object, object, object> CreateAdaptTargetFunc(Type sourceType, Type destinationType)
        {
            var typeAdapterType = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType);
            var func = typeAdapterType.GetMethod("Adapt", new[] { sourceType, destinationType });

            var p = Expression.Parameter(typeof(object));
            var p2 = Expression.Parameter(typeof (object));
            var convert = Expression.Convert(p, sourceType);
            var convert2 = Expression.Convert(p2, destinationType);
            var adapt = Expression.Call(func, convert, convert2);
            var body = Expression.Convert(adapt, typeof(object));
            return Expression.Lambda<Func<object, object, object>>(body, p).Compile();
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(object source)
        {
            var sourceType = source.GetType();
            var hash = ReflectionUtils.GetHashKey(sourceType, typeof (TDestination));
            var func = _adaptDict.GetOrAdd(hash, (ulong _) => CreateAdaptFunc(sourceType, typeof (TDestination)));
            return (TDestination) func(source);
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
            return TypeAdapter<TSource, TDestination>.Adapt(source);
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
            return TypeAdapter<TSource, TDestination>.Adapt(source, destination);
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
            var hash = ReflectionUtils.GetHashKey(sourceType, destinationType);
            var func = _adaptDict.GetOrAdd(hash, (ulong _) => CreateAdaptFunc(sourceType, destinationType));
            return func(source);
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
            var hash = ReflectionUtils.GetHashKey(sourceType, destinationType);
            var func = _adaptTargetDict.GetOrAdd(hash, _ => CreateAdaptTargetFunc(sourceType, destinationType));
            return func(source, destination);
        }

        /// <summary>
        /// Returns an instance representation of the adapter, mainly for DI/IOC situations.
        /// </summary>
        /// <returns>Instance of the adapter.</returns>
        public static IAdapter GetInstance()
        {
            return new Adapter();
        }
    }

    internal static class TypeAdapter<TSource, TDestination>
    {
        private static Func<int, TSource, TDestination> _adapt = CreateAdaptFunc();
        private static Func<int, TSource, TDestination, TDestination> _adaptTarget = CreateAdaptTargetFunc();

        private static string _adaptCode;
        private static string _adaptTargetCode;

        private static Func<int, TSource, TDestination> CreateAdaptFunc()
        {
            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var converter = config?.ConverterFactory;
            if (converter != null)
            {
                return (d, src) => converter().Resolve(src);
            }
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            if (config == null && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {typeof(TSource)} TDestination: {typeof(TDestination)}");
            }

            if (sourceType.IsPrimitiveRoot() && destinationType.IsPrimitiveRoot())
                return PrimitiveAdapter<TSource, TDestination>.CreateAdaptFunc().CompileWithCode(out _adaptCode);
            else if (sourceType.IsCollection() && destinationType.IsCollection())
                return CollectionAdapter<TSource, TDestination>.CreateAdaptFunc().CompileWithCode(out _adaptCode);
            else
                return ClassAdapter<TSource, TDestination>.CreateAdaptFunc().CompileWithCode(out _adaptCode);
        }

        private static Func<int, TSource, TDestination, TDestination> CreateAdaptTargetFunc()
        {
            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var converter = config?.ConverterFactory;
            if (converter != null)
            {
                return (d, src, dest) => converter().Resolve(src, dest);
            }
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            if (config == null && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {typeof(TSource)} TDestination: {typeof(TDestination)}");
            }

            if (sourceType.IsPrimitiveRoot() || destinationType.IsPrimitiveRoot())
                return PrimitiveAdapter<TSource, TDestination>.CreateAdaptTargetFunc().CompileWithCode(out _adaptTargetCode);
            else if (sourceType.IsCollection() && destinationType.IsCollection())
                return CollectionAdapter<TSource, TDestination>.CreateAdaptTargetFunc().CompileWithCode(out _adaptTargetCode);
            else
                return ClassAdapter<TSource, TDestination>.CreateAdaptTargetFunc().CompileWithCode(out _adaptTargetCode);
        }

        public static void Recompile()
        {
            _adapt = CreateAdaptFunc();
            _adaptTarget = CreateAdaptTargetFunc();
        }

        public static TDestination Adapt(TSource source)
        {
            return _adapt(0, source);
        }

        public static TDestination AdaptWithDepth(int depth, TSource source)
        {
            return _adapt(depth, source);
        }

        public static TDestination Adapt(TSource source, TDestination destination)
        {
            return _adaptTarget(0, source, destination);
        }

        public static TDestination AdaptWithDepth(int depth, TSource source, TDestination destination)
        {
            return _adaptTarget(depth, source, destination);
        }
    }
}
