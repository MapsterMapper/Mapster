using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fpr.Utils;

namespace Fpr
{
    public static class IQueryableExtensions
    {
        public static ProjectionExpression<TSource> Project<TSource>(this IQueryable<TSource> source)
        {
            return new ProjectionExpression<TSource>(source);
        }
    }
}
