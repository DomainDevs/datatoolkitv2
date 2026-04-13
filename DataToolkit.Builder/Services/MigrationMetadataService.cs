using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DataToolkit.Builder.Services
{
    public class MigrationMetadataService
    {
        /// <summary>
        /// Extrae metadata de tablas y columnas usando la conexión activa.
        /// </summary>
        public async Task<List<TableMetadata>> ExtractMetadataAsync(ISqlConnectionManager connectionManager)
        {
            var metadata = new List<TableMetadata>();

            if (!connectionManager.IsConnected())
                return metadata;

            using var conn = connectionManager.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    CASE   
      WHEN c.max_length = -1 THEN 'MAX'
      WHEN ty.name IN ('nvarchar','nchar') THEN CAST(c.max_length / 2 AS VARCHAR(10))  
      ELSE CAST(c.max_length AS VARCHAR(10)) 
    END AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable,
    CASE WHEN c.is_identity = 1 THEN 'YES' ELSE 'NO' END AS IsIdentity,
    CASE WHEN c.is_computed = 1 THEN 'YES' ELSE 'NO' END AS IsComputed,
    c.collation_name AS Collation,
    dc.definition AS DefaultValue,
    CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS IsPrimaryKey,
    pk.constraint_name AS PrimaryKeyName,
    rt.name AS ForeignTable,
    rc.name AS ForeignColumn,
    fkref.name AS ForeignKeyName,
    fkref.delete_referential_action_desc AS FK_DeleteAction,
    fkref.update_referential_action_desc AS FK_UpdateAction,
    fkref.is_disabled AS FK_IsDisabled,
    fkref.is_not_trusted AS FK_IsNotTrusted
FROM sys.schemas s
INNER JOIN sys.tables t ON t.schema_id = s.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id, i.name AS constraint_name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic 
        ON i.object_id = ic.object_id 
        AND i.index_id = ic.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
LEFT JOIN sys.foreign_key_columns fk ON fk.parent_object_id = c.object_id AND fk.parent_column_id = c.column_id
LEFT JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
LEFT JOIN sys.columns rc ON fk.referenced_object_id = rc.object_id AND fk.referenced_column_id = rc.column_id
LEFT JOIN sys.foreign_keys fkref ON fk.constraint_object_id = fkref.object_id
ORDER BY s.name, t.name, c.column_id;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = metadata.Find(m => m.Schema == reader["SchemaName"].ToString()
                                             && m.Name == reader["TableName"].ToString());

                if (table == null)
                {
                    table = new TableMetadata
                    {
                        Schema = reader["SchemaName"].ToString() ?? "",
                        Name = reader["TableName"].ToString() ?? "",
                        Columns = new List<ColumnMetadata>()
                    };
                    metadata.Add(table);
                }

                table.Columns.Add(new ColumnMetadata
                {
                    Name = reader["ColumnName"].ToString() ?? "",
                    SqlType = reader["DataType"].ToString() ?? "",
                    MaxLength = reader["MaxLength"].ToString(),
                    Precision = reader["Precision"].ToString(),
                    Scale = reader["Scale"].ToString(),
                    IsNullable = reader["IsNullable"].ToString() == "YES",
                    IsIdentity = reader["IsIdentity"].ToString() == "YES",
                    IsComputed = reader["IsComputed"].ToString() == "YES",
                    Collation = reader["Collation"].ToString(),
                    DefaultValue = reader["DefaultValue"].ToString(),
                    IsPrimaryKey = reader["IsPrimaryKey"].ToString() == "YES",
                    PrimaryKeyName = reader["PrimaryKeyName"].ToString(),
                    ForeignTable = reader["ForeignTable"].ToString(),
                    ForeignColumn = reader["ForeignColumn"].ToString(),
                    ForeignKeyName = reader["ForeignKeyName"].ToString(),
                    FK_DeleteAction = reader["FK_DeleteAction"].ToString(),
                    FK_UpdateAction = reader["FK_UpdateAction"].ToString(),
                    FK_IsDisabled = reader["FK_IsDisabled"].ToString() == "1",
                    FK_IsNotTrusted = reader["FK_IsNotTrusted"].ToString() == "1"
                });
            }

            return metadata;
        }

        /// <summary>
        /// Compara metadata y devuelve solo las diferencias.
        /// </summary>
        public List<MetadataDifference> CompareMetadata(
            List<TableMetadata> sourceMetadata,
            List<TableMetadata> targetMetadata)
        {
            var differences = new List<MetadataDifference>();

            foreach (var sourceTable in sourceMetadata)
            {
                var targetTable = targetMetadata
                    .FirstOrDefault(t => t.Schema == sourceTable.Schema && t.Name == sourceTable.Name);

                if (targetTable == null)
                {
                    differences.Add(new MetadataDifference
                    {
                        Schema = sourceTable.Schema,
                        Table = sourceTable.Name,
                        DifferenceType = "Tabla faltante en destino"
                    });
                    continue;
                }

                foreach (var sourceColumn in sourceTable.Columns)
                {
                    var targetColumn = targetTable.Columns
                        .FirstOrDefault(c => c.Name == sourceColumn.Name);

                    if (targetColumn == null)
                    {
                        differences.Add(new MetadataDifference
                        {
                            Schema = sourceTable.Schema,
                            Table = sourceTable.Name,
                            Column = sourceColumn.Name,
                            DifferenceType = "Columna faltante en destino"
                        });
                        continue;
                    }

                    // Comparaciones campo a campo
                    if (sourceColumn.SqlType != targetColumn.SqlType)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "Tipo de dato distinto", sourceColumn.SqlType, targetColumn.SqlType));

                    if (sourceColumn.MaxLength != targetColumn.MaxLength)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "MaxLength distinto", sourceColumn.MaxLength, targetColumn.MaxLength));

                    if (sourceColumn.IsNullable != targetColumn.IsNullable)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "Nullable distinto", sourceColumn.IsNullable.ToString(), targetColumn.IsNullable.ToString()));

                    if (sourceColumn.IsIdentity != targetColumn.IsIdentity)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "Identity distinto", sourceColumn.IsIdentity.ToString(), targetColumn.IsIdentity.ToString()));

                    if (sourceColumn.IsPrimaryKey != targetColumn.IsPrimaryKey)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "PK distinto", sourceColumn.IsPrimaryKey.ToString(), targetColumn.IsPrimaryKey.ToString()));

                    if (sourceColumn.ForeignTable != targetColumn.ForeignTable ||
                        sourceColumn.ForeignColumn != targetColumn.ForeignColumn)
                        differences.Add(CreateDifference(sourceTable, sourceColumn, "FK distinto",
                            $"{sourceColumn.ForeignTable}.{sourceColumn.ForeignColumn}",
                            $"{targetColumn.ForeignTable}.{targetColumn.ForeignColumn}"));
                }
            }

            // Detectar tablas extra en destino
            foreach (var targetTable in targetMetadata)
            {
                var sourceTable = sourceMetadata
                    .FirstOrDefault(t => t.Schema == targetTable.Schema && t.Name == targetTable.Name);
                if (sourceTable == null)
                {
                    differences.Add(new MetadataDifference
                    {
                        Schema = targetTable.Schema,
                        Table = targetTable.Name,
                        DifferenceType = "Tabla extra en destino"
                    });
                }
            }

            return differences;
        }

        private MetadataDifference CreateDifference(TableMetadata table, ColumnMetadata column, string type, string sourceVal, string targetVal)
        {
            return new MetadataDifference
            {
                Schema = table.Schema,
                Table = table.Name,
                Column = column.Name,
                DifferenceType = type,
                SourceValue = sourceVal,
                TargetValue = targetVal
            };
        }

        /// <summary>
        /// Genera inventario completo de tablas y columnas (con diferencias si las hay).
        /// </summary>
        public List<TableInventory> BuildFullInventory(List<TableMetadata> sourceMetadata, List<TableMetadata> targetMetadata)
        {
            var report = new List<TableInventory>();

            var allTables = sourceMetadata.Select(t => t.Name)
                .Union(targetMetadata.Select(t => t.Name));

            foreach (var tableName in allTables)
            {
                var sourceTable = sourceMetadata.FirstOrDefault(t => t.Name == tableName);
                var targetTable = targetMetadata.FirstOrDefault(t => t.Name == tableName);

                var tableReport = new TableInventory
                {
                    Schema = sourceTable?.Schema ?? targetTable?.Schema ?? "",
                    Name = tableName,
                    Columns = new List<ColumnInventory>()
                };

                var allColumns = (sourceTable?.Columns.Select(c => c.Name) ?? new List<string>())
                    .Union(targetTable?.Columns.Select(c => c.Name) ?? new List<string>());

                foreach (var colName in allColumns)
                {
                    var sourceCol = sourceTable?.Columns.FirstOrDefault(c => c.Name == colName);
                    var targetCol = targetTable?.Columns.FirstOrDefault(c => c.Name == colName);

                    tableReport.Columns.Add(new ColumnInventory
                    {
                        Name = colName,
                        SourceValue = sourceCol,
                        TargetValue = targetCol,
                        DifferenceType = CompareColumn(sourceCol, targetCol)
                    });
                }

                report.Add(tableReport);
            }

            return report;
        }

        private string CompareColumn(ColumnMetadata? source, ColumnMetadata? target)
        {
            if (source == null) return "Columna faltante en origen";
            if (target == null) return "Columna faltante en destino";

            if (source.SqlType != target.SqlType) return "Tipo de dato distinto";
            if (source.MaxLength != target.MaxLength) return "MaxLength distinto";
            if (source.IsNullable != target.IsNullable) return "Nullable distinto";
            if (source.IsIdentity != target.IsIdentity) return "Identity distinto";
            if (source.IsPrimaryKey != target.IsPrimaryKey) return "PK distinto";
            if (source.ForeignTable != target.ForeignTable || source.ForeignColumn != target.ForeignColumn) return "FK distinto";

            return ""; // sin diferencias
        }
    }

    #region Metadata Models
    public class TableMetadata
    {
        public string Schema { get; set; } = "";
        public string Name { get; set; } = "";
        public List<ColumnMetadata> Columns { get; set; } = new();
    }

    public class ColumnMetadata
    {
        public string Name { get; set; } = "";
        public string SqlType { get; set; } = "";
        public string? MaxLength { get; set; }
        public string? Precision { get; set; }
        public string? Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsComputed { get; set; }
        public string? Collation { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string? PrimaryKeyName { get; set; }
        public string? ForeignTable { get; set; }
        public string? ForeignColumn { get; set; }
        public string? ForeignKeyName { get; set; }
        public string? FK_DeleteAction { get; set; }
        public string? FK_UpdateAction { get; set; }
        public bool FK_IsDisabled { get; set; }
        public bool FK_IsNotTrusted { get; set; }
    }
    #endregion

    public class MetadataDifference
    {
        public string Schema { get; set; } = "";
        public string Table { get; set; } = "";
        public string Column { get; set; } = "";
        public string DifferenceType { get; set; } = "";
        public string SourceValue { get; set; } = "";
        public string TargetValue { get; set; } = "";
    }

    public class TableInventory
    {
        public string Schema { get; set; } = "";
        public string Name { get; set; } = "";
        public List<ColumnInventory> Columns { get; set; } = new();
    }

    public class ColumnInventory
    {
        public string Name { get; set; } = "";
        public ColumnMetadata? SourceValue { get; set; }
        public ColumnMetadata? TargetValue { get; set; }
        public string DifferenceType { get; set; } = "";
    }
}
