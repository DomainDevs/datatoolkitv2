using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataToolkit.Builder.Models;
using System.Linq;
using DataToolkit.Builder.Helpers;
using DataToolkit.Builder.Controllers;

namespace DataToolkit.Builder.Services;

public class EntityGenerator
{
    public string GenerateEntity(DbTable table)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Domain.Entities");
        sb.AppendLine("{");

        var className = ToPascalCase(table.Name);
        sb.AppendLine($"    [Table(\"{table.Name}\", Schema = \"{table.Schema}\")]");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        // Columnas normales
        foreach (var column in table.Columns)
        {
            var propName = ToPascalCase(column.Name);

            if (column.SqlType.ToLower().Contains("char"))
                column.Precision = column.Length;

            var (clrType, rangeAttr) = SqlTypeMapper.ConvertToClrType(
                column.SqlType,
                column.Precision,
                column.Scale,
                column.Length,
                column.IsNullable,
                true
            );

            sb.AppendLine($"        // {column.Name}");

            if (column.IsPrimaryKey)
            {
                sb.AppendLine("        [Key]");
                if (column.IsIdentity)
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
            }

            if (!column.IsNullable && !column.IsIdentity && clrType == "string")
                sb.AppendLine("        [Required]");

            if ((column.SqlType.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                 column.SqlType.Equals("numeric", StringComparison.OrdinalIgnoreCase)) &&
                 column.Precision.HasValue && column.Scale.HasValue)
            {
                sb.AppendLine($"        [Column(\"{column.Name}\")]");
                if (!string.IsNullOrEmpty(rangeAttr))
                    sb.AppendLine($"        {rangeAttr}");
            }
            else
            {
                sb.AppendLine($"        [Column(\"{column.Name}\")]");
                if (IsStringType(column.SqlType) && column.Length.HasValue && column.Length.Value > 0 && !string.IsNullOrEmpty(rangeAttr))
                    sb.AppendLine($"        {rangeAttr}");
            }

            string nullableSuffix = "";
            sb.AppendLine($"        public {clrType}{nullableSuffix} {propName} {{ get; set; }}");
            sb.AppendLine();
        }

        // Propiedades de navegación
        foreach (var nav in table.NavigationProperties)
        {
            if (nav.IsCollection)
                sb.AppendLine($"        public ICollection<{nav.TypeName}> {nav.PropertyName} {{ get; set; }} = new List<{nav.TypeName}>();");
            else
                sb.AppendLine($"        public {nav.TypeName}? {nav.PropertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<DbTable> AddNavigationProperties(
        List<DbTable> tables,
        NavigationMode navigationMode,
        int maxDepth = 0)
    {
        // Copia liviana
        var enrichedTables = tables
            .Select(t => new DbTable
            {
                Name = t.Name,
                Schema = t.Schema,
                Columns = t.Columns.ToList(),
                ForeignKeys = t.ForeignKeys.ToList(),
                PrimaryKeyCount = t.PrimaryKeyCount,
                NavigationProperties = new List<DbNavigationProperty>()
            })
            .ToList();

        foreach (var table in enrichedTables)
        {
            //
            // 1) PRINCIPAL COLLECTIONS → lado principal (1:N ó 1:1)
            //
            if (navigationMode == NavigationMode.PrincipalCollections)
            {
                var children = enrichedTables
                    .SelectMany(t => t.ForeignKeys
                        .Where(fk => fk.ReferencedTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(fk => new { Child = t, FK = fk }));

                foreach (var entry in children)
                {
                    string propName = ToPascalCase(entry.Child.Name);

                    if (!table.NavigationProperties.Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                    {
                        table.NavigationProperties.Add(new DbNavigationProperty
                        {
                            PropertyName = propName,
                            TypeName = ToPascalCase(entry.Child.Name),
                            IsCollection = entry.FK.IsCollection //IsCollection = (!entry.FK.IsUnique) //(!entry.FK.IsCollection && entry.FK.IsUnique)
                        });
                    }
                }
            }

            //
            // 2) REFERENCE ON DEPENDENT → lado dependiente (N:1 ó 1:1)
            //
            if (navigationMode == NavigationMode.ReferenceOnDependent)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    string propName = ToPascalCase(fk.ReferencedTable);

                    if (!table.NavigationProperties.Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                    {
                        table.NavigationProperties.Add(new DbNavigationProperty
                        {
                            PropertyName = propName,
                            TypeName = ToPascalCase(fk.ReferencedTable),
                            IsCollection = false
                        });
                    }
                }
            }
        }

        return enrichedTables;
    }

    private bool IsStringType(string sqlType) =>
        sqlType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
        sqlType.Equals("varchar", StringComparison.OrdinalIgnoreCase) ||
        sqlType.Equals("text", StringComparison.OrdinalIgnoreCase) ||
        sqlType.Equals("ntext", StringComparison.OrdinalIgnoreCase);

    private string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Si NO contiene separadores → devolver tal cual.
        if (!name.Contains('_') && !name.Contains('-') && !name.Contains(' '))
            return name;

        var parts = name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var builder = new StringBuilder();

        foreach (var part in parts)
        {
            var clean = new string(part.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(clean))
                continue;

            builder.Append(char.ToUpperInvariant(clean[0]));
            if (clean.Length > 1)
                builder.Append(clean.Substring(1).ToLowerInvariant());
        }

        return builder.ToString();
    }
}

public class DbNavigationProperty
{
    public string PropertyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsCollection { get; set; } = false;
}