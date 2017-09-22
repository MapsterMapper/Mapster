using System;
using System.Collections.Generic;
using Mapster.Utils;
using System.Collections;

namespace Mapster
{
    public class SettingStore: IApplyable<SettingStore>
    {
        private readonly Dictionary<string, object> _objectStore = new Dictionary<string, object>();
        private readonly Dictionary<string, bool?> _booleanStore = new Dictionary<string, bool?>();

        public void Set(string key, bool? value)
        {
            if (value == null)
                _booleanStore.Remove(key);
            else
                _booleanStore[key] = value;
        }

        public void Set(string key, object value)
        {
            if (value == null)
                _objectStore.Remove(key);
            else
                _objectStore[key] = value;
        }

        public bool? Get(string key)
        {
            return _booleanStore.GetValueOrDefault(key);
        }

        public T Get<T>(string key) where T : class
        {
            return (T)_objectStore.GetValueOrDefault(key);
        }

        public T Get<T>(string key, Func<T> initializer) where T : class
        {
            var value = _objectStore.GetValueOrDefault(key);
            if (value == null)
            {
                _objectStore[key] = value = initializer();
            }
            return (T)value;
        }

        public virtual void Apply(object other)
        {
            if (other is SettingStore settingStore)
                Apply(settingStore);
        }
        public void Apply(SettingStore other)
        {
            foreach (var kvp in other._booleanStore)
            {
                if (_booleanStore.GetValueOrDefault(kvp.Key) == null)
                    _booleanStore[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in other._objectStore)
            {
                var self = _objectStore.GetValueOrDefault(kvp.Key);
                if (self == null)
                {
                    var value = kvp.Value;
                    if (value is IApplyable)
                    {
                        var applyable = (IApplyable)Activator.CreateInstance(value.GetType());
                        applyable.Apply(value);
                        value = applyable;
                    }
                    else if (value is IList side)
                    {
                        var list = (IList)Activator.CreateInstance(value.GetType());
                        foreach (var item in side)
                            list.Add(item);
                        value = list;
                    }
                    _objectStore[kvp.Key] = value;
                }
                else if (self is IApplyable applyable)
                {
                    applyable.Apply(kvp.Value);
                }
                else if (self is IList list && kvp.Value is IList side)
                {
                    foreach (var item in side)
                        list.Add(item);
                }
            }
        }
    }
}