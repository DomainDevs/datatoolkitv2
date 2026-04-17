using System.Text;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Compilation;

internal sealed class SqlCompiler
{


    private void Render(StringBuilder sb, SqlNode node)
    {
        switch (node)
        {
            case SqlRaw r:
                sb.Append("(").Append(r.Text).Append(")");
                break;

            case SqlBinary b:
                sb.Append("(");
                Render(sb, b.Left);
                sb.Append(" ").Append(b.Op).Append(" ");
                Render(sb, b.Right);
                sb.Append(")");
                break;

            case SqlGroupBy g:
                sb.Append("GROUP BY ")
                  .Append(string.Join(", ", g.Columns));
                break;
        }
    }
}