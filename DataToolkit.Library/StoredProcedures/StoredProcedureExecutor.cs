using AdoNetCore.AseClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using DataToolkit.Library.Common;
using Serilog;

namespace DataToolkit.Library.StoredProcedures;

/// <summary>
/// Ejecuta procedimientos almacenados en SQL Server o Sybase y retorna resultados como DataSet o DataTable.
/// Maneja errores con logging usando Serilog y los encapsula en SqlExecutorException.
/// </summary>
public class StoredProcedureExecutor : IStoredProcedureExecutor, IDisposable
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly int? _defaultTimeout;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Inicializa una nueva instancia del ejecutor de procedimientos almacenados.
    /// </summary>
    public StoredProcedureExecutor(IDbConnection connection, IDbTransaction? transaction = null, int? defaultTimeout = null, ILogger logger = null)
    {
        //_connection = connection;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
        _defaultTimeout = defaultTimeout;
        _logger = logger ?? new LoggerConfiguration().CreateLogger(); //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta un procedimiento almacenado y retorna el resultado como un DataSet.
    /// </summary>
    public DataSet ExecuteDataSet(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        /*
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = procedure;
        cmd.CommandType = CommandType.StoredProcedure;

        if (_transaction != null)
            cmd.Transaction = _transaction;

        if (parameters != null)
        {
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
        }

        var ds = new DataSet();
        var adapter = CreateAdapter(cmd);
        adapter.Fill(ds);
        return ds;
        */
        return ExecuteSafe(() =>
        {
            using var cmd = BuildCommand(procedure, parameters);
            var ds = new DataSet();
            var adapter = CreateAdapter(cmd);
            adapter.Fill(ds);
            return ds;
        }, procedure);
    }

    /// <summary>
    /// Ejecuta un procedimiento almacenado de forma asincrónica y retorna un DataSet.
    /// </summary>
    public async Task<DataSet> ExecuteDataSetAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        // ADO.NET does not support async fill — wrap in Task.Run
        //return await Task.Run(() => ExecuteDataSet(procedure, parameters));
        return await ExecuteSafeAsync(() => Task.Run(() => ExecuteDataSet(procedure, parameters)), procedure);
    }

    /// <summary>
    /// Ejecuta un procedimiento almacenado y retorna solo la primera tabla del resultado.
    /// </summary>
    public DataTable ExecuteDataTable(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        //return ExecuteDataSet(procedure, parameters).Tables[0];
        return ExecuteSafe(() => ExecuteDataSet(procedure, parameters).Tables[0], procedure);
    }

    /// <summary>
    /// Ejecuta un procedimiento almacenado de forma asincrónica y retorna solo la primera tabla.
    /// </summary>
    public async Task<DataTable> ExecuteDataTableAsync(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        /*
        var ds = await ExecuteDataSetAsync(procedure, parameters);
        return ds.Tables[0];
        */
        return await ExecuteSafeAsync(async () =>
        {
            var ds = await ExecuteDataSetAsync(procedure, parameters);
            return ds.Tables[0];
        }, procedure);
    }

    /// <summary>
    /// Crea el adaptador adecuado según el tipo de comando (SQL Server o Sybase).
    /// </summary>
    private static DbDataAdapter CreateAdapter(IDbCommand cmd)
    {
        return cmd switch
        {
            SqlCommand sql => new SqlDataAdapter(sql),
            AseCommand ase => new AseDataAdapter(ase),
            _ => throw new NotSupportedException("Comando no soportado.")
        };
    }

    /// <summary>
    /// Construye un IDbCommand con el procedimiento y sus parámetros.
    /// </summary>
    private IDbCommand BuildCommand(string procedure, IEnumerable<IDbDataParameter> parameters)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = procedure;
        cmd.CommandType = CommandType.StoredProcedure;

        if (_transaction != null)
            cmd.Transaction = _transaction;

        if (parameters != null)
        {
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
        }

        return cmd;
    }

    /// <summary>
    /// Ejecuta una función envolviéndola en try/catch para capturar y registrar errores sincronizados.
    /// </summary>
    private T ExecuteSafe<T>(Func<T> func, string procedure)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al ejecutar el procedimiento almacenado '{Procedure}'", procedure);
            throw new SqlExecutorException($"Error al ejecutar procedimiento '{procedure}'", ex, procedure);
        }
    }

    /// <summary>
    /// Ejecuta una función asincrónica envolviéndola en try/catch para capturar y registrar errores.
    /// </summary>
    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string procedure)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error asincrónico al ejecutar el procedimiento almacenado '{Procedure}'", procedure);
            throw new SqlExecutorException($"Error asincrónico al ejecutar procedimiento '{procedure}'", ex, procedure);
        }
    }

    /// <summary>
    // 🔒 Método Dispose público
    /// Libera los recursos utilizados por la conexión y transacción.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Método protegido para liberar recursos de forma segura.
    /// 🛡️ Método protegido para herencia segura (evita más de un llamado)
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }
}