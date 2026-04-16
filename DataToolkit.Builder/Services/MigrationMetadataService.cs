using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;
using System.IO;
using System.Text;

namespace DataToolkit.Builder.Services;

public class MigrationMetadataService
{

    private static readonly List<string> _includedTables = new()
    {
        "Cliente",
        "PolizaRiesgos"
    };

    /// <summary>
    /// Extrae metadata de tablas y columnas usando la conexión activa.
    /// </summary>
    public async Task<List<TableMetadata>> ExtractMetadataAsync(ISqlConnectionManager connectionManager)
    {
        var metadata = new List<TableMetadata>();

        if (!connectionManager.IsConnected())
            return metadata;

        using var conn = connectionManager.GetConnection();

        // 🔴 FIX CRÍTICO: asegurar conexión abierta
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var tableFilter = string.Join(",",
            _includedTables.Select(t => $"'{t.Replace("'", "''")}'"));

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, ty.name AS DataType,
    CASE WHEN c.max_length = -1 THEN 'MAX'
      WHEN ty.name IN ('nvarchar','nchar') THEN CAST(c.max_length / 2 AS VARCHAR(10))  
      ELSE CAST(c.max_length AS VARCHAR(10)) END AS MaxLength,
    c.precision AS Precision, c.scale AS Scale,
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable,
    CASE WHEN c.is_identity = 1 THEN 'YES' ELSE 'NO' END AS IsIdentity,
    CASE WHEN c.is_computed = 1 THEN 'YES' ELSE 'NO' END AS IsComputed,
    c.collation_name AS Collation, dc.definition AS DefaultValue,
    CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS IsPrimaryKey,
    pk.constraint_name AS PrimaryKeyName,
    rt.name AS ForeignTable, rc.name AS ForeignColumn, fkref.name AS ForeignKeyName,
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
WHERE t.name IN ({tableFilter})
ORDER BY s.name, t.name, c.column_id;
";

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var schema = reader["SchemaName"]?.ToString() ?? "";
            var tableName = reader["TableName"]?.ToString() ?? "";

            var table = metadata.FirstOrDefault(m =>
                m.Schema == schema && m.Name == tableName);

            if (table == null)
            {
                table = new TableMetadata
                {
                    Schema = schema,
                    Name = tableName,
                    Columns = new List<ColumnMetadata>()
                };

                metadata.Add(table);
            }

            table.Columns.Add(new ColumnMetadata
            {
                Name = reader["ColumnName"]?.ToString() ?? "",
                SqlType = reader["DataType"]?.ToString() ?? "",
                MaxLength = reader["MaxLength"]?.ToString(),
                Precision = reader["Precision"]?.ToString(),
                Scale = reader["Scale"]?.ToString(),
                IsNullable = reader["IsNullable"]?.ToString() == "YES",
                IsIdentity = reader["IsIdentity"]?.ToString() == "YES",
                IsComputed = reader["IsComputed"]?.ToString() == "YES",
                Collation = reader["Collation"]?.ToString(),
                DefaultValue = reader["DefaultValue"]?.ToString(),
                IsPrimaryKey = reader["IsPrimaryKey"]?.ToString() == "YES",
                PrimaryKeyName = reader["PrimaryKeyName"]?.ToString(),
                ForeignTable = reader["ForeignTable"]?.ToString(),
                ForeignColumn = reader["ForeignColumn"]?.ToString(),
                ForeignKeyName = reader["ForeignKeyName"]?.ToString(),
                FK_DeleteAction = reader["FK_DeleteAction"]?.ToString(),
                FK_UpdateAction = reader["FK_UpdateAction"]?.ToString(),
                FK_IsDisabled = reader["FK_IsDisabled"]?.ToString() == "1",
                FK_IsNotTrusted = reader["FK_IsNotTrusted"]?.ToString() == "1"
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

                //if (sourceColumn.SqlType != targetColumn.SqlType)
                if (!Eq(sourceColumn.SqlType, targetColumn.SqlType))
                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                        "Tipo de dato distinto", sourceColumn.SqlType, targetColumn.SqlType));

                //if (sourceColumn.MaxLength != targetColumn.MaxLength)
                if (!Eq(sourceColumn.MaxLength, targetColumn.MaxLength))
                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                        "MaxLength distinto", sourceColumn.MaxLength, targetColumn.MaxLength));

                if (sourceColumn.IsNullable != targetColumn.IsNullable)
                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                        "Nullable distinto", sourceColumn.IsNullable.ToString(), targetColumn.IsNullable.ToString()));

                if (sourceColumn.IsIdentity != targetColumn.IsIdentity)
                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                        "Identity distinto", sourceColumn.IsIdentity.ToString(), targetColumn.IsIdentity.ToString()));

                if (sourceColumn.IsPrimaryKey != targetColumn.IsPrimaryKey)
                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                        "PK distinto", sourceColumn.IsPrimaryKey.ToString(), targetColumn.IsPrimaryKey.ToString()));

                //if (sourceColumn.ForeignTable != targetColumn.ForeignTable ||
                //sourceColumn.ForeignColumn != targetColumn.ForeignColumn)
                if (!Eq(sourceColumn.ForeignTable, targetColumn.ForeignTable) ||
                        !Eq(sourceColumn.ForeignColumn, targetColumn.ForeignColumn))

                    differences.Add(CreateDifference(sourceTable, sourceColumn,
                    "FK distinto",
                    string.Format("{0}.{1}",
                        sourceColumn.ForeignTable ?? "",
                        sourceColumn.ForeignColumn ?? ""),
                    string.Format("{0}.{1}",
                        targetColumn.ForeignTable ?? "",
                        targetColumn.ForeignColumn ?? "")
                    ));
            }
        }

        foreach (var targetTable in targetMetadata)
        {
            var sourceTable = sourceMetadata
                //.FirstOrDefault(t => t.Schema == targetTable.Schema && t.Name == targetTable.Name);
                .FirstOrDefault(t =>
                string.Equals(t.Schema, targetTable.Schema, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.Name, targetTable.Name, StringComparison.OrdinalIgnoreCase));

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

    private static bool Eq(string? a, string? b)
    {
        return string.Equals(a?.Trim(), b?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private MetadataDifference CreateDifference(
        TableMetadata table,
        ColumnMetadata column,
        string type,
        string sourceVal,
        string targetVal)
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

    public async Task GenerateWorkFilesAsync(
        List<TableMetadata> sourceMetadata,
        List<TableMetadata> targetMetadata,
        string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        foreach (var sourceTable in sourceMetadata)
        {
            var targetTable = targetMetadata
                .FirstOrDefault(t =>
                    string.Equals(t.Schema, sourceTable.Schema, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.Name, sourceTable.Name, StringComparison.OrdinalIgnoreCase));

            var sb = new StringBuilder();

            sb.AppendLine("Campo\tNombre\tTipo/Long\tRequerido\tFormato\tDescripción\tObservación\tSolicitud de SISTRAN\tDatos del Cliente\tTablas Relacionadas\tValidaciones\tComentarios / Status\tTipo Conversion\tProceso de Conversion o Forzamiento");
            sb.AppendLine();

            var usedTargetColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int i = 1;

            foreach (var srcCol in sourceTable.Columns)
            {
                var tgtCol = targetTable?.Columns
                    .FirstOrDefault(c => c.Name == srcCol.Name);

                usedTargetColumns.Add(srcCol.Name);

                var row = BuildRow(i++, srcCol, tgtCol, sourceTable);
                sb.AppendLine(row);
            }

            // ➕ columnas nuevas en destino
            if (targetTable != null)
            {
                foreach (var tgtCol in targetTable.Columns)
                {
                    if (usedTargetColumns.Contains(tgtCol.Name))
                        continue;

                    sb.AppendLine(BuildRow(i++, null, tgtCol, sourceTable, isOrphan: true));
                }

                // 🔑 PK al final
                var pkCols = targetTable.Columns.Where(c => c.IsPrimaryKey).ToList();
                if (pkCols.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine($"CLAVES:\tprimary key ( {string.Join(",", pkCols.Select(x => x.Name))} )");
                }
            }

            var file = Path.Combine(outputPath, $"{sourceTable.Schema}_{sourceTable.Name}_WF.txt");
            await File.WriteAllTextAsync(file, sb.ToString(), Encoding.UTF8);
        }
    }

    private string BuildRow(
        int index,
        ColumnMetadata? source,
        ColumnMetadata? target,
        TableMetadata table,
        bool isOrphan = false)
    {
        string campo = source?.Name ?? target?.Name ?? "";

        string tipo = BuildType(source, target);
        string requerido = BuildRequired(source, target);

        string descripcion = "";
        string observacion = "";

        string conversion = "";
        string proceso = "";

        if (source == null && target != null)
        {
            // columna nueva en destino
            descripcion = "Columna existe solo en destino (HUÉRFANA del origen)";
            conversion = "REVISAR USO";
            proceso = "POSIBLE ELIMINACIÓN O AJUSTE";
        }
        else if (source != null && target == null)
        {
            // nueva columna origen
            descripcion = "Columna nueva en origen (DEBE MIGRARSE)";
            conversion = "AGREGAR EN DESTINO";
            proceso = "ALTER TABLE ADD COLUMN";
        }
        else if (source != null && target != null)
        {
            if (!Eq(source.SqlType, target.SqlType))
                observacion += "Tipo diferente; ";

            if (!Eq(source.MaxLength, target.MaxLength))
            {
                observacion += "Longitud diferente; ";

                if (int.TryParse(source.MaxLength, out var s) &&
                    int.TryParse(target.MaxLength, out var t) &&
                    s > t)
                {
                    conversion = "EXPANDIR";
                    proceso = "ALTER COLUMN INCREASE SIZE";
                }
            }

            if (source.IsNullable != target.IsNullable)
                observacion += "Nullability diferente; ";

            if (string.IsNullOrEmpty(conversion))
            {
                conversion = "SIN CAMBIO";
                proceso = "OK";
            }
        }

        return string.Join("\t", new[]
        {
        index.ToString(),
        campo,
        tipo,
        requerido,
        "", // Formato
        "", // Descripción base (ya incluida arriba)
        observacion,
        "", "", "", "", "",
        conversion,
        proceso
    });
    }

    //helpers simples
    private string BuildType(ColumnMetadata? source, ColumnMetadata? target)
    {
        var col = source ?? target;
        if (col == null) return "";

        return $"{col.SqlType}({col.MaxLength ?? ""})";
    }

    private string BuildRequired(ColumnMetadata? source, ColumnMetadata? target)
    {
        var col = source ?? target;
        if (col == null) return "";

        if (col.IsNullable) return "No";
        return "Si";
    }

}

#region Models

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

public class WfFinalRow
{
    public int Orden { get; set; }
    public string Campo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string TipoLong { get; set; } = "";
    public string Requerido { get; set; } = "";
    public string Formato { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string SolicitudSistran { get; set; } = "";
    public string DatosCliente { get; set; } = "";
    public string TablasRelacionadas { get; set; } = "";
    public string Validaciones { get; set; } = "";
    public string ComentariosStatus { get; set; } = "";
    public string TipoConversion { get; set; } = "";
    public string ProcesoConversion { get; set; } = "";
}


#endregion
