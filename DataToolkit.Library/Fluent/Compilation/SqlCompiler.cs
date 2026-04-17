using System.Text;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Compilation;

internal sealed class SqlCompiler
{
    public string Compile(FluentQuery q)
    {
        var sb = new StringBuilder();

        // SELECT
        foreach (var n in q.Nodes)
            if (n is SqlSelect s)
            {
                sb.Append("SELECT ");
                sb.Append(s.Columns.Count == 0 ? "*" : string.Join(", ", s.Columns));
                sb.Append(' ');
            }

        // FROM
        foreach (var n in q.Nodes)
            if (n is SqlFrom f)
            {
                sb.Append("FROM ");
                sb.Append(string.Join(", ", f.Tables));
                sb.Append(' ');
            }

        // JOIN
        foreach (var n in q.Nodes)
            if (n is SqlJoin j)
            {
                sb.Append(j.Sql);
                sb.Append(' ');
            }

        // WHERE (SEPARADO Y CORRECTO)
        if (q.WhereNodes.Count > 0)
        {
            sb.Append("WHERE ");

            for (int i = 0; i < q.WhereNodes.Count; i++)
            {
                if (i > 0)
                    sb.Append(" AND ");

                Render(sb, q.WhereNodes[i]);
            }

            sb.Append(' ');
        }

        // GROUP BY
        foreach (var n in q.Nodes)
            if (n is SqlGroupBy gb)
            {
                sb.Append("GROUP BY ");
                sb.Append(string.Join(", ", gb.Columns));
                sb.Append(' ');
            }

        // ORDER BY
        foreach (var n in q.Nodes)
            if (n is SqlOrderBy ob)
            {
                sb.Append("ORDER BY ");
                sb.Append(string.Join(", ", ob.Columns));
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