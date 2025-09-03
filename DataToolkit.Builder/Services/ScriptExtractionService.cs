using DataToolkit.Builder.Helpers;
using DataToolkit.Builder.Models;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using Microsoft.AspNetCore.Connections;
using Microsoft.OpenApi.Expressions;
using System.Collections.Generic;
using System.Text;


namespace DataToolkit.Builder.Services;

public class ScriptExtractionService
{
    private readonly ISqlConnectionManager _connectionManager;


    public ScriptExtractionService(ISqlConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task<string?> GetCreateScriptAsync(string objectName, string objectType, string objectSchema = "dbo", DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        if (provider == DatabaseProvider.SqlServer)
        {
            return await GetScriptSqlServerAsync(objectName, objectType, objectSchema);
        }
        else if (provider == DatabaseProvider.Sybase)
        {
            return await GetScriptSybaseAsync(objectName, objectType, objectSchema);
        }

        return null;
    }

    private async Task<string?> GetScriptSqlServerAsync(string name, string objectType, string objectSchema = "dbo")
    {
        DBObject? vDbStored = new DBObject();
        if (objectType.ToUpper() == "P" || objectType.ToUpper() == "V" || objectType.ToUpper() == "TR")
        {
            vDbStored = await GetBodySqlServerAsync(name, objectType); //BODY
            if (!vDbStored.ScriptSQL.Equals(string.Empty))
            {
                vDbStored.ScriptSQL = ScriptUtils.ReplaceCreateHeader(vDbStored.ScriptSQL, vDbStored.Name, vDbStored.Schema, vDbStored.ObjectType);
                vDbStored.ScriptSQL = ScriptUtils.SetHeaderScript(vDbStored.Schema, vDbStored.Name, vDbStored.ObjectType) + vDbStored.ScriptSQL;
                vDbStored.ScriptSQL = vDbStored.ScriptSQL + "\r\n" + ScriptUtils.SetFooterScript(vDbStored.Schema, vDbStored.Name, vDbStored.ObjectType);
                return await AppendPermissionsToScriptAsync(vDbStored.ScriptSQL, vDbStored.Name, vDbStored.Schema, objectType);
            }
        }
        else if (objectType.ToUpper() == "U")
        {
            vDbStored = await GetScriptSqlServerTableAsync(name);
            if (!vDbStored.ScriptSQL.Equals(string.Empty))
            {
                vDbStored.ScriptSQL = ScriptUtils.SetHeaderScript(vDbStored.Schema, vDbStored.Name, objectType) + vDbStored.ScriptSQL;
                vDbStored.ScriptSQL = vDbStored.ScriptSQL + "\r\n" + ScriptUtils.SetFooterScript(vDbStored.Schema, vDbStored.Name, objectType);
                return await AppendPermissionsToScriptAsync(vDbStored.ScriptSQL, vDbStored.Name, vDbStored.Schema, objectType);
            }
        }
            return "";
    }


    private async Task<DBObject?> GetBodySqlServerAsync(string name, string objectType)
    {
        var query = @"
        SELECT 
            s.name AS SchemaName,
            o.name AS ObjectName,
            sm.definition
        FROM sys.objects o
        INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
        INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
        WHERE o.name = @name AND o.type = @objectType";

        if (!_connectionManager.IsConnected())
            return null;

        using (var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection()))
        {
            var result = await _sqlExecutor.FromSqlAsync<(string SchemaName, string ObjectName, string Definition)>(query, new { name, objectType });
            var first = result.FirstOrDefault();
            if (first == default) return null;

            return new DBObject
            {
                Name = first.ObjectName,
                Schema = first.SchemaName,
                ObjectType = objectType,
                ScriptSQL = first.Definition + "GO",
                Permissions = null,
            };
        }

    }

