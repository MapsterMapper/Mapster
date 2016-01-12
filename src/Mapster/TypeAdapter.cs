using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Adapters;
using Mapster.Models;

namespace Mapster
{
    public static class TypeAdapter
    {
        private static readonly ConcurrentDictionary<TypeTuple, Func<object, object>> _adaptDict = new ConcurrentDictionary<TypeTuple, Func<object, object>>();
        private static readonly ConcurrentDictionary<TypeTuple, Func<object, object, object>> _adaptTargetDict = new ConcurrentDictionary<TypeTuple, Func<object, object, object>>();
        private static readonly ConcurrentDictionary<TypeTuple, Func<ITypeAdapter>> _getAdapter = new ConcurrentDictionary<TypeTuple, Func<ITypeAdapter>>();
        private static readonly ConcurrentDictionary<TypeTuple, Func<TypeAdapterConfigSettingsBase>> _getSettings = new ConcurrentDictionary<TypeTuple, Func<TypeAdapterConfigSettingsBase>>();

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

        private static Func<ITypeAdapter> CreateGetAdapter(Type sourceType, Type destinationType)
        {
            var typeAdapterType = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType);
            var func = typeAdapterType.GetMethod("GetAdapter", Type.EmptyTypes);
            return Expression.Lambda<Func<ITypeAdapter>>(Expression.Call(func)).Compile();
        }

        private static Func<TypeAdapterConfigSettingsBase> CreateGetSettings(Type sourceType, Type destinationType)
        {
            var typeAdapterType = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType);
            var func = typeAdapterType.GetMethod("GetSettings", Type.EmptyTypes);
            return Expression.Lambda<Func<TypeAdapterConfigSettingsBase>>(Expression.Call(func)).Compile();
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
            var hash = new TypeTuple(sourceType, typeof (TDestination));
            var func = _adaptDict.GetOrAdd(hash, (TypeTuple _) => CreateAdaptFunc(sourceType, typeof (TDestination)));
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
            var hash = new TypeTuple(sourceType, destinationType);
            var func = _adaptDict.GetOrAdd(hash, (TypeTuple _) => CreateAdaptFunc(sourceType, destinationType));
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
            var hash = new TypeTuple(sourceType, destinationType);
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

        internal static readonly List<ITypeAdapter> Adapters = new List<ITypeAdapter>
        {
            new CollectionAdapter(),
            new ClassAdapter(),
            new PrimitiveAdapter(),
        };

        public static ITypeAdapter GetAdapter(Type sourceType, Type destinationType)
        {
            var hash = new TypeTuple(sourceType, destinationType);
            var func = _getAdapter.GetOrAdd(hash, _ => CreateGetAdapter(sourceType, destinationType));
            return func();
        }

        public static TypeAdapterConfigSettingsBase GetSettings(Type sourceType, Type destinationType)
        {
            var hash = new TypeTuple(sourceType, destinationType);
            var func = _getSettings.GetOrAdd(hash, _ => CreateGetSettings(sourceType, destinationType));
            return func();
        }
    }

    internal static class TypeAdapter<TSource, TDestination>
    {
        private static Func<TSource, TDestination> _adapt;
        private static Func<TSource, TDestination, TDestination> _adaptTarget;
        //private static int _maxDepth;

        static TypeAdapter()
        {
            Recompile();
        }

        private static ITypeResolver<TSource, TDestination> _resolver; 
        private static Func<TSource, TDestination> CreateAdaptFunc()
        {
            if (_resolver != null)
                return _resolver.Resolve;

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            if (config == null && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {typeof(TSource)} TDestination: {typeof(TDestination)}");
            }

            var adapter = TypeAdapterConfig.GlobalSettings.CustomAdapters.Concat(TypeAdapter.Adapters)
                .First(a => a.CanAdapt(sourceType, destinationType));

            return adapter.CreateAdaptFunc<TSource, TDestination>();
        }

        private static Func<TSource, TDestination, TDestination> CreateAdaptTargetFunc()
        {
            var resolverWithTarget = _resolver as ITypeResolverWithTarget<TSource, TDestination>;
            if (resolverWithTarget != null)
                return resolverWithTarget.Resolve;

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            if (config == null && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {typeof(TSource)} TDestination: {typeof(TDestination)}");
            }

            var adapter = TypeAdapterConfig.GlobalSettings.CustomAdapters.Concat(TypeAdapter.Adapters)
                .Select(a => a as ITypeAdapterWithTarget)
                .First(a => a?.CanAdapt(sourceType, destinationType) == true);

            return adapter.CreateAdaptTargetFunc<TSource, TDestination>();
        }

        public static void Recompile()
        {
            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            //_maxDepth = config?.MaxDepth ?? -1;
            _resolver = config?.ConverterFactory?.Invoke();
            _adapt = CreateAdaptFunc();
            _adaptTarget = CreateAdaptTargetFunc();
        }

        public static ITypeAdapter GetAdapter()
        {
            if (_resolver != null)
                return null;

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var config = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            if (config == null && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {typeof(TSource)} TDestination: {typeof(TDestination)}");
            }

            return TypeAdapterConfig.GlobalSettings.CustomAdapters.Concat(TypeAdapter.Adapters)
                .Select(a => a as ITypeAdapterWithTarget)
                .First(a => a?.CanAdapt(sourceType, destinationType) == true);
        }

        public static TypeAdapterConfigSettingsBase GetSettings()
        {
            return TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
        }

        public static TDestination Adapt(TSource source)
        {
            var result = _adapt(source);
            MapContext.Clear();
            return result;
        }

        public static TDestination AdaptWithContext(TSource source)
        {
            return _adapt(source);
        }

        public static TDestination Adapt(TSource source, TDestination destination)
        {
            var result = _adaptTarget(source, destination);
            MapContext.Clear();
            return result;
        }

        public static TDestination AdaptWithContext(TSource source, TDestination destination)
        {
            return _adaptTarget(source, destination);
        }
    }
}
