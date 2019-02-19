using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;

namespace Mapster
{
    public static class Extensions
    {
        public static IQueryable<TDestination> ProjectToType<TDestination>(this IQueryable source, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var mockCall = config.GetProjectionCallExpression(source.ElementType, typeof(TDestination));
            var sourceCall = Expression.Call(mockCall.Method, source.Expression, mockCall.Arguments[1]);
            return source.Provider.CreateQuery<TDestination>(sourceCall);
        }

        public static bool HasCustomAttribute(this IMemberModel member, Type type)
        {
            return member.GetCustomAttributes(true).Any(attr => attr.GetType() == type);
        }

        public static T GetCustomAttribute<T>(this IMemberModel member)
        {
            return (T) member.GetCustomAttributes(true).FirstOrDefault(attr => attr is T);
        }
    }
}
