using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using DataToolkit.Library.Fluent.Sql;
using DataToolkit.Library.Fluent.Parsing;
using DataToolkit.Library.Fluent.Compilation;

namespace DataToolkit.Library.Fluent;

public sealed class FluentQuery : IFluentQuery
{
    private SqlSelect? _select;
    private SqlFrom? _from;

    private readonly List<SqlJoin> _joins = new();
    private readonly List<SqlNode> _where = new();
    private SqlGroupBy? _groupBy;
    private SqlOrderBy? _orderBy;

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
        _select = new SqlSelect(columns.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
        return this;
    }

    // ---------------- FROM ----------------
    public IFluentQuery From(string table)
    {
        EnsureNotBuilt();
        _from = new SqlFrom(new List<string> { table });
        return this;
    }

    // ---------------- JOIN ----------------
    public IFluentQuery Join(string sql)
    {
        EnsureNotBuilt();
        _joins.Add(new SqlJoin(sql));
        return this;
    }

    // ---------------- WHERE RAW ----------------
    public IFluentQuery Where(string sql, object? parameters = null)
    {
        EnsureNotBuilt();

        Merge(parameters);
        _where.Add(new SqlRaw(sql));

        return this;
    }

    // ---------------- WHERE EXPRESSIONS ----------------
    public IFluentQuery Where<T>(Expression<Func<T, bool>> expr)
    {
        EnsureNotBuilt();

        _where.Add(ExpressionParser.Parse(expr.Body));
        return this;
    }

    // ---------------- GROUP BY ----------------
    public IFluentQuery GroupBy(params string[] columns)
    {
        EnsureNotBuilt();
        _groupBy = new SqlGroupBy(columns.ToList());
        return this;
    }

    // ---------------- ORDER BY ----------------
    public IFluentQuery OrderBy(params string[] columns)
    {
        EnsureNotBuilt();
        _orderBy = new SqlOrderBy(columns.ToList());
        return this;
    }

    // ---------------- BUILD ----------------
    public (string Sql, object Parameters) Build()
    {
        if (_built)
            return (_cachedSql!, _parameters);

        if (_from is null)
            throw new InvalidOperationException("FROM is required.");

        var compiler = new SqlCompiler();
        _cachedSql = compiler.Compile(this);

        _built = true;

        return (_cachedSql, _parameters);
    }

    public string ToSql() => Build().Sql;

    // ---------------- INTERNAL ACCESS ----------------
    internal SqlSelect? SelectNode => _select;
    internal SqlFrom FromNode => _from!;
    internal IReadOnlyList<SqlJoin> Joins => _joins;
    internal IReadOnlyList<SqlNode> WhereNodes => _where;
    internal SqlGroupBy? GroupByNode => _groupBy;
    internal SqlOrderBy? OrderByNode => _orderBy;

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