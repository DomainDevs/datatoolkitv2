using DataToolkit.Library.Common;

namespace DataToolkit.Library.Fluent
{
    public interface IFluentQuery
    {
        IFluentQuery And(string condition);
        Task<IEnumerable<T>> ExecuteAsync<T>();
        Task<IEnumerable<T>> ExecuteMultiMapAsync<T>(MultiMapRequest request);
        Task<List<IEnumerable<dynamic>>> ExecuteMultipleAsync();
        IFluentQuery From(string table);
        IFluentQuery FullJoin(string table, string on);
        IFluentQuery GroupBy(params string[] columns);
        IFluentQuery InnerJoin(string table, string on);
        IFluentQuery LeftJoin(string table, string on);
        IFluentQuery Or(string condition);
        IFluentQuery OrderBy(params string[] columns);
        IFluentQuery Params(object parameters);
        IFluentQuery RightJoin(string table, string on);
        IFluentQuery Select(params string[] columns);
        IFluentQuery Timeout(int seconds);
        IFluentQuery Where(string condition);
        string ToSql();
    }
}