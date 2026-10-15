using System.Linq.Expressions;

namespace Mapster.Models
{
    public interface IMemberModelEx: IMemberModel
    {
        Expression GetExpression(Expression source);
        Expression SetExpression(Expression source, Expression value);
    }
}