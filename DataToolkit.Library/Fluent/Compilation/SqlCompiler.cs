using System.Text;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Compilation;

internal sealed class SqlCompiler
{
    public string Compile(FluentQuery q)
    {
        var sb = new StringBuilder();

        foreach (var node in q.Nodes)
        {
            Render(sb, node);
            sb.Append(' ');
        }

        return sb.ToString().Trim();
    }

    private void Render(StringBuilder sb, SqlNode node)
    {
        switch (node)
        {
            case SqlSelect s:
                sb.Append("SELECT ");
                sb.Append(s.Columns.Count == 0 ? "*" : string.Join(", ", s.Columns));
                break;

            case SqlFrom f:
                sb.Append("FROM ");
                sb.Append(string.Join(", ", f.Tables));
                break;

            // 🔥 FIX PRINCIPAL: JOIN estructurado
            case SqlJoin j:
                sb.Append(j.Type)
                  .Append(' ')
                  .Append(j.Table)
                  .Append(" ON ")
                  .Append(j.On);
                break;

            case SqlRaw r:
                sb.Append(r.Text);
                break;

            case SqlBinary b:
                sb.Append("(");
                Render(sb, b.Left);
                sb.Append(" ").Append(b.Op).Append(" ");
                Render(sb, b.Right);
                sb.Append(")");
                break;

            case SqlGroupBy gb:
                sb.Append("GROUP BY ");
                sb.Append(string.Join(", ", gb.Columns));
                break;

            case SqlOrderBy ob:
                sb.Append("ORDER BY ");
                sb.Append(string.Join(", ", ob.Columns));
                break;

            case SqlParameter p:
                sb.Append(p.Name);
                break;
        }
    }
}