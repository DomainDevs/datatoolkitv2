using System.Text;

namespace DataToolkit.Library.Fluent;

public sealed class FluentQuery : IFluentQuery
{
    private readonly List<string> _select = new();
    private readonly List<string> _from = new();
    private readonly List<string> _joins = new();

    private readonly List<WhereClause> _where = new();
    private readonly List<string> _groupBy = new();
    private readonly List<string> _orderBy = new();

    private readonly Dictionary<string, object?> _parameters =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _built;
    private string? _cachedSql;

    // ---------------------------
    // SELECT / FROM
    // ---------------------------
    public IFluentQuery Select(params string[] columns)
    {
        EnsureNotBuilt();

        foreach (var c in columns)
            if (!_select.Contains(c))
                _select.Add(c);

        return this;
    }

    public IFluentQuery From(string table)
    {
        EnsureNotBuilt();

        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("FROM table cannot be empty.");

        _from.Add(table);
        return this;
    }

    // ---------------------------
    // JOINS
    // ---------------------------
    public IFluentQuery InnerJoin(string table, string on)
        => AddJoin($"INNER JOIN {table} ON {on}");

    public IFluentQuery LeftJoin(string table, string on)
        => AddJoin($"LEFT JOIN {table} ON {on}");

    public IFluentQuery RightJoin(string table, string on)
        => AddJoin($"RIGHT JOIN {table} ON {on}");

    public IFluentQuery FullJoin(string table, string on)
        => AddJoin($"FULL JOIN {table} ON {on}");

    private IFluentQuery AddJoin(string join)
    {
        EnsureNotBuilt();

        if (string.IsNullOrWhiteSpace(join))
            throw new ArgumentException("JOIN cannot be empty.");

        _joins.Add(join);
        return this;
    }

    // ---------------------------
    // WHERE
    // ---------------------------
    public IFluentQuery Where(string condition, object? parameters = null)
        => AddWhere("AND", condition, parameters);

    public IFluentQuery WhereIf(bool condition, string sqlCondition, object? parameters = null)
        => condition ? Where(sqlCondition, parameters) : this;

    public IFluentQuery And(string condition, object? parameters = null)
        => AddWhere("AND", condition, parameters);

    public IFluentQuery Or(string condition, object? parameters = null)
        => AddWhere("OR", condition, parameters);

    private IFluentQuery AddWhere(string op, string condition, object? parameters)
    {
        EnsureNotBuilt();

        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("WHERE condition cannot be empty.");

        MergeParameters(parameters);

        _where.Add(new WhereClause(op, condition));
        return this;
    }

    // ---------------------------
    // GROUP / ORDER
    // ---------------------------
    public IFluentQuery GroupBy(params string[] columns)
    {
        EnsureNotBuilt();
        _groupBy.AddRange(columns.Distinct());
        return this;
    }

    public IFluentQuery OrderBy(params string[] columns)
    {
        EnsureNotBuilt();
        _orderBy.AddRange(columns.Distinct());
        return this;
    }

    // ---------------------------
    // BUILD
    // ---------------------------
    public (string Sql, object Parameters) Build()
    {
        if (_built)
            return (_cachedSql!, _parameters);

        Validate();

        _cachedSql = BuildSql();
        _built = true;

        return (_cachedSql, _parameters);
    }

    public string ToSql() => Build().Sql;

    // ---------------------------
    // CORE BUILDER
    // ---------------------------
    private string BuildSql()
    {
        var sql = new StringBuilder(256);

        sql.Append("SELECT ")
           .Append(_select.Count > 0 ? string.Join(", ", _select) : "*")
           .Append(' ');

        sql.Append("FROM ")
           .Append(string.Join(", ", _from))
           .Append(' ');

        if (_joins.Count > 0)
            sql.Append(string.Join(" ", _joins)).Append(' ');

        BuildWhere(sql);

        if (_groupBy.Count > 0)
            sql.Append("GROUP BY ")
               .Append(string.Join(", ", _groupBy))
               .Append(' ');

        if (_orderBy.Count > 0)
            sql.Append("ORDER BY ")
               .Append(string.Join(", ", _orderBy))
               .Append(' ');

        return sql.ToString().Trim();
    }

    // ---------------------------
    // WHERE BUILDER (SIMPLIFIED BUT SAFE)
    // ---------------------------
    private void BuildWhere(StringBuilder sql)
    {
        if (_where.Count == 0) return;

        sql.Append("WHERE ");

        for (int i = 0; i < _where.Count; i++)
        {
            var w = _where[i];

            if (i > 0)
                sql.Append(' ').Append(w.Op).Append(' ');

            sql.Append('(').Append(w.Condition).Append(')');
        }

        sql.Append(' ');
    }

    // ---------------------------
    // VALIDATION (LIGHT ENTERPRISE)
    // ---------------------------
    private void Validate()
    {
        if (_from.Count == 0)
            throw new InvalidOperationException("FROM clause is required.");
    }

    // ---------------------------
    // PARAMETERS
    // ---------------------------
    private void MergeParameters(object? parameters)
    {
        if (parameters is null) return;

        foreach (var prop in parameters.GetType().GetProperties())
        {
            _parameters.TryAdd(prop.Name, prop.GetValue(parameters));
        }
    }

    // ---------------------------
    // STATE
    // ---------------------------
    private void EnsureNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException("Query already built and frozen.");
    }

    // ---------------------------
    // INTERNAL MODEL
    // ---------------------------
    private sealed record WhereClause(string Op, string Condition);
}