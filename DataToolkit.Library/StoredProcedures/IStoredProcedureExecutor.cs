using System.Data;

namespace DataToolkit.Library.StoredProcedures;

public interface IStoredProcedureExecutor
{
    DataSet ExecuteDataSet(string procedure, IEnumerable<IDbDataParameter> parameters);
    Task<DataSet> ExecuteDataSetAsync(string procedure, IEnumerable<IDbDataParameter> parameters);
    DataTable ExecuteDataTable(string procedure, IEnumerable<IDbDataParameter> parameters);
    Task<DataTable> ExecuteDataTableAsync(string procedure, IEnumerable<IDbDataParameter> parameters);
}
