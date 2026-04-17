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

    // =========================================================
    // CONSTRUCTOR LEGACY (COMPATIBLE)
    // =========================================================
    public StoredProcedureExecutor(
        IDbConnection connection,
        IDbTransaction? transaction = null,
        int? defaultTimeout = null,
        ILogger? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        _transactionProvider = () => transaction;

        _defaultTimeout = defaultTimeout;
        _logger = logger ?? Log.Logger;
    }

    // =========================================================
    // CONSTRUCTOR MODERNO (RECOMENDADO)
    // =========================================================
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

    // =========================================================
    // CORE
    // =========================================================

    private IDbTransaction? Tx => _transactionProvider?.Invoke();

    private void Validate(string procedure)
    {
        if (string.IsNullOrWhiteSpace(procedure))
            throw new ArgumentException("Procedure cannot be null or empty.");
    }

    // =========================================================
    // EXECUTE DATASET
    // =========================================================

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

    // =========================================================
    // EXECUTE DATATABLE
    // =========================================================

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

    // =========================================================
    // COMMAND BUILDER
    // =========================================================

    private IDbCommand BuildCommand(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        Validate(procedure);

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

    // =========================================================
    // SAFE WRAPPERS (ALINEADOS CON SQL EXECUTOR)
    // =========================================================

    private T ExecuteSafe<T>(Func<T> func, string procedure)
    {
        try
        {
            Validate(procedure);
            return func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "StoredProcedure execution error: {Procedure}",
                procedure);

            throw new SqlExecutorException(
                $"Error al ejecutar procedimiento '{procedure}'",
                ex,
                procedure);
        }
    }

    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string procedure)
    {
        try
        {
            Validate(procedure);
            return await func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "StoredProcedure async execution error: {Procedure}",
                procedure);

            throw new SqlExecutorException(
                $"Error asincrónico al ejecutar procedimiento '{procedure}'",
                ex,
                procedure);
        }
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}