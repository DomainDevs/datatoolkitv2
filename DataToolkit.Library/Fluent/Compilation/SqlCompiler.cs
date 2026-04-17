using System.Text;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Compilation;

internal sealed class SqlCompiler
{
    public string Compile(FluentQuery q)
    {
        var sb = new StringBuilder();

        // ---------------- SELECT ----------------
        var select = q.Nodes.OfType<SqlSelect>().FirstOrDefault();
        sb.Append("SELECT ");
        sb.Append(select is null || select.Columns.Count == 0
            ? "*"
            : string.Join(", ", select.Columns));
        sb.AppendLine();

        // ---------------- FROM ----------------
        var from = q.Nodes.OfType<SqlFrom>().FirstOrDefault();
        sb.Append("FROM ");
        sb.Append(from is null
            ? ""
            : string.Join(", ", from.Tables));
        sb.AppendLine();

        // ---------------- JOIN ----------------
        foreach (var join in q.Nodes.OfType<SqlJoin>())
        {
            sb.Append(join.Type);
            sb.Append(" ");
            sb.Append(join.Table);
            sb.Append(" ON ");
            sb.Append(join.On);
            sb.AppendLine();
        }

        // ---------------- WHERE ----------------
        var whereNodes = q.Nodes
            .Where(n => n is SqlRaw || n is SqlBinary)
            .ToList();

        if (whereNodes.Count > 0)
        {
            sb.Append("WHERE ");

            for (int i = 0; i < whereNodes.Count; i++)
            {
                if (i > 0)
                    sb.Append(" AND ");

                Render(sb, whereNodes[i]);
            }

            sb.AppendLine();
        }

        // ---------------- GROUP BY ----------------
        var groupBy = q.Nodes.OfType<SqlGroupBy>().FirstOrDefault();

        if (groupBy is not null && groupBy.Columns.Count > 0)
        {
            sb.Append("GROUP BY ");
            sb.Append(string.Join(", ", groupBy.Columns));
            sb.AppendLine();
        }

        // ---------------- ORDER BY ----------------
        var orderBy = q.Nodes.OfType<SqlOrderBy>().FirstOrDefault();

        if (orderBy is not null && orderBy.Columns.Count > 0)
        {
            sb.Append("ORDER BY ");
            sb.Append(string.Join(", ", orderBy.Columns));
        }

        return sb.ToString().Trim();
    }

    private void Render(StringBuilder sb, SqlNode node)
    {
        switch (node)
        {
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
        }
    }
}