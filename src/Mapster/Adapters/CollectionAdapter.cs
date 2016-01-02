using System;
using System.Collections;
using System.Collections.Generic;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public static class CollectionAdapter<TSourceElement, TSource, TDestinationElement, TDestination>
        where TSource : IEnumerable
        where TDestination : IEnumerable
    {

        private static readonly CollectionAdapterModel _collectionAdapterModel = CreateCollectionAdapterModel();
        private static readonly long _hashCode = ReflectionUtils.GetHashKey<TSourceElement, TDestinationElement>();

        public static object Adapt(TSource source)
        {
            return Adapt(source, true, new Dictionary<long, int>());
        }

        public static object Adapt(TSource source, object destination)
        {
            return Adapt(source, destination, new Dictionary<long, int>());
        }

        public static object Adapt(TSource source, bool evaluateMaxDepth, Dictionary<long, int> parameterIndexes)
        {
            if (parameterIndexes == null)
                parameterIndexes = new Dictionary<long, int>();

            return Adapt(source, null, parameterIndexes);
        }

        public static object Adapt(TSource source, object destination, Dictionary<long, int> parameterIndexes)
        {
            if (source == null)
                return null;
         
            #region Check MaxDepth

            var configSettings = TypeAdapterConfig<TSourceElement, TDestinationElement>.ConfigSettings;

            var hasMaxDepth = false;

            if (configSettings != null)
            {
                int maxDepth = configSettings.MaxDepth.GetValueOrDefault();
                if (maxDepth > 0)
                {
                    hasMaxDepth = true;
                    if (MaxDepthExceeded(ref parameterIndexes, maxDepth, true))
                        return null;
                }
            }

            #endregion

            var destinationType = typeof(TDestination);

            if (destinationType.IsArray)
            {
                #region CopyToArray

                int i = 0;
                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? new TDestinationElement[((ICollection)source).Count] : (TDestinationElement[])destination;
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            array.SetValue(default(TDestinationElement), i);
                        if (hasInvoker)
                            array.SetValue(adapterInvoker(null, new[] { item }), i);
                        else
                            array.SetValue(item, i);
                        i++;
                    }
                }
                else
                {
                    foreach (var item in source)
                    {
                        array.SetValue(adapterInvoker(null, new[] { item, false, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexes) : parameterIndexes) }), i);
                        i++;
                    }
                }

                return array;

                #endregion
            }

            var canInstantiate = !destinationType.IsInterface && typeof (ICollection<TDestinationElement>).IsAssignableFrom(destinationType);
            if (canInstantiate || destinationType.IsAssignableFrom(typeof(List<TDestinationElement>)))
            {
                #region CopyToList

                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                ICollection<TDestinationElement> list;
                if (destination == null)
                {
                    list = canInstantiate
                        ? (ICollection<TDestinationElement>) ActivatorExtensions.CreateInstance(destinationType)
                        : new List<TDestinationElement>();
                }
                else
                {
                    list = (ICollection<TDestinationElement>)destination;
                }
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            list.Add(default(TDestinationElement));
                        else if (hasInvoker)
                            list.Add((TDestinationElement)adapterInvoker(null, new[] { item }));
                        else
                            list.Add((TDestinationElement)item);
                    }
                }
                else
                {
                    foreach (var item in source)
                    {
                        list.Add((TDestinationElement)adapterInvoker(null, new[] { item, false, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexes) : parameterIndexes) }));
                    }
                }

                return list;

                #endregion
            }
            
            if (!destinationType.IsInterface && typeof(IList).IsAssignableFrom(destinationType))
            {
                #region CopyToArrayList

                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? (IList)ActivatorExtensions.CreateInstance(destinationType) : (IList)destination;
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            array.Add(default(TDestinationElement));
                        else if(hasInvoker)
                            array.Add(adapterInvoker(null, new[] { item }));
                        else
                            array.Add(item);
                    }
                }
                else
                {
                    foreach (var item in source)
                    {
                        array.Add(adapterInvoker(null, new[] { item, true, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexes) : parameterIndexes) }));
                    }
                }

                return array;

                #endregion
            }

            return (TDestination)destination;
        }

        private static CollectionAdapterModel CreateCollectionAdapterModel()
        {
            var cam = new CollectionAdapterModel();

            var sourceElementType = typeof(TSource).ExtractCollectionType();
            var destinationElementType = typeof(TDestinationElement);

            if (destinationElementType.IsPrimitiveRoot() || destinationElementType == typeof(object))
            {
                cam.IsPrimitive = true;

                var converter = sourceElementType.CreatePrimitiveConverter(destinationElementType);
                if (converter != null)
                    cam.AdaptInvoker = converter;
            }
            else if (destinationElementType.IsCollection())
            {
                var methodInfo = typeof(CollectionAdapter<,,,>)
                    .MakeGenericType(sourceElementType.ExtractCollectionType(), sourceElementType, destinationElementType.ExtractCollectionType(), destinationElementType)
                    .GetMethod("Adapt", new[] { sourceElementType, typeof(bool), typeof(Dictionary<,>).MakeGenericType(typeof(long), typeof(int)) });
                cam.AdaptInvoker = FastInvoker.GetMethodInvoker(methodInfo);
            }
            else
            {
                var methodInfo = typeof(ClassAdapter<,>)
                    .MakeGenericType(sourceElementType, destinationElementType)
                    .GetMethod("Adapt", new[] { sourceElementType, typeof(bool), typeof(Dictionary<,>).MakeGenericType(typeof(long), typeof(int)) });

                cam.AdaptInvoker = FastInvoker.GetMethodInvoker(methodInfo);
            }

            return cam;
        }

        private static bool MaxDepthExceeded(ref Dictionary<long, int> parameterIndexes, int maxDepth, bool evaluateMaxDepth)
        {
            if (parameterIndexes == null)
                parameterIndexes = new Dictionary<long, int>();

            if (parameterIndexes.ContainsKey(_hashCode))
            {
                int index = parameterIndexes[_hashCode];
                if (evaluateMaxDepth)
                {
                    index++;
                    parameterIndexes[_hashCode] = index;
                }

                if (index > maxDepth)
                {
                    return true;
                }
            }
            else if (evaluateMaxDepth)
            {
                parameterIndexes.Add(_hashCode, 1);
            }
            return false;
        }

    }
}
