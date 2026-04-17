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
    private readonly List<SqlNode> _whereNodes = new();

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
    public IFluentQuery InnerJoin(string table, string on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin("INNER JOIN", table, on));
        return this;
    }

    public IFluentQuery LeftJoin(string table, string on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin("LEFT JOIN", table, on));
        return this;
    }

    public IFluentQuery RightJoin(string table, string on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin("RIGHT JOIN", table, on));
        return this;
    }

    public IFluentQuery FullJoin(string table, string on)
    {
        EnsureNotBuilt();
        _nodes.Add(new SqlJoin("FULL JOIN", table, on));
        return this;
    }

    // ---------------- WHERE ----------------
    public IFluentQuery Where(string sql, object? parameters = null)
    {
        EnsureNotBuilt();

        Merge(parameters);
        _whereNodes.Add(new SqlRaw(sql));

        return this;
    }

    public IFluentQuery WhereIf(bool condition, string sql, object? parameters = null)
    {
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
    internal IReadOnlyList<SqlNode> WhereNodes => _whereNodes;
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