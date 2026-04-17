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
    {
        return ExecuteSafe(() =>
        {
            using var cmd = BuildCommand(procedure, parameters);
            var ds = new DataSet();
            var adapter = CreateAdapter(cmd);
            adapter.Fill(ds);
            return ds;
        }, procedure);
    }

    public Task<DataSet> ExecuteDataSetAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        return ExecuteSafeAsync(() =>
            Task.Run(() => ExecuteDataSet(procedure, parameters)),
            procedure);
    }

    public DataTable ExecuteDataTable(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        return ExecuteSafe(() =>
            ExecuteDataSet(procedure, parameters).Tables[0],
            procedure);
    }

    public async Task<DataTable> ExecuteDataTableAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var ds = await ExecuteDataSetAsync(procedure, parameters);
            return ds.Tables[0];
        }, procedure);
    }

    // ---------------- BUILD COMMAND ----------------

    private IDbCommand BuildCommand(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = procedure;
        cmd.CommandType = CommandType.StoredProcedure;

        if (Tx != null)
            cmd.Transaction = Tx;

        if (parameters != null)
        {
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
        }

        return cmd;
    }

    private static DbDataAdapter CreateAdapter(IDbCommand cmd)
    {
        return cmd switch
        {
            SqlCommand sql => new SqlDataAdapter(sql),
            AseCommand ase => new AseDataAdapter(ase),
            _ => throw new NotSupportedException("Comando no soportado.")
        };
    }

    // ---------------- SAFE ----------------

    private T ExecuteSafe<T>(Func<T> func, string procedure)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Error SP '{Procedure}'",
                procedure);

            throw new SqlExecutorException(
                $"Error SP '{procedure}'",
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
            _logger.Error(ex,
                "Error async SP '{Procedure}'",
                procedure);

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