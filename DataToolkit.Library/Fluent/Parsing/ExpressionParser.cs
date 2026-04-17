using System.Linq.Expressions;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Parsing;

public static class ExpressionParser
{
    public static SqlNode Parse(Expression expr)
    {
        return expr switch
        {
            BinaryExpression b => new SqlBinary(
                Parse(b.Left),
                GetOp(b.NodeType),
                Parse(b.Right)
            ),

            MemberExpression m => new SqlRaw($"[{m.Member.Name}]"),

            ConstantExpression c => new SqlParameter(
                $"@p{Guid.NewGuid():N}",
                c.Value
            ),

            UnaryExpression u => Parse(u.Operand),

            _ => new SqlRaw(expr.ToString())
        };
    }

    private static string GetOp(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.Equal => "=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operator not supported: {type}")
        };
    }
}