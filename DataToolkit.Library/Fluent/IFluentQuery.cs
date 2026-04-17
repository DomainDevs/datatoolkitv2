namespace DataToolkit.Library.Fluent
{
    public interface IFluentQuery
    {
        (string Sql, object Parameters) Build();
        IFluentQuery From(string table);
        IFluentQuery Select(params string[] columns);
        string ToSql();
        IFluentQuery Where(string condition, object? parameters = null);
        IFluentQuery Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> expr);
    }
}