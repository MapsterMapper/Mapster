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

        public PropertySettingBuilder<T> Ignore<TReturn>(Expression<Func<T, TReturn>> member)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.Ignore = true;
            return this;
        }

        public PropertySettingBuilder<T> Map<TReturn>(Expression<Func<T, TReturn>> member, string targetPropertyName)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }

        public PropertySettingBuilder<T> Map<TReturn>(Expression<Func<T, TReturn>> member, Type targetPropertyType, string? targetPropertyName = null)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.TargetPropertyType = targetPropertyType;
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }

        public PropertySettingBuilder<T> Map<TReturn, TReturn2>(Expression<Func<T, TReturn>> member, Expression<Func<T, TReturn2>> mapFunc, string? targetPropertyName = null)
        {
            var setting = ForProperty(member.GetMemberName());
            setting.MapFunc = mapFunc;
            setting.TargetPropertyName = targetPropertyName;
            return this;
        }
   
    }
}