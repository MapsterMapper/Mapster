using System;
using System.Collections.Generic;
using System.Reflection;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr.Adapters
{
    /// <summary>
    /// Maps one class to another.
    /// </summary>
    /// <remarks>The operations in this class must be extremely fast.  Make sure to benchmark before making **any** changes in here.  
    /// The core Adapt method is critically important to performance.
    /// </remarks>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    public static class ClassAdapter<TSource, TDestination>
    {
        private static Func<TDestination> _destinationFactory;
        private static AdapterModel<TSource, TDestination> _adapterModel;

        private static readonly string _nonInitializedAdapterMessage =
            String.Format("This class adapter was not initialized properly. This typically happens if one of the classes does not have a default (empty) constructor.  SourceType: {0}, DestinationType{1}",
                typeof (TSource), typeof (TDestination));

        private static readonly string _propertyMappingErrorMessage =
            String.Format("Error occurred mapping the following property.\nSource Type: {0}  Destination Type: {1}  Destination Property: ",
            typeof (TSource), typeof (TDestination));

        private static readonly string _noExplicitMappingMessage =
           String.Format("Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {0} TDestination: {1}",
               typeof(TSource), typeof(TDestination));

        private static readonly string _unmappedMembersMessage =
            String.Format("The following members of destination class {0} do not have a corresponding source member mapped or ignored:", typeof(TDestination));

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

        /// <summary>
        /// Creates a destination object and maps the source to that object.
        /// </summary>
        /// <param name="source">The Source object to map.</param>
        /// <returns>The resulting Destination object.</returns>
        public static TDestination Adapt(TSource source)
        {
            return Adapt(source, DestinationFactory(), true, new Dictionary<int, int>());
        }

        /// <summary>
        /// Maps the source to an existing destination object.
        /// </summary>
        /// <param name="source">The Source object to map.</param>
        /// <param name="destination">The Destination to map to.</param>
        /// <returns>The resulting Destination object.</returns>
        public static TDestination Adapt(TSource source, TDestination destination)
        {
            return Adapt(source, destination, false, new Dictionary<int, int>());
        }

        /// <summary>
        /// Maps the source to the destination based on parameter indexes - For internal use
        /// </summary>
        /// <param name="source">The Source object to map.</param>
        /// <param name="parameterIndexes">The parameter indexes.</param>
        /// <returns>The destination object.</returns>
        public static TDestination Adapt(TSource source, Dictionary<int, int> parameterIndexes)
        {
            return Adapt(source, DestinationFactory(), true, parameterIndexes);
        }

        private static TDestination Adapt(TSource source, TDestination destination, bool isNew,
            Dictionary<int, int> parameterIndexes)
        {
            if (source == null)
            {
                return default(TDestination);
            }

            var configSettings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

            bool hasConfig = false;
            bool hasMaxDepth = false;

            if (configSettings != null)
            {
                hasConfig = true;
                if (configSettings.ConverterFactory != null)
                {
                    var converter = configSettings.ConverterFactory();
                    return converter.Resolve(source);
                }
                if (configSettings.MaxDepth > 0)
                {
                    hasMaxDepth = true;
                    if(MaxDepthExceeded(ref parameterIndexes, configSettings.MaxDepth))
                        return default(TDestination);
                }
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

        private static bool MaxDepthExceeded(ref Dictionary<int, int> parameterIndexes, int maxDepth)
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
            _destinationFactory = null;
        }


        #region Build the Adapter Model

        private static AdapterModel<TSource, TDestination> GetAdapterModel()
        {
            return _adapterModel ?? (_adapterModel = CreateAdapterModel());
        }

        private static AdapterModel<TSource, TDestination> CreateAdapterModel()
        {
            Func<FieldModel> fieldModelFactory = FastObjectFactory.CreateObjectFactory<FieldModel>();
            Func<PropertyModel<TSource, TDestination>> propertyModelFactory = FastObjectFactory.CreateObjectFactory<PropertyModel<TSource, TDestination>>();
            Func<AdapterModel<TSource, TDestination>> adapterModelFactory = FastObjectFactory.CreateObjectFactory<AdapterModel<TSource, TDestination>>();

            Type destinationType = typeof(TDestination);
            Type sourceType = typeof(TSource);

            var unmappedDestinationMembers = new List<string>();

            var fields = new List<FieldModel>();
            var properties = new List<PropertyModel<TSource, TDestination>>();

            List<MemberInfo> destinationMembers = destinationType.GetPublicFieldsAndProperties(allowNoSetter: false);

            var configSettings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

            bool hasConfig = configSettings != null;

            if (!hasConfig && TypeAdapterConfig.GlobalSettings.RequireExplicitMapping && sourceType != destinationType)
            {
                throw new ArgumentOutOfRangeException(_noExplicitMappingMessage);
            }

            IDictionary<Type, Func<object, object>> destinationTransforms = hasConfig
                ? configSettings.DestinationTransforms.Transforms : TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;

            for (int i = 0; i < destinationMembers.Count; i++)
            {
                MemberInfo destinationMember = destinationMembers[i];
                bool isProperty = destinationMember is PropertyInfo;

                if (hasConfig)
                {
                    if (ProcessIgnores(configSettings, destinationMember)) continue;

                    if (ProcessCustomResolvers(configSettings, destinationMember, propertyModelFactory, properties, destinationTransforms)) continue;
                }

                MemberInfo sourceMember = ReflectionUtils.GetPublicFieldOrProperty(sourceType, isProperty, destinationMember.Name);
                if (sourceMember == null)
                {
                    if (FlattenMethod(sourceType, destinationMember, propertyModelFactory, properties, destinationTransforms)) continue;

                    if (FlattenClass(sourceType, destinationMember, propertyModelFactory, properties, destinationTransforms)) continue;

                    if (destinationMember.HasPublicSetter())
                    {
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }

                    continue;
                }

                if (isProperty)
                {
                    var destinationProperty = (PropertyInfo)destinationMember;

                    var setter = PropertyCaller<TDestination>.CreateSetMethod(destinationProperty);
                    if (setter == null)
                        continue;

                    var sourceProperty = (PropertyInfo)sourceMember;

                    var getter = PropertyCaller<TSource>.CreateGetMethod(sourceProperty);
                    if (getter == null)
                        continue;

                    Type destinationPropertyType = destinationProperty.PropertyType;

                    var propertyModel = propertyModelFactory();
                    propertyModel.Getter = getter;
                    propertyModel.Setter = setter;
                    propertyModel.SetterPropertyName = ExtractPropertyName(setter, "Set");
                    if (destinationTransforms.ContainsKey(destinationPropertyType))
                        propertyModel.DestinationTransform = destinationTransforms[destinationPropertyType];

                    //if (!ReflectionUtils.IsNullable(destinationPropertyType) && destinationPropertyType != typeof(string) && ReflectionUtils.IsPrimitive(destinationPropertyType))
                    //    propertyModel.DefaultDestinationValue = new TDestination();

                    if (destinationPropertyType.IsPrimitiveRoot())
                    {
                        propertyModel.ConvertType = 1;

                        var converter = sourceProperty.PropertyType.CreatePrimitiveConverter(destinationPropertyType);
                        if (converter != null)
                            propertyModel.AdaptInvoker = converter;
                    }
                    else
                    {
                        propertyModel.ConvertType = 4;

                        if (destinationPropertyType.IsCollection()) //collections
                        {
                            propertyModel.AdaptInvoker =
                                FastInvoker.GetMethodInvoker(
                                    typeof(CollectionAdapter<,,>).MakeGenericType(sourceProperty.PropertyType,
                                        destinationPropertyType.ExtractCollectionType(),
                                        destinationPropertyType)
                                        .GetMethod("Adapt",
                                            new[]
                                            {
                                                sourceProperty.PropertyType,
                                                typeof (Dictionary<,>).MakeGenericType(typeof (int), typeof (int))
                                            }));
                        }
                        else // class
                        {
                            if (destinationPropertyType == sourceProperty.PropertyType)
                            {
                                bool newInstance;

                                if (hasConfig && configSettings.NewInstanceForSameType.HasValue)
                                    newInstance = configSettings.NewInstanceForSameType.Value;
                                else
                                    newInstance = TypeAdapterConfig.ConfigSettings.NewInstanceForSameType;

                                if (!newInstance)
                                    propertyModel.ConvertType = 1;
                                else
                                    propertyModel.AdaptInvoker =
                                        FastInvoker.GetMethodInvoker(typeof(ClassAdapter<,>)
                                            .MakeGenericType(sourceProperty.PropertyType, destinationPropertyType)
                                            .GetMethod("Adapt",
                                                new[]
                                                {
                                                    sourceProperty.PropertyType,
                                                    typeof (Dictionary<,>).MakeGenericType(typeof (int),
                                                        typeof (int))
                                                }));
                            }
                            else
                            {
                                propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(typeof(ClassAdapter<,>)
                                    .MakeGenericType(sourceProperty.PropertyType, destinationPropertyType)
                                    .GetMethod("Adapt",
                                        new[]
                                        {
                                            sourceProperty.PropertyType,
                                            typeof (Dictionary<,>).MakeGenericType(typeof (int), typeof (int))
                                        }));
                            }
                        }
                    }

                    properties.Add(propertyModel);
                }
                else // Fields
                {
                    var fieldModel = fieldModelFactory();
                    var fieldInfoType = typeof(FieldInfo);

                    fieldModel.Getter = FastInvoker.GetMethodInvoker(fieldInfoType.GetMethod("GetValue"));
                    fieldModel.Setter = FastInvoker.GetMethodInvoker(fieldInfoType.GetMethod("SetValue"));

                    fields.Add(fieldModel);
                }
            }

            if (TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new ArgumentOutOfRangeException(_unmappedMembersMessage + string.Join(",", unmappedDestinationMembers));
            }

            var adapterModel = adapterModelFactory();
            adapterModel.Fields = fields.ToArray();
            adapterModel.Properties = properties.ToArray();

            return adapterModel;
        }

        private static bool FlattenClass(Type sourceType, MemberInfo destinationMember, Func<Object> propertyModelFactory,
            List<PropertyModel<TSource, TDestination>> properties, IDictionary<Type, Func<object, object>> destinationTransforms)
        {
            var delegates = new List<GenericGetter>();
            ReflectionUtils.GetDeepFlattening(sourceType, destinationMember.Name, delegates);
            if (delegates.Count > 0)
            {
                var setter = PropertyCaller<TDestination>.CreateSetMethod((PropertyInfo)destinationMember);
                if (setter != null)
                {
                    var propertyModel = (PropertyModel<TSource, TDestination>)propertyModelFactory();
                    propertyModel.ConvertType = 3;
                    propertyModel.Setter = setter;
                    propertyModel.SetterPropertyName = ExtractPropertyName(setter, "Set");
                    var destinationPropertyType = typeof(TDestination);
                    if (destinationTransforms.ContainsKey(destinationPropertyType))
                        propertyModel.DestinationTransform = destinationTransforms[destinationPropertyType];

                    propertyModel.FlatteningInvokers = delegates.ToArray();

                    properties.Add(propertyModel);

                    return true;
                }
            }
            return false;
        }

        private static bool FlattenMethod(Type sourceType, MemberInfo destinationMember, Func<Object> propertyModelFactory,
            List<PropertyModel<TSource, TDestination>> properties, IDictionary<Type, Func<object, object>> destinationTransforms)
        {
            var getMethod = sourceType.GetMethod(string.Concat("Get", destinationMember.Name));
            if (getMethod != null)
            {
                var setter = PropertyCaller<TDestination>.CreateSetMethod((PropertyInfo)destinationMember);
                if (setter == null)
                    return true;

                var propertyModel = (PropertyModel<TSource, TDestination>)propertyModelFactory();
                propertyModel.ConvertType = 2;
                propertyModel.Setter = setter;
                propertyModel.SetterPropertyName = ExtractPropertyName(setter, "Set");
                var destinationPropertyType = typeof(TDestination);
                if (destinationTransforms.ContainsKey(destinationPropertyType))
                    propertyModel.DestinationTransform = destinationTransforms[destinationPropertyType];
                propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(getMethod);

                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool ProcessCustomResolvers(TypeAdapterConfigSettings<TSource, TDestination> config, MemberInfo destinationMember,
            Func<Object> propertyModelFactory, List<PropertyModel<TSource, TDestination>> properties,
            IDictionary<Type, Func<object, object>> destinationTransforms)
        {
            var resolvers = config.Resolvers;
            if (resolvers != null && resolvers.Count > 0)
            {
                //Todo: Evaluate this to convert to foreach
                bool hasCustomResolve = false;
                for (int j = 0; j < resolvers.Count; j++)
                {
                    var resolver = resolvers[j];
                    if (destinationMember.Name.Equals(resolver.MemberName))
                    {
                        var destinationProperty = (PropertyInfo)destinationMember;

                        var setter = PropertyCaller<TDestination>.CreateSetMethod(destinationProperty);
                        if (setter == null)
                            continue;

                        var propertyModel = (PropertyModel<TSource, TDestination>)propertyModelFactory();
                        propertyModel.ConvertType = 5;
                        propertyModel.Setter = setter;
                        propertyModel.SetterPropertyName = ExtractPropertyName(setter, "Set");
                        propertyModel.CustomResolver = resolver.Invoker;
                        propertyModel.Condition = resolver.Condition;
                        var destinationPropertyType = typeof(TDestination);
                        if (destinationTransforms.ContainsKey(destinationPropertyType))
                            propertyModel.DestinationTransform = destinationTransforms[destinationPropertyType];

                        properties.Add(propertyModel);

                        hasCustomResolve = true;
                        break;
                    }
                }
                if (hasCustomResolve)
                    return true;
            }
            return false;
        }


        private static bool ProcessIgnores(TypeAdapterConfigSettings<TSource, TDestination> config, MemberInfo destinationMember)
        {
            var ignoreMembers = config.IgnoreMembers;
            if (ignoreMembers != null && ignoreMembers.Count > 0)
            {
                bool ignored = false;
                for (int j = 0; j < ignoreMembers.Count; j++)
                {
                    if (destinationMember.Name.Equals(ignoreMembers[j]))
                    {
                        ignored = true;
                        break;
                    }
                }
                if (ignored)
                    return true;
            }
            return false;
        }


        private static string ExtractPropertyName(Delegate caller, string prefix)
        {
            if (caller == null || caller.Method == null)
                return "";

            var name = caller.Method.Name.Trim('_');
            if (name.StartsWith(prefix))
            {
                name = name.Substring(prefix.Length);
            }

            return name;
        }

        #endregion

    }
}
