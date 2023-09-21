using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster
{
    public class PropertySettingBuilder<T>
    {
        public Dictionary<string, PropertySetting> Settings { get; }
        public PropertySettingBuilder(Dictionary<string, PropertySetting> settings)
        {
            this.Settings = settings;
        }

        private PropertySetting ForProperty(string name)
        {
            if (!this.Settings.TryGetValue(name, out var setting))
            {
                setting = new PropertySetting();
                this.Settings.Add(name, setting);
            }
            return setting;
        }


		/// <summary>
		/// Ignore a specific property during mapping.
		/// </summary>
		/// <typeparam name="TReturn"></typeparam>
		/// <param name="member">A lambda expression that identifies the property to be ignored during mapping.</param>
		/// <returns></returns>
		public PropertySettingBuilder<T> Ignore<TReturn>(Expression<Func<T, TReturn>> member)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.Ignore = true;
            return this;
        }


		/// <summary>
		/// Map a specific property of the source type to a target property with a different name during mapping.
		/// </summary>
		/// <typeparam name="TReturn"></typeparam>
		/// <param name="member">A lambda expression that identifies the source property to be mapped.</param>
		/// <param name="targetPropertyName">The name of the target property to which the source property should be mapped during the mapping process.</param>
		/// <returns></returns>
		public PropertySettingBuilder<T> Map<TReturn>(Expression<Func<T, TReturn>> member, string targetPropertyName)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }


		/// <summary>
		/// Map a specific property of the source type to a target property with a different type and name during mapping.
		/// </summary>
		/// <typeparam name="TReturn"></typeparam>
		/// <param name="member">A lambda expression that identifies the source property to be mapped.</param>
		/// <param name="targetPropertyType">The type of the target property to which the source property should be mapped during the mapping process.</param>
		/// <param name="targetPropertyName">The name of the target property to which the source property should be mapped.</param>
		/// <returns></returns>
		public PropertySettingBuilder<T> Map<TReturn>(Expression<Func<T, TReturn>> member, Type targetPropertyType, string? targetPropertyName = null)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.TargetPropertyType = targetPropertyType;
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }


		/// <summary>
		/// Map a specific property of the source type to a target property using a custom mapping function.
		/// </summary>
		/// <typeparam name="TReturn">Type of source property.</typeparam>
		/// <typeparam name="TReturn2">Type of target property type.</typeparam>
		/// <param name="member">A lambda expression that identifies the source property to be mapped.</param>
		/// <param name="mapFunc">A lambda expression that defines the custom mapping function.</param>
		/// <param name="targetPropertyName">The name of the target property to which the source property should be mapped.</param>
		/// <returns></returns>
		public PropertySettingBuilder<T> Map<TReturn, TReturn2>(Expression<Func<T, TReturn>> member, Expression<Func<T, TReturn2>> mapFunc, string? targetPropertyName = null)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.MapFunc = mapFunc;
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }
   
    }
}