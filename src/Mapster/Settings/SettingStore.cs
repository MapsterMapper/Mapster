using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mapster
{
    public class SettingStore: IApplyable<SettingStore>
    {
        private readonly ConcurrentDictionary<string, object> _objectStore = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, bool?> _booleanStore = new ConcurrentDictionary<string, bool?>();

        public void Set(string key, bool? value)
        {
            if (value == null)
                _booleanStore.TryRemove(key, out _);
            else
                _booleanStore[key] = value;
        }

        public void Set(string key, object? value)
        {
            if (value == null)
                _objectStore.TryRemove(key, out _);
            else
                _objectStore[key] = value;
        }


        public T GetEnum<T>(string key, Func<T> initializer) where T : System.Enum
        {
            return (T)_objectStore.GetOrAdd(key, _ => initializer());
        }

        public bool? Get(string key)
        {
            return _booleanStore.TryGetValue(key, out var value) ? value : null;
        }

        public T? Get<T>(string key) where T : class
        {
            return _objectStore.TryGetValue(key, out var value) ? (T)value : null;
        }

        public T Get<T>(string key, Func<T> initializer) where T : class
        {
            return (T)_objectStore.GetOrAdd(key, _ => initializer());
        }


        private void ApplyBoolSettings (IEnumerable<KeyValuePair<string, bool?>> otherBoolStore)
        {
            foreach (var kvp in otherBoolStore)
            {
                _booleanStore.TryAdd(kvp.Key, kvp.Value);
            }
        }

        private void ApplyObjectSettings(IEnumerable<KeyValuePair<string, object?>> otherBoolStore)
        {
            foreach (var kvp in otherBoolStore)
            {
                var self = _objectStore.GetOrAdd(kvp.Key, key =>
                {
                    var value = kvp.Value;
                    if (value is IApplyable)
                        return (IApplyable)Activator.CreateInstance(value.GetType())!;
                    if (value is IList)
                        return (IList)Activator.CreateInstance(value.GetType())!;
                    return value;
                });
                if (self is IApplyable applyable)
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


        public virtual void Apply(object other)
        {
            if (other is SettingStore settingStore)
                Apply(settingStore);
        }


        public virtual void Apply(SettingStore other)
        {
            ApplyBoolSettings(other._booleanStore);
            ApplyObjectSettings(other._objectStore);
        }

        public virtual void ApplyWithSkipSettings(SettingStore other, List<String> skipSettingNames)
        {
            ApplyBoolSettings(other._booleanStore.Where(x => !skipSettingNames.Contains(x.Key)));
            ApplyObjectSettings(other._objectStore.Where(x => !skipSettingNames.Contains(x.Key)));
        }

    }
}