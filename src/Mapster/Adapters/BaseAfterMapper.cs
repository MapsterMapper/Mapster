using System.Linq.Expressions;

namespace Mapster.Adapters
{
    public abstract class BaseAfterMapper
    {
        protected virtual int Score => 0;

        public virtual int? Priority(PreCompileArgument arg)
        {
            return CanMap(arg) ? this.Score : (int?)null;
        }

        protected abstract bool CanMap(PreCompileArgument arg);

        public LambdaExpression CreateAfterMapFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return Expression.Lambda(body, p, p2);
        }

        protected abstract Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg);

        public TypeAdapterRule CreateRule()
        {
            var settings = new TypeAdapterSettings();
            settings.AfterMappingFactories.Add(this.CreateAfterMapFunc);
            var rule = new TypeAdapterRule
            {
                Priority = this.Priority,
                Settings = settings,
            };
            DecorateRule(rule);
            return rule;
        }

        protected virtual void DecorateRule(TypeAdapterRule rule) { }
    }
}
