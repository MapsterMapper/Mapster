using System;
using System.Collections.Generic;
using System.Reflection;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public sealed class ClassAdapter<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {

        private static readonly FastObjectFactory.CreateObject _destinationFactory = FastObjectFactory.CreateObjectFactory<TDestination>();
        private static AdapterModel<TSource, TDestination> _adapterModel = CreateAdapterModel();

        private static readonly string _nonInitializedAdapterMessage =
            String.Format("This class adapter was not initialized properly. This typically happens if one of the classes does not have a default (empty) constructor.  SourceType: {0}, DestinationType{1}",
                typeof (TSource), typeof (TDestination));

        private static readonly string _propertyMappingErrorMessage =
            String.Format("Error occurred mapping the following property.\nSource Type: {0}  Destination Type: {1}  Destination Property: ",
            typeof (TSource), typeof (TDestination));


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

            return Adapt(source, (TDestination)_destinationFactory(), true, parameterIndexs);
        }

        public static TDestination Adapt(TSource source, TDestination destination, bool isNew,
            Dictionary<int, int> parameterIndexs)
        {
            if (source == null)
                return null;

            var config = TypeAdapterConfig<TSource, TDestination>.Configuration;

            var hasConfig = config != null;

            var hasMaxDepth = hasConfig && config.MaxDepth > 0;

            if (hasMaxDepth)
            {
                if (CheckMaxDepth(ref parameterIndexs, config)) return null;
            }

            if (destination == null)
                destination = (TDestination) _destinationFactory();

            bool ignoreNullValues = isNew || (hasConfig && config.IgnoreNullValues.HasValue && config.IgnoreNullValues.Value);


            PropertyModel<TSource, TDestination> property = null;
            try
            {
                var properties = _adapterModel.Properties;

                for (int index = 0; index < properties.Length; index++)
                {
                    property = properties[index];
                    switch (property.ConvertType)
                    {
                        case 1: //Primitive
                            object primitiveValue = property.Getter.Invoke(source);
                            if (primitiveValue == null)
                            {
                                if (ignoreNullValues)
                                    continue;

                                primitiveValue = property.DefaultDestinationValue;
                            }

                            if (property.AdaptInvoker == null)
                                property.Setter.Invoke(destination, primitiveValue);
                            else
                                property.Setter.Invoke(destination,
                                    property.AdaptInvoker(null,
                                        new[]
                                        {
                                            primitiveValue,
                                            (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexs) : parameterIndexs)
                                        }));
                            break;
                        case 2: //Flattening Get Method
                            property.Setter.Invoke(destination, property.AdaptInvoker(source, null));
                            break;
                        case 3: //Flattening Deep Property
                            var flatInvokers = property.FlatteningInvokers;
                            object value = source;
                            foreach (GenericGetter getter in flatInvokers)
                            {
                                value = getter(value);
                                if (value == null)
                                    break;
                            }

                            if (value == null)
                            {
                                if (ignoreNullValues)
                                    continue;

                                value = property.DefaultDestinationValue;
                            }

                            property.Setter.Invoke(destination, value);
                            break;
                        case 4: // Adapter
                            object sourceValue = property.Getter.Invoke(source);
                            if (sourceValue == null)
                            {
                                if (ignoreNullValues)
                                    continue;

                                sourceValue = property.DefaultDestinationValue;
                            }

                            property.Setter.Invoke(destination, property.AdaptInvoker(null,
                                    new[]
                                    {
                                        sourceValue,
                                        (hasMaxDepth ? ReflectionUtils.Clone(parameterIndexs) : parameterIndexs)
                                    }));
                            break;
                        case 5: // Custom Resolve
                            if (property.Condition == null || property.Condition(source))
                            {
                                property.Setter.Invoke(destination, property.CustomResolver(source));
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if(_adapterModel == null)
                    throw new InvalidOperationException(_nonInitializedAdapterMessage);

                if (property != null)
                {
                    //Todo: This slows things down with the try-catch but the information is critical in debugging
                    throw new InvalidOperationException(_propertyMappingErrorMessage + property.SetterPropertyName + "\nException: " + ex);
                }
            }

            return destination;

        }

        private static bool CheckMaxDepth(ref Dictionary<int, int> parameterIndexs, TypeAdapterConfigSettings<TSource> config)
        {
            if (parameterIndexs == null)
                parameterIndexs = new Dictionary<int, int>();

            int hashCode = typeof (TSource).GetHashCode() + typeof (TDestination).GetHashCode();

            if (parameterIndexs.ContainsKey(hashCode))
            {
                int index = parameterIndexs[hashCode] + 1;

                parameterIndexs[hashCode] = index;

                if (index >= config.MaxDepth)
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

        public static void Reset()
        {
            _adapterModel = CreateAdapterModel();
        }

        private static AdapterModel<TSource, TDestination> CreateAdapterModel()
        {
            FastObjectFactory.CreateObject fieldModelFactory = FastObjectFactory.CreateObjectFactory<FieldModel>();
            FastObjectFactory.CreateObject propertyModelFactory = FastObjectFactory.CreateObjectFactory<PropertyModel<TSource, TDestination>>();
            FastObjectFactory.CreateObject adapterModelFactory = FastObjectFactory.CreateObjectFactory<AdapterModel<TSource, TDestination>>();

            Type destinationType = typeof(TDestination);
            Type sourceType = typeof(TSource);

            var fields = new List<FieldModel>();
            var properties = new List<PropertyModel<TSource, TDestination>>();

            MemberInfo[] destinationMembers = ReflectionUtils.GetPublicFieldsAndProperties(destinationType);
            int length = destinationMembers.Length;

            var config = TypeAdapterConfig<TSource, TDestination>.Configuration;
            bool hasConfig = config != null;

            for (int i = 0; i < length; i++)
            {
                MemberInfo destinationMember = destinationMembers[i];
                bool isProperty = destinationMember is PropertyInfo;
               
                if (hasConfig)
                {
                    if (ProcessIgnores(config, destinationMember)) continue;

                    if (ProcessCustomResolvers(config, destinationMember, propertyModelFactory, properties)) continue;
                }

                MemberInfo sourceMember = ReflectionUtils.GetPublicFieldOrProperty(sourceType, isProperty, destinationMember.Name);
                if (sourceMember == null)
                {
                    if (FlattenMethod(sourceType, destinationMember, propertyModelFactory, properties)) continue;

                    FlattenClass(sourceType, destinationMember, propertyModelFactory, properties);

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

                    var propertyModel = (PropertyModel<TSource, TDestination>)propertyModelFactory();
                    propertyModel.Getter = getter;
                    propertyModel.Setter = setter;

                    if (!ReflectionUtils.IsNullable(destinationPropertyType) && destinationPropertyType != typeof(string) && ReflectionUtils.IsPrimitive(destinationPropertyType))
                        propertyModel.DefaultDestinationValue = Activator.CreateInstance(destinationPropertyType);

                    if (ReflectionUtils.IsPrimitive(destinationPropertyType))
                    {
                        propertyModel.ConvertType = 1;
                       
                        var converter = ReflectionUtils.CreatePrimitiveConverter(sourceProperty.PropertyType, destinationPropertyType);
                        if (converter != null)
                            propertyModel.AdaptInvoker = converter;
                    }
                    else
                    {
                        propertyModel.ConvertType = 4;

                        if (ReflectionUtils.IsCollection(destinationPropertyType)) //collections
                        {
                            propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(typeof(CollectionAdapter<,,>).MakeGenericType(sourceProperty.PropertyType, ReflectionUtils.ExtractElementType(destinationPropertyType), destinationPropertyType).GetMethod("Adapt", new[] { sourceProperty.PropertyType, typeof(Dictionary<,>).MakeGenericType(typeof(int), typeof(int)) }));
                        }
                        else // class
                        {
                            if (destinationPropertyType == sourceProperty.PropertyType)
                            {
                                bool newInstance;

                                if (hasConfig && config.NewInstanceForSameType.HasValue)
                                    newInstance = config.NewInstanceForSameType.Value;
                                else
                                    newInstance = TypeAdapterConfig.Configuration.NewInstanceForSameType;

                                if (!newInstance)
                                    propertyModel.ConvertType = 1;
                                else
                                    propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(typeof(ClassAdapter<,>)
                                        .MakeGenericType(sourceProperty.PropertyType, destinationPropertyType)
                                        .GetMethod("Adapt", new [] { sourceProperty.PropertyType, typeof(Dictionary<,>).MakeGenericType(typeof(int), typeof(int)) }));                                    
                            }
                            else
                            {
                                propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(typeof(ClassAdapter<,>)
                                    .MakeGenericType(sourceProperty.PropertyType, destinationPropertyType)
                                    .GetMethod("Adapt", new [] { sourceProperty.PropertyType, typeof(Dictionary<,>).MakeGenericType(typeof(int), typeof(int)) }));
                            }
                        }
                    }

                    properties.Add(propertyModel);
                }
                else // Fields
                {
                    var fieldModel = (FieldModel)fieldModelFactory();
                    var fieldInfoType = typeof(FieldInfo);

                    fieldModel.Getter = FastInvoker.GetMethodInvoker(fieldInfoType.GetMethod("GetValue"));
                    fieldModel.Setter = FastInvoker.GetMethodInvoker(fieldInfoType.GetMethod("SetValue"));

                    fields.Add(fieldModel);
                }
            }

            var adapterModel = (AdapterModel<TSource, TDestination>)adapterModelFactory();
            adapterModel.Fields = fields.ToArray();
            adapterModel.Properties = properties.ToArray();

            return adapterModel;
        }

        private static bool FlattenClass(Type sourceType, MemberInfo destinationMember, FastObjectFactory.CreateObject propertyModelFactory,
            List<PropertyModel<TSource, TDestination>> properties)
        {
            var delegates = new List<GenericGetter>();
            GetDeepFlattening(sourceType, destinationMember.Name, delegates);
            if (delegates.Count > 0)
            {
                var setter = PropertyCaller<TDestination>.CreateSetMethod((PropertyInfo) destinationMember);
                if (setter == null)
                    return true;

                var propertyModel = (PropertyModel<TSource, TDestination>) propertyModelFactory();
                propertyModel.ConvertType = 3;
                propertyModel.Setter = setter;
                propertyModel.FlatteningInvokers = delegates.ToArray();

                properties.Add(propertyModel);
            }
            return true;
        }

        private static bool FlattenMethod(Type sourceType, MemberInfo destinationMember, FastObjectFactory.CreateObject propertyModelFactory,
            List<PropertyModel<TSource, TDestination>> properties)
        {
            var getMethod = sourceType.GetMethod(string.Concat("Get", destinationMember.Name));
            if (getMethod != null)
            {
                var setter = PropertyCaller<TDestination>.CreateSetMethod((PropertyInfo) destinationMember);
                if (setter == null)
                    return true;

                var propertyModel = (PropertyModel<TSource, TDestination>) propertyModelFactory();
                propertyModel.ConvertType = 2;
                propertyModel.Setter = setter;
                propertyModel.AdaptInvoker = FastInvoker.GetMethodInvoker(getMethod);

                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool ProcessCustomResolvers(TypeAdapterConfigSettings<TSource> config, MemberInfo destinationMember,
            FastObjectFactory.CreateObject propertyModelFactory, List<PropertyModel<TSource, TDestination>> properties)
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
                        var destinationProperty = (PropertyInfo) destinationMember;

                        var setter = PropertyCaller<TDestination>.CreateSetMethod(destinationProperty);
                        if (setter == null)
                            continue;

                        var propertyModel = (PropertyModel<TSource, TDestination>) propertyModelFactory();
                        propertyModel.ConvertType = 5;
                        propertyModel.Setter = setter;
                        propertyModel.CustomResolver = resolver.Invoker;
                        propertyModel.Condition = resolver.Condition;

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

        private static bool ProcessIgnores(TypeAdapterConfigSettings<TSource> config, MemberInfo destinationMember)
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

        private static void GetDeepFlattening(Type type, string propertyName, List<GenericGetter> invokers)
        {
            var properties = type.GetProperties();
            for (int j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                if (property.PropertyType.IsClass && property.PropertyType != typeof (string) 
                    && propertyName.StartsWith(property.Name))
                {
                    invokers.Add(PropertyCaller.CreateGetMethod(property));
                    GetDeepFlattening(property.PropertyType, propertyName.Substring(property.Name.Length), invokers);
                }
                else if (string.Equals(propertyName, property.Name))
                {
                    invokers.Add(PropertyCaller.CreateGetMethod(property));
                }
            }
        }

    }
}
