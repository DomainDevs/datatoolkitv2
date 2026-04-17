namespace DataToolkit.Library.Fluent
{
    public interface IFluentQuery
    {
        IFluentQuery And(string condition, object? parameters = null);
        (string Sql, object Parameters) Build();
        IFluentQuery From(string table);
        IFluentQuery FullJoin(string table, string on);
        IFluentQuery GroupBy(params string[] columns);
        IFluentQuery InnerJoin(string table, string on);
        IFluentQuery LeftJoin(string table, string on);
        IFluentQuery Or(string condition, object? parameters = null);
        IFluentQuery OrderBy(params string[] columns);
        IFluentQuery RightJoin(string table, string on);
        IFluentQuery Select(params string[] columns);
        string ToSql();
        IFluentQuery Where(string condition, object? parameters = null);
        IFluentQuery WhereIf(bool condition, string sqlCondition, object? parameters = null);
    }
}