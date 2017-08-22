using System.Linq.Expressions;

namespace Mapster.Models
{
    internal interface IMemberModelEx: IMemberModel
    {
        Expression GetExpression(Expression source);
        Expression SetExpression(Expression source, Expression value);
    }
}