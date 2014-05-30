using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Omu.ValueInjecter;

namespace DeepCloning.SmartConvention
{
    public class SmartConventionInjection : ValueInjection
    {
        private class Path
        {
            public IDictionary<string, string> MatchingProps { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<KeyValuePair<Type, Type>, Path>> WasLearned = new ConcurrentDictionary<Type, ConcurrentDictionary<KeyValuePair<Type, Type>, Path>>();

        protected virtual void SetValue(PropertyDescriptor prop, object component, object value)
        {
            prop.SetValue(component, value);
        }

        protected virtual object GetValue(PropertyDescriptor prop, object component)
        {
            return prop.GetValue(component);
        }

        protected virtual bool Match(SmartConventionInfo c)
        {
            return c.SourceProp.Name == c.TargetProp.Name && c.SourceProp.PropertyType == c.TargetProp.PropertyType;
        }

        protected virtual void ExecuteMatch(SmartMatchInfo mi)
        {
            SetValue(mi.TargetProp, mi.Target, GetValue(mi.SourceProp, mi.Source));
        }

        private Path Learn(object source, object target)
        {
            Path path = null;
            var sourceProps = source.GetProps();
            var targetProps = target.GetProps();
            var smartConventionInfo = new SmartConventionInfo
                {
                    SourceType = source.GetType(),
                    TargetType = target.GetType()
                };

            for (var i = 0; i < sourceProps.Count; i++)
            {
                var sourceProp = sourceProps[i];
                smartConventionInfo.SourceProp = sourceProp;

                for (var j = 0; j < targetProps.Count; j++)
                {
                    var targetProp = targetProps[j];
                    smartConventionInfo.TargetProp = targetProp;

                    if (!Match(smartConventionInfo)) continue;
                    if (path == null)
                        path = new Path
                            {
                                MatchingProps = new Dictionary<string, string> { { smartConventionInfo.SourceProp.Name, smartConventionInfo.TargetProp.Name } }
                            };
                    else path.MatchingProps.Add(smartConventionInfo.SourceProp.Name, smartConventionInfo.TargetProp.Name);
                }
            }
            return path;
        }

        protected override void Inject(object source, object target)
        {
            var sourceProps = source.GetProps();
            var targetProps = target.GetProps();

            var cacheEntry = WasLearned.GetOrAdd(GetType(), new ConcurrentDictionary<KeyValuePair<Type, Type>, Path>());

            var path = cacheEntry.GetOrAdd(new KeyValuePair<Type, Type>(source.GetType(), target.GetType()), pair => Learn(source, target));

            if (path == null) return;

            foreach (var pair in path.MatchingProps)
            {
                var sourceProp = sourceProps.GetByName(pair.Key);
                var targetProp = targetProps.GetByName(pair.Value);
                ExecuteMatch(new SmartMatchInfo
                    {
                        Source = source, 
                        Target = target, 
                        SourceProp = sourceProp, 
                        TargetProp = targetProp
                    });
            }
        }
    }
}