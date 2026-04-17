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

            MemberExpression m => new SqlRaw(m.Member.Name),

            ConstantExpression c => new SqlParameter(c.Value?.ToString() ?? "NULL"),

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
            _ => throw new NotSupportedException($"Operator not supported: {type}")
        };
    }
}