using System.Collections.Concurrent;
using System.Reflection;
using DataToolkit.Library.Fluent.Sql;
using DataToolkit.Library.Fluent.Compilation;
using DataToolkit.Library.Fluent.Parsing;

namespace DataToolkit.Library.Fluent;

public sealed class FluentQuery : IFluentQuery
{
    private readonly List<string> _select = new();
    private readonly HashSet<string> _selectSet = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<string> _from = new();
    private readonly List<string> _joins = new();

    private readonly List<SqlNode> _where = new();
    private readonly List<string> _groupBy = new();
    private readonly List<string> _orderBy = new();

    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _paramKeys = new(StringComparer.OrdinalIgnoreCase);

    private bool _built;
    private string? _cachedSql;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    // ---------------- SELECT ----------------
    public IFluentQuery Select(params string[] columns)
    {
        EnsureNotBuilt();

        foreach (var c in columns)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;

            if (_selectSet.Add(c))
                _select.Add(c);
        }

        return this;
    }

    public IFluentQuery From(string table)
    {
        EnsureNotBuilt();

        _from.Add(table);
        return this;
    }

    // ---------------- WHERE ----------------

    public IFluentQuery Where(string condition, object? parameters = null)
    {
        EnsureNotBuilt();

        MergeParameters(parameters);

        _where.Add(new SqlRaw(condition));

        return this;
    }

    public IFluentQuery Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> expr)
    {
        EnsureNotBuilt();

        var node = ExpressionParser.Parse(expr.Body);
        _where.Add(node);

        return this;
    }

    // ---------------- BUILD ----------------

    public (string Sql, object Parameters) Build()
    {
        if (_built)
            return (_cachedSql!, _parameters);

        var compiler = new SqlCompiler();

        _cachedSql = compiler.Compile(this);

        _built = true;

        return (_cachedSql, _parameters);
    }

    public string ToSql() => Build().Sql;

    // ---------------- INTERNAL ACCESS ----------------

    internal IReadOnlyList<string> Select => _select;
    internal IReadOnlyList<string> From => _from;
    internal IReadOnlyList<string> Joins => _joins;
    internal IReadOnlyList<SqlNode> WhereNodes => _where;
    internal IReadOnlyList<string> GroupBy => _groupBy;
    internal IReadOnlyList<string> OrderBy => _orderBy;

    // ---------------- PARAMS ----------------

    private void MergeParameters(object? parameters)
    {
        if (parameters is null) return;

        var type = parameters.GetType();
        var props = _propertyCache.GetOrAdd(type, t => t.GetProperties());

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