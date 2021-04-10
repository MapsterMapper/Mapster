using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mapster
{
    public class AdaptAttributeBuilder
    {
        public BaseAdaptAttribute Attribute { get; }
        public Dictionary<Type, Dictionary<string, PropertySetting>> TypeSettings { get; } = new Dictionary<Type, Dictionary<string, PropertySetting>>();
        public List<Func<Type, Type?>> AlterTypes { get; } = new List<Func<Type, Type?>>();

        public AdaptAttributeBuilder(BaseAdaptAttribute attribute)
        {
            this.Attribute = attribute;
        }

        public AdaptAttributeBuilder ForTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                if (!this.TypeSettings.ContainsKey(type))
                    this.TypeSettings.Add(type, new Dictionary<string, PropertySetting>());
            }

            return this;
        }

        public AdaptAttributeBuilder ForAllTypesInNamespace(Assembly assembly, string @namespace)
        {
            foreach (var type in assembly.GetTypes())
            {
                if ((type.Namespace == @namespace || type.Namespace?.StartsWith(@namespace + '.') == true) 
                    && !type.Name.Contains('<')
                    && !this.TypeSettings.ContainsKey(type))
                    this.TypeSettings.Add(type, new Dictionary<string, PropertySetting>());
            }

            return this;
        }

        public AdaptAttributeBuilder ForType<T>(Action<PropertySettingBuilder<T>>? propertyConfig = null)
        {
            if (!this.TypeSettings.TryGetValue(typeof(T), out var settings))
            {
                settings = new Dictionary<string, PropertySetting>();
                this.TypeSettings.Add(typeof(T), settings);
            }

            propertyConfig?.Invoke(new PropertySettingBuilder<T>(settings));
            return this;
        }

        public AdaptAttributeBuilder ExcludeTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                this.TypeSettings.Remove(type);
            }

            return this;
        }

        public AdaptAttributeBuilder ExcludeTypes(Func<Type, bool> predicate)
        {
            foreach (var type in this.TypeSettings.Keys.ToList())
            {
                if (predicate(type))
                    this.TypeSettings.Remove(type);
            }

            return this;
        }

        public AdaptAttributeBuilder IgnoreAttributes(params Type[] attributes)
        {
            this.Attribute.IgnoreAttributes = attributes;
            return this;
        }

        public AdaptAttributeBuilder IgnoreNoAttributes(params Type[] attributes)
        {
            this.Attribute.IgnoreNoAttributes = attributes;
            return this;
        }

        public AdaptAttributeBuilder IgnoreNamespaces(params string[] namespaces)
        {
            this.Attribute.IgnoreNamespaces = namespaces;
            return this;
        }

        public AdaptAttributeBuilder IgnoreNullValues(bool value)
        {
            this.Attribute.IgnoreNullValues = value;
            return this;
        }

        public AdaptAttributeBuilder RequireDestinationMemberSource(bool value)
        {
            this.Attribute.RequireDestinationMemberSource = value;
            return this;
        }

        public AdaptAttributeBuilder MapToConstructor(bool value)
        {
            this.Attribute.MapToConstructor = value;
            return this;
        }

        public AdaptAttributeBuilder MaxDepth(int depth)
        {
            this.Attribute.MaxDepth = depth;
            return this;
        }
        
        public AdaptAttributeBuilder PreserveReference(bool value)
        {
            this.Attribute.PreserveReference = value;
            return this;
        }
        
        public AdaptAttributeBuilder ShallowCopyForSameType(bool value)
        {
            this.Attribute.ShallowCopyForSameType = value;
            return this;
        }

        public AdaptAttributeBuilder AlterType<TFrom, TTo>()
        {
            this.AlterTypes.Add(type => type == typeof(TFrom) ? typeof(TTo) : null);
            return this;
        }

        public AdaptAttributeBuilder AlterType(Func<Type, bool> predicate, Type toType)
        {
            this.AlterTypes.Add(type => predicate(type) ? toType : null);
            return this;
        }
    }
}