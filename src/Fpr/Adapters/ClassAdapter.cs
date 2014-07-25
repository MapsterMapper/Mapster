using System;
using System.Collections.Generic;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public sealed class ClassAdapter<TSource, TDestination>
    {
        private static Func<TDestination> _destinationFactory;
        private static AdapterModel<TSource, TDestination> _adapterModel;

        private static readonly string _nonInitializedAdapterMessage =
            String.Format("This class adapter was not initialized properly. This typically happens if one of the classes does not have a default (empty) constructor.  SourceType: {0}, DestinationType{1}",
                typeof (TSource), typeof (TDestination));

        private static readonly string _propertyMappingErrorMessage =
            String.Format("Error occurred mapping the following property.\nSource Type: {0}  Destination Type: {1}  Destination Property: ",
            typeof (TSource), typeof (TDestination));

        private static Func<TDestination> DestinationFactory
        {
            get
            {
                return _destinationFactory ?? (_destinationFactory = FastObjectFactory.CreateObjectFactory(
                    TypeAdapterConfig<TSource, TDestination>.ConfigSettings != null
                        ? TypeAdapterConfig<TSource, TDestination>.ConfigSettings.ConstructUsing
                        : null));
            }
        }


        public static TDestination Adapt(TSource source)
        {
            return Adapt(source, new Dictionary<int, int>());
        }

        public static TDestination Adapt(TSource source, TDestination destination)
        {
            return Adapt(source, destination, false, new Dictionary<int, int>());
        }

        public static TDestination Adapt(TSource source, Dictionary<int, int> parameterIndexs)
        {
            if (parameterIndexs == null)
                parameterIndexs = new Dictionary<int, int>();

            return Adapt(source, DestinationFactory(), true, parameterIndexs);
        }

        public static TDestination Adapt(TSource source, TDestination destination, bool isNew,
            Dictionary<int, int> parameterIndexes)
        {
            if (source == null)
                return default(TDestination);

            var configSettings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

            var hasConfig = configSettings != null;

            var hasMaxDepth = hasConfig && configSettings.MaxDepth > 0;

            if (hasMaxDepth && CheckMaxDepth(ref parameterIndexes, configSettings.MaxDepth))
            {
                return default(TDestination);
            }

            if (destination == null)
                destination = DestinationFactory();

            bool ignoreNullValues = isNew || (hasConfig && configSettings.IgnoreNullValues.HasValue && configSettings.IgnoreNullValues.Value);

            PropertyModel<TSource, TDestination> propertyModel = null;
            try
            {
                var propertyModels = GetAdapterModel().Properties;

                for (int index = 0; index < propertyModels.Length; index++)
                {
                    propertyModel = propertyModels[index];

                    object destinationValue = null;

                    switch (propertyModel.ConvertType)
                    {
                        case 1: //Primitive
                            object primitiveValue = propertyModel.Getter.Invoke(source);
                            if (primitiveValue == null)
                            {
                                continue;
                            }

                            if (propertyModel.AdaptInvoker == null)
                                destinationValue = primitiveValue;
                            else
                                destinationValue = propertyModel.AdaptInvoker(null,
                                    new[]
                                    {
                                        primitiveValue,
                                        (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexes) : parameterIndexes)
                                    });
                            break;
                        case 2: //Flattening Get Method
                            destinationValue = propertyModel.AdaptInvoker(source, null);
                            break;
                        case 3: //Flattening Deep Property
                            var flatInvokers = propertyModel.FlatteningInvokers;
                            object value = source;
                            foreach (GenericGetter getter in flatInvokers)
                            {
                                value = getter(value);
                                if (value == null)
                                    break;
                            }

                            if (value == null && ignoreNullValues)
                            {
                                continue;
                            }
                            destinationValue = value;
                            break;
                        case 4: // Adapter
                            object sourceValue = propertyModel.Getter.Invoke(source);
                            if (sourceValue == null && ignoreNullValues)
                            {
                                continue;
                            }

                            destinationValue = propertyModel.AdaptInvoker(null,
                                new[]
                                {
                                    sourceValue,
                                    (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexes) : parameterIndexes)
                                });
                            break;
                        case 5: // Custom Resolve
                            if (propertyModel.Condition == null || propertyModel.Condition(source))
                            {
                                destinationValue = propertyModel.CustomResolver(source);
                                break;
                            }
                            continue;
                            
                    }

                    if (propertyModel.DestinationTransform != null)
                    {
                        propertyModel.Setter.Invoke(destination, propertyModel.DestinationTransform(destinationValue));
                    }
                    else
                    {
                        propertyModel.Setter.Invoke(destination, destinationValue);
                    }
      
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentOutOfRangeException)
                    throw;

                if(_adapterModel == null)
                    throw new InvalidOperationException(_nonInitializedAdapterMessage);
                
                if (propertyModel != null)
                {
                    //Todo: This slows things down with the try-catch but the information is critical in debugging
                    throw new InvalidOperationException(_propertyMappingErrorMessage + propertyModel.SetterPropertyName + "\nException: " + ex);
                }
                throw;
            }

            return destination;
        }

        private static bool CheckMaxDepth(ref Dictionary<int, int> parameterIndexes, int maxDepth)
        {
            if (parameterIndexes == null)
                parameterIndexes = new Dictionary<int, int>();

            int hashCode = typeof (TSource).GetHashCode() + typeof (TDestination).GetHashCode();

            if (parameterIndexes.ContainsKey(hashCode))
            {
                int index = parameterIndexes[hashCode] + 1;

                parameterIndexes[hashCode] = index;

                if (index >= maxDepth)
                {
                    return true;
                }
            }
            else
            {
                parameterIndexes.Add(hashCode, 1);
            }
            return false;
        }

        public static void Reset()
        {
            _adapterModel = null;
        }

        private static AdapterModel<TSource, TDestination> GetAdapterModel()
        {
            return _adapterModel ?? (_adapterModel = ClassAdapterBuilder<TSource, TDestination>.CreateAdapterModel());
        }

       
    }
}
