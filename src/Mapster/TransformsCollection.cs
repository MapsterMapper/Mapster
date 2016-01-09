using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster
{
    public class TransformsCollection
    {
        private readonly Dictionary<Type, LambdaExpression> _transforms = new Dictionary<Type, LambdaExpression>();

        public Expression<Func<T, T>> Get<T>()
        {
            LambdaExpression found;
            _transforms.TryGetValue(typeof(T), out found);

            return (Expression<Func<T, T>>)found;
        }

        public void Upsert<T>(Expression<Func<T, T>> transform)
        {
            if (transform == null)
                return;
            
            var type = typeof (T);
            _transforms[type] = transform;
        }

        public void Upsert(IDictionary<Type, LambdaExpression> sourceTransforms)
        {
            foreach (var sourceTransform in sourceTransforms)
            {
                _transforms[sourceTransform.Key] = sourceTransform.Value;
            }
        }

        public void TryAdd(IDictionary<Type, LambdaExpression> sourceTransforms)
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
            _transforms.Remove(type);
        }

        public void Clear()
        {
            _transforms.Clear();
        }

        internal IDictionary<Type, LambdaExpression> Transforms
        {
            get { return _transforms; }
        } 

    }
}