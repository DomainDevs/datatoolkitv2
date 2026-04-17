using DataToolkit.Library.Fluent;
using DataToolkit.Library.Fluent.Sql;
using System.Text;

internal sealed class SqlCompiler
{
    public string Compile(FluentQuery q)
    {
        var sql = new StringBuilder();

        foreach (var node in q.Nodes)
        {
            Render(sql, node);
            sql.Append(' ');
        }

        return sql.ToString().Trim();
    }

    private void Render(StringBuilder sql, SqlNode node)
    {
        switch (node)
        {
            case SqlRaw r:
                sql.Append(r.Text);
                break;

            case SqlBinary b:
                sql.Append('(');
                Render(sql, b.Left);
                sql.Append(' ').Append(b.Op).Append(' ');
                Render(sql, b.Right);
                sql.Append(')');
                break;

            case SqlGroup g:
                sql.Append('(');
                Render(sql, g.Node);
                sql.Append(')');
                break;
        }
    }
}