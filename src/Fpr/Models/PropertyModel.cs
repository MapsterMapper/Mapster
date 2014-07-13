using System;
using Fpr.Utils;

namespace Fpr.Models
{
    public class PropertyModel<TSource, TDestination>
    {
        public PropertyCaller<TSource>.GenGetter Getter;
        public PropertyCaller<TDestination>.GenSetter Setter;

        //public object DefaultDestinationValue;

        public FastInvokeHandler AdaptInvoker;
        public GenericGetter[] FlatteningInvokers;

        public Func<TSource, object> CustomResolver;

        public Func<TSource, bool> Condition;

        public Func<object, object> DestinationTransform;

        // Use byte, because byte performance better than enum
        public byte ConvertType; //Primitive = 1, FlatteningGetMethod = 2, FlatteningDeep = 3, Adapter = 4, CustomResolve = 5;

        public string SetterPropertyName;

       
    }
}