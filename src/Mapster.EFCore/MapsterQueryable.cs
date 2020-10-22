using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetEnumerator() => ((IAsyncQueryProvider) this.Provider).ExecuteAsync<T>(this.Expression).GetEnumerator();
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

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            var enumerable = ((IAsyncQueryProvider) _provider).ExecuteAsync<TResult>(expression);
            return new MapsterAsyncEnumerable<TResult>(enumerable, _builder);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            using (_builder.CreateMapContextScope())
            {
                return await ((IAsyncQueryProvider) _provider).ExecuteAsync<TResult>(expression, cancellationToken);
            }
        }
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

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new MapsterAsyncEnumerator<T>(_enumerable.GetEnumerator(), _builder);
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

        public void Dispose()
        {
            _scope.Dispose();
        }

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return _enumerator.MoveNext(cancellationToken);
        }

        public T Current => _enumerator.Current;
    }
}
