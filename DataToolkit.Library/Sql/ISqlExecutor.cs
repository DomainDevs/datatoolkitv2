using Dapper;
using DataToolkit.Library.Common;
using System.Data;

namespace DataToolkit.Library.Sql
{
    public interface ISqlExecutor
    {
        void Dispose();
        int Execute(string sql, object? parameters = null, int? commandTimeout = null);
        Task<int> ExecuteAsync(string sql, object? parameters = null, int? commandTimeout = null);
        (int RowsAffected, Dictionary<string, object> OutputValues) ExecuteWithOutput(string storedProcedure, Action<DynamicParameters> configureParameters, int? commandTimeout = null);
        Task<(int RowsAffected, DynamicParameters Output)> ExecuteWithOutputAsync(string storedProcedure, Action<DynamicParameters> configureParameters, int? commandTimeout = null);
        IEnumerable<T> FromSql<T>(string sql, object? parameters = null, int? commandTimeout = null);
        Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters = null, int? commandTimeout = null);
        IEnumerable<T> FromSqlInterpolated<T>(FormattableString query, int? commandTimeout = null);
        Task<IEnumerable<T>> FromSqlInterpolatedAsync<T>(FormattableString query, int? commandTimeout = null);
        IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest request, int? commandTimeout = null);
        Task<IEnumerable<T>> FromSqlMultiMapAsync<T>(MultiMapRequest request, int? commandTimeout = null);
        Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(string sql, object? parameters = null, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null);
    }
}