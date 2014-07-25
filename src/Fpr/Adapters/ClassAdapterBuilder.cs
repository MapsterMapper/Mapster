using System;
using System.Collections.Generic;
using System.Reflection;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public static class ClassAdapterBuilder<TSource, TDestination>
    {
        private static readonly string _noExplicitMappingMessage =
            String.Format("Implicit mapping is not allowed (check GlobalSettings.AllowImplicitMapping) and no configuration exists for the following mapping: TSource: {0} TDestination: {1}",
                typeof (TSource), typeof (TDestination));

        private static readonly string _unmappedMembersMessage =
            String.Format("The following members of destination class {0} do not have a corresponding source member mapped or ignored:", typeof(TDestination));

        public static AdapterModel<TSource, TDestination> CreateAdapterModel()
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
                                    newInstance = TypeAdapterConfig.Configuration.NewInstanceForSameType;

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
    }
}