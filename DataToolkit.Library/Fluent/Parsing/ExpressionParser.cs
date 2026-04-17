using System.Linq.Expressions;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Parsing;

internal static class ExpressionParser
{
    public static SqlNode Parse(Expression expr)
    {
        return expr switch
        {
            BinaryExpression b => ParseBinary(b),
            MemberExpression m => new SqlRaw(m.Member.Name),
            ConstantExpression c => new SqlParameter("@p" + Guid.NewGuid().ToString("N")[..6], c.Value),
            _ => throw new NotSupportedException(expr.NodeType.ToString())
        };
    }

    private static SqlNode ParseBinary(BinaryExpression b)
    {
        var op = b.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            _ => throw new NotSupportedException(b.NodeType.ToString())
        };

        return new SqlBinary(
            op,
            Parse(b.Left),
            Parse(b.Right)
        );
    }
}