using System.Linq.Expressions;

namespace DataToolkit.Library.Fluent
{
    public interface IFluentQuery
    {
        (string Sql, object Parameters) Build();
        IFluentQuery From(params string[] tables);
        IFluentQuery FullJoin(string table, Expression<Func<bool>> on);
        IFluentQuery GroupBy(params string[] columns);
        IFluentQuery InnerJoin(string table, Expression<Func<bool>> on);
        IFluentQuery LeftJoin(string table, Expression<Func<bool>> on);
        IFluentQuery OrderBy(params string[] columns);
        IFluentQuery RightJoin(string table, Expression<Func<bool>> on);
        IFluentQuery Select(params string[] columns);
        string ToSql();
        IFluentQuery Where(string sql, object? parameters = null);
        IFluentQuery Where<T>(Expression<Func<T, bool>> expr);
        IFluentQuery WhereIf(bool condition, string sql, object? parameters = null);
    }
}