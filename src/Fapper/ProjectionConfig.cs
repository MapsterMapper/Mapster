using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Fapper.Models;
using Fapper.Utils;

namespace Fapper
{
    internal class BaseProjectionConfig
    {
        internal List<string> IgnoreMembers = new List<string>();

        internal List<ExpressionModel> Expressions = new List<ExpressionModel>();

        public int MaxDepth { get; set; }
    }

    internal class ProjectionConfig<TSource, TDestination>
    {
        public static ProjectionConfig<TSource, TDestination> NewConfig()
        {
            int key = ReflectionUtils.GetHashKey<TSource, TDestination>();
            var cache = ProjectionExpression<TSource>.ConfigurationCache;
            if (cache.ContainsKey(key))
                cache.Remove(key);

            return new ProjectionConfig<TSource, TDestination>();
        }

        public ProjectionConfig<TSource, TDestination> IgnoreMember(params Expression<Func<TDestination, object>>[] members)
        {
            if (members != null && members.Length > 0)
            {
                var ignoreMembers = new List<string>();

                for (int i = 0; i < members.Length; i++)
                {
                    var memberExp = ReflectionUtils.GetMemberInfo(members[i]);
                    if (memberExp != null)
                    {
                        ignoreMembers.Add(memberExp.Member.Name);
                    }
                }

                SetCache(ignoreMembers.ToArray());
            }

            return this;
        }

        public ProjectionConfig<TSource, TDestination> IgnoreMember(params string[] members)
        {
            if (members != null && members.Length > 0)
            {
                members = typeof(TDestination).GetProperties().Where(p => members.Contains(p.Name)).Select(p => p.Name).ToArray();

                if (members.Length > 0)
                {
                    SetCache(members);
                }
            }

            return this;
        }

        public ProjectionConfig<TSource, TDestination> MapFrom<TKey>(Expression<Func<TDestination, TKey>> destinationMember, Expression<Func<TSource, TKey>> sourceExpression)
        {
            if (sourceExpression != null)
            {
                var memberExp = destinationMember.Body as MemberExpression;
                if (memberExp != null)
                {
                    SetCache(new ExpressionModel { DestinationMemberName = memberExp.Member.Name, SourceExpression = sourceExpression });
                }
            }

            return this;
        }

        /// <summary>
        /// Set MaxDepth Value. Default 3
        /// </summary>
        /// <param name="maxDepth">int maxDepth</param>
        /// <returns>return instance</returns>
        public ProjectionConfig<TSource, TDestination> MaxDepth(int maxDepth)
        {
            SetCache(maxDepth);
            return this;
        }

        private static void SetCache(int maxDepth)
        {
            int key = ReflectionUtils.GetHashKey<TSource, TDestination>();
            var cache = ProjectionExpression<TSource>.ConfigurationCache;
            if (cache.ContainsKey(key))
            {
                cache[key].MaxDepth = maxDepth;
            }
            else
            {
                var config = new BaseProjectionConfig {MaxDepth = maxDepth};
                cache.Add(key, config);
            }
        }

        private static void SetCache(params string[] ignoreMembers)
        {
            if (ignoreMembers == null || ignoreMembers.Length == 0)
                return;

            int key = ReflectionUtils.GetHashKey<TSource, TDestination>();
            var cache = ProjectionExpression<TSource>.ConfigurationCache;
            if (cache.ContainsKey(key))
            {
                cache[key].IgnoreMembers.AddRange(ignoreMembers);
            }
            else
            {
                var config = new BaseProjectionConfig();
                config.IgnoreMembers.AddRange(ignoreMembers);
                cache.Add(key, config);
            }
        }

        private static void SetCache(params ExpressionModel[] expressionModels)
        {
            if (expressionModels == null || expressionModels.Length == 0)
                return;

            int key = ReflectionUtils.GetHashKey<TSource, TDestination>();
            var cache = ProjectionExpression<TSource>.ConfigurationCache;
            if (cache.ContainsKey(key))
            {
                cache[key].Expressions.AddRange(expressionModels);
            }
            else
            {
                var config = new BaseProjectionConfig();
                config.Expressions.AddRange(expressionModels);
                cache.Add(key, config);
            }
        }

    }
}
