using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster
{
    public class TransformsCollection
    {
        private readonly Dictionary<Type, Func<object, object>> _transforms = new Dictionary<Type, Func<object, object>>();

        public Func<object, object> Get<T>()
        {
            Func<object, object> found;
            _transforms.TryGetValue(typeof(T), out found);

            return found;
        }

        public void Upsert<T>(Expression<Func<T, T>> transform)
        {
            if (transform == null)
                return;

            var compiledFunction = transform.Compile();

            Func<object, object> wrappedFunction = (x) => compiledFunction((T)x);

            var type = typeof (T);

            if (_transforms.ContainsKey(type))
            {
                _transforms[type] = wrappedFunction;
            }
            else
            {
                _transforms.Add(type, wrappedFunction);
            }
        }

        public void Upsert(IDictionary<Type, Func<object, object>> sourceTransforms)
        {
            foreach (var sourceTransform in sourceTransforms)
            {
                if (_transforms.ContainsKey(sourceTransform.Key))
                {
                    _transforms[sourceTransform.Key] = sourceTransform.Value;
                }
                else
                {
                    _transforms.Add(sourceTransform.Key, sourceTransform.Value);
                }
            }
        }

        public void TryAdd(IDictionary<Type, Func<object, object>> sourceTransforms)
        {
            foreach (var sourceTransform in sourceTransforms)
            {
                if (!_transforms.ContainsKey(sourceTransform.Key))
                {
                    _transforms.Add(sourceTransform.Key, sourceTransform.Value);
                }
            }
        }

        public void Remove<T>()
        {
            var type = typeof (T);

            if (_transforms.ContainsKey(type))
                _transforms.Remove(type);
        }

        public void Clear()
        {
            _transforms.Clear();
        }

        internal IDictionary<Type, Func<object, object>> Transforms
        {
            get { return _transforms; }
        } 

    }
}