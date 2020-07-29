using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Newtonsoft.Json.Linq;

namespace Mapster.JsonNet
{
    public class JsonAdapter : BaseAdapter
    {
        protected override int Score => -111;   //execute after string (-110)
        protected override bool CheckExplicitMapping => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return typeof(JToken).IsAssignableFrom(arg.SourceType) ||
                   typeof(JToken).IsAssignableFrom(arg.DestinationType);
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            //source & dest are json, just return reference
            if (arg.SourceType == arg.DestinationType)
                return source;

            //from json
            if (typeof(JToken).IsAssignableFrom(arg.SourceType))
            {
                //json.ToObject<T>();
                var toObject = (from method in arg.SourceType.GetMethods()
                                where method.Name == nameof(JToken.ToObject) &&
                                      method.IsGenericMethod &&
                                      method.GetParameters().Length == 0
                                select method).First().MakeGenericMethod(arg.DestinationType);
                return Expression.Call(source, toObject);
            }

            else //to json
            {
                //JToken.FromObject(source);
                var fromObject = typeof(JToken).GetMethod(nameof(JToken.FromObject), new[] {typeof(object)});
                Expression exp = Expression.Call(fromObject, source);
                if (arg.DestinationType != typeof(JToken))
                    exp = Expression.Convert(exp, arg.DestinationType);
                return exp;
            }
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            throw new System.NotImplementedException();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            throw new System.NotImplementedException();
        }
    }
}
