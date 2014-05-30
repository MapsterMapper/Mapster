using System.Collections;
using System.Collections.Generic;
using Fapper.Models;
using Fapper.Utils;

namespace Fapper.Adapters
{
    public class CollectionAdapter<TSource, TDestinationElementType, TDestination>
        where TSource : IEnumerable
        where TDestination : IEnumerable
    {

        private static CollectionAdapterModel _collectionAdapterModel = CreateCollectionAdapterModel();

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

            var config = TypeAdapterConfig<TSource, TDestination>.Configuration;
            var hasConfig = config != null;

            var hasMaxDepth = hasConfig && config.MaxDepth > 0;

            if (hasMaxDepth)
            {
                if (parameterIndexs == null)
                    parameterIndexs = new Dictionary<int, int>();

                int hashCode = typeof(TSource).GetHashCode() + typeof(TDestination).GetHashCode();

                if (parameterIndexs.ContainsKey(hashCode))
                {
                    parameterIndexs[hashCode] = parameterIndexs[hashCode] + 1;

                    if (parameterIndexs[hashCode] >= config.MaxDepth)
                    {
                        return null;
                    }
                }
                else
                {
                    parameterIndexs.Add(hashCode, 1);
                }
            }

            #endregion
            
            var collectionAdapterModel = _collectionAdapterModel;

            var destinationType = typeof(TDestination);

            if (destinationType.IsArray)
            {
                #region CopyToArray

                byte i = 0;
                var adapterInvoker = collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? new TDestinationElementType[((ICollection)source).Count] : (TDestinationElementType[])destination;
                if (collectionAdapterModel.IsPrimitive)
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

                var adapterInvoker = collectionAdapterModel.AdaptInvoker;
                var list = destination == null ? new List<TDestinationElementType>() : (List<TDestinationElementType>)destination;
                if (collectionAdapterModel.IsPrimitive)
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

                var adapterInvoker = collectionAdapterModel.AdaptInvoker;
                var array = destination == null ? new ArrayList() : (ArrayList)destination;
                if (collectionAdapterModel.IsPrimitive)
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


        private static CollectionAdapterModel CreateCollectionAdapterModel()
        {
            var cam = new CollectionAdapterModel();

            var config = TypeAdapterConfig<TSource, TDestination>.Configuration;

            var sourceElementType = ReflectionUtils.ExtractElementType(typeof(TSource));
            var destinationElementType = typeof(TDestinationElementType);

            if (ReflectionUtils.IsPrimitive(destinationElementType) || destinationElementType == typeof(object))
            {
                cam.IsPrimitive = true;

                var converter = ReflectionUtils.CreatePrimitiveConverter(sourceElementType, destinationElementType);
                if (converter != null)
                    cam.AdaptInvoker = converter;
            }
            else if (ReflectionUtils.IsCollection(destinationElementType))
            {
                var methodInfo = typeof(CollectionAdapter<,,>)
                    .MakeGenericType(sourceElementType, ReflectionUtils.ExtractElementType(destinationElementType), destinationElementType)
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
