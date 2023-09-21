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

		/// <summary>
		/// Configures the builder for specific types.	
		/// </summary>
		/// <param name="types">Types to configure.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder ForTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                if (!this.TypeSettings.ContainsKey(type))
                    this.TypeSettings.Add(type, new Dictionary<string, PropertySetting>());
            }

            return this;
        }


		/// <summary>
		/// Configures the builder for all types in a given namespace within an assembly.
		/// </summary>
		/// <param name="assembly">The assembly containing the types.</param>
		/// <param name="namespace">The namespace of the types to include.</param>
		/// <returns></returns>
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


		/// <summary>
		/// Configures the builder for a specific type and allows for property-specific configuration.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyConfig">An optional action for configuring properties of the specified type.</param>
		/// <returns></returns>
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


		/// <summary>
		/// Excludes specific types from the configuration.
		/// </summary>
		/// <param name="types">An array of types to exclude.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder ExcludeTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                this.TypeSettings.Remove(type);
            }

            return this;
        }


		/// <summary>
		/// Exclude certain types from the adaptation process based on a provided predicate.
		/// </summary>
		/// <param name="predicate">Predicate function should evaluate to true for types that you want to exclude from the mapping and false for types that should not be excluded.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder ExcludeTypes(Func<Type, bool> predicate)
        {
            foreach (var type in this.TypeSettings.Keys.ToList())
            {
                if (predicate(type))
                    this.TypeSettings.Remove(type);
            }

            return this;
        }


		/// <summary>
		/// Specifies attributes to ignore during mapping.
		/// </summary>
		/// <param name="attributes">An array of attributes to ignore.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder IgnoreAttributes(params Type[] attributes)
        {
            this.Attribute.IgnoreAttributes = attributes;
            return this;
        }


		/// <summary>
		/// Specifies attributes that should not be ignored during mapping.
		/// </summary>
		/// <param name="attributes">An array of attributes that should not be ignored.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder IgnoreNoAttributes(params Type[] attributes)
        {
            this.Attribute.IgnoreNoAttributes = attributes;
            return this;
        }


		/// <summary>
		/// Specifies namespaces to ignore during mapping.
		/// </summary>
		/// <param name="namespaces">An array of namespaces to ignore.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder IgnoreNamespaces(params string[] namespaces)
        {
            this.Attribute.IgnoreNamespaces = namespaces;
            return this;
        }


		/// <summary>
		/// Configures whether null values should be ignored during mapping.
		/// </summary>
		/// <param name="value">A boolean value indicating whether to ignore null values.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder IgnoreNullValues(bool value)
        {
            this.Attribute.IgnoreNullValues = value;
            return this;
        }


		/// <summary>
		/// Configures whether a destination member source is required during.
		/// </summary>
		/// <param name="value">A boolean value indicating whether a destination member source is required.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder RequireDestinationMemberSource(bool value)
        {
            this.Attribute.RequireDestinationMemberSource = value;
            return this;
        }


		/// <summary>
		/// Configures whether mapping should be performed to constructors.
		/// </summary>
		/// <param name="value">A boolean value indicating whether mapping to constructors is enabled.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder MapToConstructor(bool value)
        {
            this.Attribute.MapToConstructor = value;
            return this;
        }


		/// <summary>
		/// Sets the maximum depth for mapping.
		/// </summary>
		/// <param name="depth">The maximum depth for mapping.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder MaxDepth(int depth)
        {
            this.Attribute.MaxDepth = depth;
            return this;
        }


		/// <summary>
		/// Configures whether to preserve object references during mapping.
		/// </summary>
		/// <param name="value">A boolean value indicating whether to preserve object references.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder PreserveReference(bool value)
        {
            this.Attribute.PreserveReference = value;
            return this;
        }


		/// <summary>
		/// Configures whether to perform a shallow copy for the same source and destination type.
		/// </summary>
		/// <param name="value">A boolean value indicating whether to perform a shallow copy.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder ShallowCopyForSameType(bool value)
        {
            this.Attribute.ShallowCopyForSameType = value;
            return this;
        }


		/// <summary>
		/// Forward property types.
		/// </summary>
		/// <typeparam name="TFrom">Forward property from type.</typeparam>
		/// <typeparam name="TTo">Forward property to type.</typeparam>
		/// <returns></returns>
		public AdaptAttributeBuilder AlterType<TFrom, TTo>()
        {
            this.AlterTypes.Add(type => type == typeof(TFrom) ? typeof(TTo) : null);
            return this;
        }


		/// <summary>
		/// Forward property types for Code generation.
		/// </summary>
		/// <param name="predicate">A function that takes a Type as input and returns a Boolean value. This function is used to evaluate whether the forward property should be applied to the target type. If the predicate returns true, the target type will be replaced; otherwise, it remains unchanged.</param>
		/// <param name="toType">Type of destination to forward property type.</param>
		/// <returns></returns>
		public AdaptAttributeBuilder AlterType(Func<Type, bool> predicate, Type toType)
        {
            this.AlterTypes.Add(type => predicate(type) ? toType : null);
            return this;
        }
    }
}