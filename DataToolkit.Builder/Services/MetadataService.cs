using DataToolkit.Builder.Models;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;

namespace DataToolkit.Builder.Services;

//Inyección de dependencias incompleta
//Las clases ScriptExtractionService y MetadataService no están interfaces.Esto impide el mocking en pruebas unitarias.

public class MetadataService
{
    /*
    //antigua ejecución
    private readonly SqlExecutor _sqlExecutor; 

    public MetadataService(SqlExecutor sqlExecutor)
    {
        _sqlExecutor = sqlExecutor;
    }
    */
    private readonly ISqlConnectionManager _connectionManager;

    public MetadataService(ISqlConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task<DbGroupResult> GetDatabaseObjectsAsync(DatabaseProvider provider)
    {
        var result = new DbGroupResult();

        //if (!_connectionManager.IsConnected())
        //    return BadRequest("Conexión no activa.");

        //using var executor = new SqlExecutor(_connectionManager.GetConnection());
        //var result = executor.FromSql<dynamic>("SELECT name FROM sys.databases");
        //return Ok(result);


        if (provider == DatabaseProvider.SqlServer)
        {
            if (!_connectionManager.IsConnected())
                return result;

            using (var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection())){
            
                result.Tables = (await _sqlExecutor.FromSqlInterpolatedAsync<DbObjectRef>(
                    $"SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name] FROM INFORMATION_SCHEMA.TABLES", commandTimeout: 60))
                    .ToList();

                result.Views = (await _sqlExecutor.FromSqlInterpolatedAsync<DbObjectRef>(
                    $"SELECT TABLE_SCHEMA AS [schema], TABLE_NAME AS [Name] FROM INFORMATION_SCHEMA.VIEWS"))
                    .ToList();

                result.Procedures = (await _sqlExecutor.FromSqlAsync<DbObjectRef>(
                    @"SELECT s.name AS [Schema], p.name AS [Name]
                  FROM sys.procedures p 
                  JOIN sys.schemas s ON p.schema_id = s.schema_id"))
                    .ToList();

                result.Triggers = (await _sqlExecutor.FromSqlAsync<DbObjectRef>(
                    @"SELECT s.name AS [Schema], t.name AS [Name]
                    FROM sys.objects t 
                    JOIN sys.schemas s 
                    ON t.schema_id = s.schema_id
                    WHERE t.type = 'TR'"))
                    .ToList();
            }
        }
        else if (provider == DatabaseProvider.Sybase)
        {
            // Esta parte la ajustaremos más adelante
            result.Tables = new();
            result.Views = new();
            result.Procedures = new();
            result.Triggers = new();
        }

        return result;
    }
}
