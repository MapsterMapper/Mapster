using System;
using System.Collections.Generic;

namespace Fpr.Utils
{
    /// <summary>
    /// In some cases, this appears to function faster than a Concurrent Dictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ThreadSafeCache<TKey, TValue> where TValue : class
    {
        private readonly object _syncLock  = new object();
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public void TryAdd(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key))
                return;

            lock (_syncLock)
            {
                if(!_dictionary.ContainsKey(key))
                    _dictionary.Add(key, value);
            }
        }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            TValue value;
            if (_dictionary.TryGetValue(key, out value))
                return _dictionary[key];

            lock (_syncLock)
            {
                if (_dictionary.TryGetValue(key, out value))
                    return _dictionary[key];

                value = factory();
                _dictionary.Add(key, value);
                return value;
            }
        }

        public void Upsert(TKey key, TValue value)
        {
            lock (_syncLock)
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary[key] = value;
                }
                else
                {
                    _dictionary.Add(key, value);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue GetValue(TKey key)
        {
            if (!_dictionary.ContainsKey(key))
                return null;

            lock (_syncLock)
            {
                if (_dictionary.ContainsKey(key))
                   return _dictionary[key];
            }
            return null;
        }

        public bool HasValue(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (!_dictionary.ContainsKey(key))
                return false;

            lock (_syncLock)
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary.Remove(key);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            lock (_syncLock)
            {
                _dictionary.Clear();
            }
        }

        public Dictionary<TKey, TValue> Dictionary
        {
            get { return _dictionary; }
        } 

    }
}