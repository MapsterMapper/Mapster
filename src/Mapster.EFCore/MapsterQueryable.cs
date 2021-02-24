using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Mapster.EFCore
{
    class MapsterQueryable : IQueryable
    {
        private readonly IQueryable _queryable;
        public MapsterQueryable(IQueryable queryable, IAdapterBuilder builder)
        {
            _queryable = queryable;
            this.Provider = new MapsterQueryableProvider(queryable.Provider, builder);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.Provider.Execute<IEnumerable>(this.Expression).GetEnumerator();

        public Type ElementType => _queryable.ElementType;
        public Expression Expression => _queryable.Expression;
        public IQueryProvider Provider { get; }
    }
    class MapsterQueryable<T> : MapsterQueryable, IQueryable<T>, IAsyncEnumerable<T>
    {
        public MapsterQueryable(IQueryable<T> queryable, IAdapterBuilder builder) : 
            base(queryable, builder) { }
        public IEnumerator<T> GetEnumerator() => this.Provider.Execute<IEnumerable<T>>(this.Expression).GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var enumerable = this.Provider is MapsterQueryableProvider mp
                ? mp.ExecuteEnumerableAsync<T>(this.Expression, cancellationToken)
                : ((IAsyncQueryProvider) this.Provider)
                    .ExecuteAsync<IAsyncEnumerable<T>>(this.Expression, cancellationToken);
            return enumerable.GetAsyncEnumerator(cancellationToken);
        }
    }

    class MapsterQueryableProvider : IAsyncQueryProvider
    {
        private readonly IQueryProvider _provider;
        private readonly IAdapterBuilder _builder;

        public MapsterQueryableProvider(IQueryProvider provider, IAdapterBuilder builder)
        {
            _provider = provider;
            _builder = builder;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new MapsterQueryable(_provider.CreateQuery(expression), _builder);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MapsterQueryable<TElement>(_provider.CreateQuery<TElement>(expression), _builder);
        }

        public object Execute(Expression expression)
        {
            using (_builder.CreateMapContextScope())
            {
                return _provider.Execute(expression);
            }
        }

        public TResult Execute<TResult>(Expression expression)
        {
            using (_builder.CreateMapContextScope())
            {
                return _provider.Execute<TResult>(expression);
            }
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var enumerable = ((IAsyncQueryProvider)_provider).ExecuteAsync<TResult>(expression, cancellationToken);
            var enumerableType = typeof(TResult);
            var elementType = enumerableType.GetGenericArguments()[0];
            var wrapType = typeof(MapsterAsyncEnumerable<>).MakeGenericType(elementType);
            return (TResult) Activator.CreateInstance(wrapType, enumerable, _builder);
        }

        public IAsyncEnumerable<TResult> ExecuteEnumerableAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var enumerable = ((IAsyncQueryProvider)_provider).ExecuteAsync<IAsyncEnumerable<TResult>>(expression, cancellationToken);
            return new MapsterAsyncEnumerable<TResult>(enumerable, _builder);
        }

        //public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        //{
        //    var enumerable = ((IAsyncQueryProvider) _provider).ExecuteAsync<IAsyncEnumerable<TResult>>(expression);
        //    return new MapsterAsyncEnumerable<TResult>(enumerable, _builder);
        //}

        //public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        //{
        //    using (_builder.CreateMapContextScope())
        //    {
        //        return await ((IAsyncQueryProvider) _provider).ExecuteAsync<TResult>(expression, cancellationToken);
        //    }
        //}
    }

    class MapsterAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _enumerable;
        private readonly IAdapterBuilder _builder;
        public MapsterAsyncEnumerable(IAsyncEnumerable<T> enumerable, IAdapterBuilder builder)
        {
            _enumerable = enumerable;
            _builder = builder;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new MapsterAsyncEnumerator<T>(_enumerable.GetAsyncEnumerator(cancellationToken), _builder);
        }
    }

    class MapsterAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _enumerator;
        private readonly MapContextScope _scope;
        public MapsterAsyncEnumerator(IAsyncEnumerator<T> enumerator, IAdapterBuilder builder)
        {
            _enumerator = enumerator;
            _scope = builder.CreateMapContextScope();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return _enumerator.MoveNextAsync();
        }

        public T Current => _enumerator.Current;
        public ValueTask DisposeAsync()
        {
            _scope.Dispose();
            return _enumerator.DisposeAsync();
        }
    }
}
