using DataToolkit.Library.Fluent;
using DataToolkit.Library.Fluent.Sql;
using System.Text;

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
            case SqlRaw r:
                sb.Append(r.Text);
                break;

            case SqlBinary b:
                sb.Append('(');
                Render(sb, b.Left);
                sb.Append(' ').Append(b.Op).Append(' ');
                Render(sb, b.Right);
                sb.Append(')');
                break;

            case SqlGroup g:
                sb.Append('(');
                Render(sb, g.Node);
                sb.Append(')');
                break;

            case SqlParameter p:
                sb.Append(p.Name);
                break;
        }
    }
}