    private async Task<DBObject?> GetScriptSqlServerTableAsync(string tableName, string schema = "dbo")
    {
        var columnsSql = @"
        SELECT 
            c.name AS ColumnName,
            t.name AS DataType,
            c.max_length AS MaxLength,
            c.precision AS Precision,
            c.scale AS Scale,
            c.is_nullable AS IsNullable,
            c.is_identity AS IsIdentity
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        INNER JOIN sys.tables tbl ON c.object_id = tbl.object_id
        INNER JOIN sys.schemas s ON tbl.schema_id = s.schema_id
        WHERE tbl.name = @tableName AND s.name = @schema
        ORDER BY c.column_id;";

        var pkSql = @"
        SELECT c.name AS ColumnName
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
        WHERE i.is_primary_key = 1 AND OBJECT_NAME(i.object_id) = @tableName;";

        var fkSql = @"
        SELECT 
            fk.name AS FK_Name,
            cp.name AS ParentColumn,
            tr.name AS ReferencedTable,
            cr.name AS ReferencedColumn
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
        INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND cp.object_id = tp.object_id
        INNER JOIN sys.tables tr ON tr.object_id = fk.referenced_object_id
        INNER JOIN sys.columns cr ON cr.column_id = fkc.referenced_column_id AND cr.object_id = tr.object_id
        WHERE tp.name = @tableName;";

        var uniqueSql = @"
        SELECT c.name AS ColumnName
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
        WHERE i.is_unique = 1 AND i.is_primary_key = 0 AND OBJECT_NAME(i.object_id) = @tableName;";

        if (!_connectionManager.IsConnected())
            return null;

        var sb = new StringBuilder();
        var columnLines = new List<string>();

        using (var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection()))
        {
            IEnumerable<ColumnDefinitionResult> columns = await _sqlExecutor.FromSqlAsync<ColumnDefinitionResult>(columnsSql, new { tableName, schema });
            var primaryKeys = (await _sqlExecutor.FromSqlAsync<ColumnNameResult>(pkSql, new { tableName }))
                              .Select(x => x.ColumnName).ToList();
            var foreignKeys = await _sqlExecutor.FromSqlAsync<ForeignKeyResult>(fkSql, new { tableName });
            var uniqueKeys = (await _sqlExecutor.FromSqlAsync<ColumnNameResult>(uniqueSql, new { tableName }))
                             .Select(x => x.ColumnName).ToList();

            if (!columns.Any())
                return null;


            var fullTableName = $"[{schema}].[{tableName}]";

            sb.AppendLine($"CREATE TABLE {fullTableName}");
            sb.AppendLine("(");

            foreach (var col in columns)
            {
                string type = col.DataType.ToUpper();
                string typeSpec = type switch
                {
                    "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" => $"{type}({(col.MaxLength == -1 ? "MAX" : col.MaxLength)})",
                    "DECIMAL" or "NUMERIC" => $"{type}({col.Precision},{col.Scale})",
                    _ => type
                };

                string identityClause = col.IsIdentity ? " IDENTITY(1,1)" : "";
                string nullable = col.IsNullable ? "NULL" : "NOT NULL";

                columnLines.Add($"    [{col.ColumnName}] {typeSpec}{identityClause} {nullable}");
            }

            if (primaryKeys.Any())
            {
                string pkCols = string.Join(", ", primaryKeys.Select(c => $"[{c}]"));
                columnLines.Add($"    CONSTRAINT [PK_{tableName}] PRIMARY KEY ({pkCols})");
            }

            foreach (var unique in uniqueKeys)
            {
                columnLines.Add($"    CONSTRAINT [UQ_{tableName}_{unique}] UNIQUE ([{unique}])");
            }

            foreach (var fk in foreignKeys)
            {
                columnLines.Add($"    CONSTRAINT [{fk.FK_Name}] FOREIGN KEY ([{fk.ParentColumn}]) REFERENCES [{fk.ReferencedTable}]([{fk.ReferencedColumn}])");
            }
        }

        sb.AppendLine(string.Join(",\n", columnLines));
        sb.AppendLine(")");
        sb.AppendLine("GO");

