using AdoNetCore.AseClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using DataToolkit.Library.Common;
using Serilog;

namespace DataToolkit.Library.StoredProcedures;

public class StoredProcedureExecutor : IStoredProcedureExecutor, IDisposable
{
    private readonly IDbConnection _connection;
    private readonly Func<IDbTransaction?> _transactionProvider;
    private readonly int? _defaultTimeout;
    private readonly ILogger _logger;

    private bool _disposed;

    public StoredProcedureExecutor(
        IDbConnection connection,
        Func<IDbTransaction?> transactionProvider,
        int? defaultTimeout = null,
        ILogger? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transactionProvider = transactionProvider ?? (() => null);
        _defaultTimeout = defaultTimeout;
        _logger = logger ?? Log.Logger;
    }

    private IDbTransaction? Tx => _transactionProvider();

    // ---------------- CORE ----------------

    public DataSet ExecuteDataSet(string procedure, IEnumerable<IDbDataParameter> parameters)
        => ExecuteSafe(() =>
        {
            using var cmd = BuildCommand(procedure, parameters);
            var ds = new DataSet();
            var adapter = CreateAdapter(cmd);
            adapter.Fill(ds);
            return ds;
        }, procedure);

    public Task<DataSet> ExecuteDataSetAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
        => ExecuteSafeAsync(() =>
            Task.Run(() => ExecuteDataSet(procedure, parameters)),
            procedure);

    public DataTable ExecuteDataTable(string procedure, IEnumerable<IDbDataParameter> parameters)
        => ExecuteSafe(() =>
        {
            var ds = ExecuteDataSet(procedure, parameters);
            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }, procedure);

    public Task<DataTable> ExecuteDataTableAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
        => ExecuteSafeAsync(async () =>
        {
            var ds = await ExecuteDataSetAsync(procedure, parameters);
            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }, procedure);

    // ---------------- COMMAND ----------------

    private IDbCommand BuildCommand(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = procedure;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Transaction = Tx;

        if (_defaultTimeout.HasValue)
            cmd.CommandTimeout = _defaultTimeout.Value;

        if (parameters != null)
        {
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
        }

        return cmd;
    }

    private static DbDataAdapter CreateAdapter(IDbCommand cmd)
        => cmd switch
        {
            SqlCommand sql => new SqlDataAdapter(sql),
            AseCommand ase => new AseDataAdapter(ase),
            _ => throw new NotSupportedException("Comando no soportado.")
        };

    // ---------------- SAFE WRAPPERS ----------------

    private T ExecuteSafe<T>(Func<T> func, string procedure)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "StoredProcedure error: {Procedure}", procedure);
            throw new SqlExecutorException(
                $"Error ejecutando SP '{procedure}'",
                ex,
                procedure);
        }
    }

    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string procedure)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "StoredProcedure async error: {Procedure}", procedure);
            throw new SqlExecutorException(
                $"Error async SP '{procedure}'",
                ex,
                procedure);
        }
    }

    // ---------------- DISPOSE ----------------

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}