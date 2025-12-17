using DataToolkit.Library.Common;
using DataToolkit.Library.Sql;
using System.Text;

namespace DataToolkit.Library.Fluent
{
    public sealed class FluentQuery : IFluentQuery
    {
        private readonly ISqlExecutor _executor;
        private readonly StringBuilder _sql = new();
        private object? _params;
        private int? _timeout;

        private bool _hasWhere;

        public FluentQuery(ISqlExecutor executor)
        {
            _executor = executor;
        }

        // ------------------------------------------------------
        // SELECT / FROM
        // ------------------------------------------------------
        public IFluentQuery Select(params string[] columns)
        {
            _sql.Append("SELECT ")
                .Append(string.Join(", ", columns))
                .Append(" ");
            return this;
        }

        public IFluentQuery From(string table)
        {
            _sql.Append("FROM ")
                .Append(table)
                .Append(" ");
            return this;
        }

        // ------------------------------------------------------
        // JOINS
        // ------------------------------------------------------
        public IFluentQuery InnerJoin(string table, string on)
        {
            _sql.Append("INNER JOIN ")
                .Append(table)
                .Append(" ON ")
                .Append(on)
                .Append(" ");
            return this;
        }

        public IFluentQuery LeftJoin(string table, string on)
        {
            _sql.Append("LEFT JOIN ")
                .Append(table)
                .Append(" ON ")
                .Append(on)
                .Append(" ");
            return this;
        }

        public IFluentQuery RightJoin(string table, string on)
        {
            _sql.Append("RIGHT JOIN ")
                .Append(table)
                .Append(" ON ")
                .Append(on)
                .Append(" ");
            return this;
        }

        public IFluentQuery FullJoin(string table, string on)
        {
            _sql.Append("FULL JOIN ")
                .Append(table)
                .Append(" ON ")
                .Append(on)
                .Append(" ");
            return this;
        }

        // ------------------------------------------------------
        // WHERE / AND / OR
        // ------------------------------------------------------
        public IFluentQuery Where(string condition)
        {
            _sql.Append("WHERE ")
                .Append(condition)
                .Append(" ");
            _hasWhere = true;
            return this;
        }

        public IFluentQuery And(string condition)
        {
            _sql.Append(_hasWhere ? "AND " : "WHERE ")
                .Append(condition)
                .Append(" ");
            _hasWhere = true;
            return this;
        }

        public IFluentQuery Or(string condition)
        {
            _sql.Append("OR ")
                .Append(condition)
                .Append(" ");
            return this;
        }

        // ------------------------------------------------------
        // GROUP BY / ORDER BY
        // ------------------------------------------------------
        public IFluentQuery GroupBy(params string[] columns)
        {
            _sql.Append("GROUP BY ")
                .Append(string.Join(", ", columns))
                .Append(" ");
            return this;
        }

        public IFluentQuery OrderBy(params string[] columns)
        {
            _sql.Append("ORDER BY ")
                .Append(string.Join(", ", columns))
                .Append(" ");
            return this;
        }

        // ------------------------------------------------------
        // PARAMS / TIMEOUT
        // ------------------------------------------------------
        public IFluentQuery Params(object parameters)
        {
            _params = parameters;
            return this;
        }

        public IFluentQuery Timeout(int seconds)
        {
            _timeout = seconds;
            return this;
        }

        // ------------------------------------------------------
        // Devolver el SQl, para ejecuciones multimap
        // ------------------------------------------------------
        public string ToSql()
        {
            return _sql.ToString();
        }

        // ------------------------------------------------------
        // EXECUTION STRATEGIES
        // ------------------------------------------------------
        public Task<IEnumerable<T>> ExecuteAsync<T>()
        {
            return _executor.FromSqlAsync<T>(_sql.ToString(), _params, _timeout);
        }

        public Task<IEnumerable<T>> ExecuteMultiMapAsync<T>(MultiMapRequest request)
        {
            return _executor.FromSqlMultiMapAsync<T>(request);
        }

        public Task<List<IEnumerable<dynamic>>> ExecuteMultipleAsync()
        {
            return _executor.QueryMultipleAsync(_sql.ToString(), _params);
        }
    }
}