        return new DBObject
        {
            Name = tableName,
            Schema = schema,
            ObjectType = "",
            ScriptSQL = sb.ToString(),
            Permissions = null,
        };
    }

    private async Task<string?> GetScriptSybaseAsync(string name, string type, string objectSchema)
    {
        if (!_connectionManager.IsConnected())
            return null;

        using (var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection()))
        {
            //Para Sybase se usa syscomments (puede estar fragmentado)
            string sql = $"SELECT text FROM syscomments WHERE id = OBJECT_ID('{name}') ORDER BY colid";
            var result = await _sqlExecutor.FromSqlInterpolatedAsync<string>($"{sql}");
            return string.Join(Environment.NewLine, result);
        }
    }

    private async Task<string> AppendPermissionsToScriptAsync(string script, string objectName, string schema = "dbo", string type = "P")
    {
        var sql = @"
        SELECT  
            dp.state_desc AS PermissionState,
            dp.permission_name AS PermissionName,
            pr.name AS GranteeName
        FROM sys.database_permissions dp
        INNER JOIN sys.objects ob ON dp.major_id = ob.object_id
        INNER JOIN sys.database_principals pr ON dp.grantee_principal_id = pr.principal_id
        WHERE ob.name = @objectName AND ob.type = @type";

        if (!_connectionManager.IsConnected())
            return null;

        var sb = new StringBuilder();
        using (var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection()))
        {
            var permissions = await _sqlExecutor.FromSqlAsync<(string PermissionState, string PermissionName, string GranteeName)>(
            sql, new { objectName, type });

            if (!permissions.Any())
                return script;


            sb.AppendLine(script);
            foreach (var perm in permissions)
            {
                if (perm.PermissionState == "GRANT")
                {
                    sb.AppendLine($"GRANT {perm.PermissionName} ON [{schema}].[{objectName}] TO [{perm.GranteeName}];");
                }
                else if (perm.PermissionState == "DENY")
                {
                    sb.AppendLine($"DENY {perm.PermissionName} ON [{schema}].[{objectName}] TO [{perm.GranteeName}];");
                }
                // Puedes incluir REVOKE si deseas
            }
        }

        return sb.ToString()+"GO";
    }

    public async Task<DbTable?> ExtractTableMetadataAsync(string schema, string tableName)
    {
        if (!_connectionManager.IsConnected())
            return null;

        using var _sqlExecutor = new SqlExecutor(_connectionManager.GetConnection());

        var columnsSql = @"
        SELECT 
            c.name AS ColumnName,
            t.name AS DataType,
            c.max_length AS MaxLength,
            c.precision AS Precision,
            c.scale AS Scale,
            c.is_nullable AS IsNullable,
            c.is_identity AS IsIdentity,
            dc.definition AS DefaultValue,
            c.is_computed AS IsComputed,
            cc.definition AS ComputedDefinition,
            chk.definition AS CheckDefinition,
            (CASE WHEN i.is_primary_key = 1 THEN 1 ELSE 0 END) AS IsPrimaryKey,
            (SELECT COUNT(1) 
            FROM sys.key_constraints kc1
            INNER JOIN sys.index_columns ic1 
            ON kc1.parent_object_id = ic1.object_id 
            AND kc1.unique_index_id = ic1.index_id 
            AND kc1.type = 'PK'
            WHERE kc1.parent_object_id = c.object_id
            ) AS PrimaryKeyCount
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        INNER JOIN sys.tables tbl ON c.object_id = tbl.object_id
        INNER JOIN sys.schemas s ON tbl.schema_id = s.schema_id
        LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
        LEFT JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
        LEFT JOIN sys.check_constraints chk 
            ON chk.parent_object_id = c.object_id 
            AND chk.parent_column_id = c.column_id
        LEFT JOIN sys.index_columns ic
            ON ic.object_id = c.object_id 
            AND ic.column_id = c.column_id
        LEFT JOIN sys.indexes i
            ON i.object_id = ic.object_id 
            AND i.index_id = ic.index_id
        WHERE tbl.name = @tableName AND s.name = @schema
        ORDER BY c.column_id;";

        var columns = await _sqlExecutor.FromSqlAsync<ColumnDefinitionResult>(columnsSql, new { tableName, schema });

        if (!columns.Any())
            return null;

        var dbTable = new DbTable
        {
            Schema = schema,
            Name = tableName,
            Columns = columns.Select(c => new DbColumn
            {
                Name = c.ColumnName,
                SqlType = c.DataType,
                Length = c.MaxLength == -1 ? null : (int?)c.MaxLength,
                Precision = c.Precision,
                Scale = c.Scale,
                IsNullable = c.IsNullable,
                IsIdentity = c.IsIdentity,
                DefaultValue = c.DefaultValue,
                IsComputed = c.IsComputed,
                ComputedDefinition = c.ComputedDefinition,
                HasCheckConstraint = !string.IsNullOrWhiteSpace(c.CheckDefinition),
                CheckDefinition = c.CheckDefinition,
                IsPrimaryKey = c.IsPrimaryKey == 1  // <--- aquí
            }).ToList()
        };


        // 🚀 Aquí se asigna el PrimaryKeyCount a nivel tabla
        dbTable.PrimaryKeyCount = dbTable.Columns.Count(c => c.IsPrimaryKey);

        return dbTable;
    }


}

public class ColumnDefinitionResult
{
    public string ColumnName { get; set; } = "";
    public string DataType { get; set; } = "";
    public short MaxLength { get; set; }
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }

    // 👇 Nuevos campos
    public string? DefaultValue { get; set; }
    public bool IsComputed { get; set; }
    public string? ComputedDefinition { get; set; }
    public string? CheckDefinition { get; set; }

    // 👇 Campo para la PK
    public int IsPrimaryKey { get; set; }  // 1 = PK, 0 = no PK
}

public class ColumnNameResult
{
    public string ColumnName { get; set; } = "";
}

public class ForeignKeyResult
{
    public string FK_Name { get; set; } = "";
    public string ParentColumn { get; set; } = "";
    public string ReferencedTable { get; set; } = "";
    public string ReferencedColumn { get; set; } = "";
}