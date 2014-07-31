using System.Collections;
using System.Collections.Generic;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public static class CollectionAdapter<TSource, TDestinationElementType, TDestination>
        where TSource : IEnumerable
        where TDestination : IEnumerable
    {

        private static readonly CollectionAdapterModel _collectionAdapterModel = CreateCollectionAdapterModel();

        public static object Adapt(TSource source)
        {
            return Adapt(source, new Dictionary<int, int>());
        }

        public static object Adapt(TSource source, object destination)
        {
            return Adapt(source, destination, new Dictionary<int, int>());
        }

        public static object Adapt(TSource source, Dictionary<int, int> parameterIndexs)
        {
            if (parameterIndexs == null)
                parameterIndexs = new Dictionary<int, int>();

            return Adapt(source, null, parameterIndexs);
        }

        public static object Adapt(TSource source, object destination, Dictionary<int, int> parameterIndexs)
        {
            if (source == null)
                return null;
         
            #region Check MaxDepth

            var configSettings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

            var hasMaxDepth = false;

            if (configSettings != null)
            {
                if (configSettings.MaxDepth.GetValueOrDefault() > 0)
                {
                    hasMaxDepth = true;
                    if (MaxDepthExceeded(ref parameterIndexs, configSettings.MaxDepth.GetValueOrDefault()))
                        return null;
                }
            }

            #endregion

            var destinationType = typeof(TDestination);

            if (destinationType.IsArray)
            {
                #region CopyToArray

                byte i = 0;
                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? new TDestinationElementType[((ICollection)source).Count] : (TDestinationElementType[])destination;
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            array.SetValue(default(TDestinationElementType), i);
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
                        array.SetValue(adapterInvoker(null, new[] { item, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexs) : parameterIndexs) }), i);
                        i++;
                    }
                }

                return array;

                #endregion
            }
            
            if (destinationType.IsGenericType)
            {
                #region CopyToList

                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                var list = destination == null ? new List<TDestinationElementType>() : (List<TDestinationElementType>)destination;
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            list.Add(default(TDestinationElementType));
                        else if(hasInvoker)
                            list.Add((TDestinationElementType)adapterInvoker(null, new[] { item }));
                        else
                            list.Add((TDestinationElementType)item);
                    }
                }
                else
                {
                    foreach (var item in source)
                    {
                        list.Add((TDestinationElementType)adapterInvoker(null, new[] { item, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexs) : parameterIndexs) }));
                    }
                }

                return list;

                #endregion
            }
            
            if (destinationType == typeof(ArrayList))
            {
                #region CopyToArrayList

                var adapterInvoker = _collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? new ArrayList() : (ArrayList)destination;
                if (_collectionAdapterModel.IsPrimitive)
                {
                    bool hasInvoker = adapterInvoker != null;
                    foreach (var item in source)
                    {
                        if (item == null)
                            array.Add(default(TDestinationElementType));
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
                        array.Add(adapterInvoker(null, new[] { item, (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexs) : parameterIndexs) }));
                    }
                }

                return array;

                #endregion
            }

            return (TDestination)destination;
        }

        private static bool MaxDepthExceeded(ref Dictionary<int, int> parameterIndexs, int maxDepth)
        {
            if (parameterIndexs == null)
                parameterIndexs = new Dictionary<int, int>();

            int hashCode = typeof (TSource).GetHashCode() + typeof (TDestination).GetHashCode();

            if (parameterIndexs.ContainsKey(hashCode))
            {
                parameterIndexs[hashCode] = parameterIndexs[hashCode] + 1;

                if (parameterIndexs[hashCode] >= maxDepth)
                {
                    return true;
                }
            }
            else
            {
                parameterIndexs.Add(hashCode, 1);
            }
            return false;
        }


        private static CollectionAdapterModel CreateCollectionAdapterModel()
        {
            var cam = new CollectionAdapterModel();

            var sourceElementType = typeof(TSource).ExtractCollectionType();
            var destinationElementType = typeof(TDestinationElementType);

            if (destinationElementType.IsPrimitiveRoot() || destinationElementType == typeof(object))
            {
                cam.IsPrimitive = true;

                var converter = sourceElementType.CreatePrimitiveConverter(destinationElementType);
                if (converter != null)
                    cam.AdaptInvoker = converter;
            }
            else if (destinationElementType.IsCollection())
            {
                var methodInfo = typeof(CollectionAdapter<,,>)
                    .MakeGenericType(sourceElementType, destinationElementType.ExtractCollectionType(), destinationElementType)
                    .GetMethod("Adapt", new[] { sourceElementType, typeof(Dictionary<,>).MakeGenericType(typeof(int), typeof(int)) });
                cam.AdaptInvoker = FastInvoker.GetMethodInvoker(methodInfo);
            }
            else
            {
                var methodInfo = typeof(ClassAdapter<,>)
                    .MakeGenericType(sourceElementType, destinationElementType)
                    .GetMethod("Adapt", new[] { sourceElementType, typeof(Dictionary<,>).MakeGenericType(typeof(int), typeof(int)) });

                cam.AdaptInvoker = FastInvoker.GetMethodInvoker(methodInfo);
            }

            return cam;
        }

    }
}
