using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using DataToolkit.Library.Fluent.Parsing;
using DataToolkit.Library.Fluent.Sql;
using DataToolkit.Library.Fluent.Compilation;

namespace DataToolkit.Library.Fluent;

public sealed class FluentQuery : IFluentQuery
{
    private readonly List<SqlNode> _nodes = new();

    private readonly Dictionary<string, object?> _parameters =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _paramKeys =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _built;
    private string? _cachedSql;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache = new();

    // ---------------- SELECT ----------------
    public IFluentQuery Select(params string[] columns)
    {
        EnsureNotBuilt();

        _nodes.Add(new SqlSelect(columns?.ToList() ?? new List<string>()));
        return this;
    }

    // ---------------- FROM ----------------
    public IFluentQuery From(params string[] tables)
    {
        EnsureNotBuilt();

        _nodes.Add(new SqlFrom(tables?.ToList() ?? new List<string>()));
        return this;
    }

    // ---------------- JOIN ----------------
    public IFluentQuery InnerJoin(string table, Expression<Func<bool>> on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin(JoinType.Inner, table, ExpressionParser.Parse(on.Body)));
        return this;
    }

    public IFluentQuery LeftJoin(string table, Expression<Func<bool>> on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin(JoinType.Left, table, ExpressionParser.Parse(on.Body)));
        return this;
    }

    public IFluentQuery RightJoin(string table, Expression<Func<bool>> on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin(JoinType.Right, table, ExpressionParser.Parse(on.Body)));
        return this;
    }

    public IFluentQuery FullJoin(string table, Expression<Func<bool>> on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin(JoinType.Full, table, ExpressionParser.Parse(on.Body)));
        return this;
    }

    // ---------------- WHERE ----------------
    public IFluentQuery Where(string sql, object? parameters = null)
    {
        EnsureNotBuilt();

        Merge(parameters);

        _nodes.Add(new SqlRaw(sql));
        return this;
    }

    public IFluentQuery Where<T>(Expression<Func<T, bool>> expr)
    {
        EnsureNotBuilt();

        var node = ExpressionParser.Parse(expr.Body);
        _nodes.Add(node);

        return this;
    }

    public IFluentQuery WhereIf(bool condition, string sql, object? parameters = null)
    {
        EnsureNotBuilt();

        if (!condition)
            return this;

        return Where(sql, parameters);
    }

    // ---------------- GROUP BY ----------------
    public IFluentQuery GroupBy(params string[] columns)
    {
        EnsureNotBuilt();

        _nodes.Add(new SqlGroupBy(columns?.ToList() ?? new List<string>()));
        return this;
    }

    // ---------------- ORDER BY ----------------
    public IFluentQuery OrderBy(params string[] columns)
    {
        EnsureNotBuilt();

        _nodes.Add(new SqlOrderBy(columns?.ToList() ?? new List<string>()));
        return this;
    }

    // ---------------- BUILD ----------------
    public (string Sql, object Parameters) Build()
    {
        if (_built)
            return (_cachedSql!, _parameters);

        _cachedSql = new SqlCompiler().Compile(this);
        _built = true;

        return (_cachedSql, _parameters);
    }

    public string ToSql() => Build().Sql;

    // ---------------- INTERNAL ----------------
    internal IReadOnlyList<SqlNode> Nodes => _nodes;

    internal IReadOnlyDictionary<string, object?> Parameters => _parameters;

    // ---------------- PARAMS ----------------
    private void Merge(object? parameters)
    {
        if (parameters is null) return;

        var type = parameters.GetType();
        var props = _cache.GetOrAdd(type, t => t.GetProperties());

        foreach (var p in props)
        {
            var key = "@" + p.Name;

            if (!_paramKeys.Add(key))
                throw new InvalidOperationException($"Duplicate parameter: {key}");

            _parameters[key] = p.GetValue(parameters);
        }
    }

    private void EnsureNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException("Query already built.");
    }
}