namespace DataToolkit.Library.Fluent
{
    public interface IFluentQuery
    {
        (string Sql, object Parameters) Build();
        IFluentQuery From(params string[] tables);
        IFluentQuery FullJoin(string table, string on);
        IFluentQuery GroupBy(params string[] columns);
        IFluentQuery InnerJoin(string table, string on);
        IFluentQuery LeftJoin(string table, string on);
        IFluentQuery OrderBy(params string[] columns);
        IFluentQuery RightJoin(string table, string on);
        IFluentQuery Select(params string[] columns);
        string ToSql();
        IFluentQuery Where(string sql, object? parameters = null);
        IFluentQuery WhereIf(bool condition, string sql, object? parameters = null);
    }
}