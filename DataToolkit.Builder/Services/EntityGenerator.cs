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
        var enrichedTables = tables.Select(t => new DbTable
        {
            Name = t.Name,
            Schema = t.Schema,
            Columns = t.Columns.ToList(),
            ForeignKeys = t.ForeignKeys.ToList(),
            PrimaryKeyCount = t.PrimaryKeyCount,
            NavigationProperties = new List<DbNavigationProperty>()
        }).ToList();

        foreach (var table in enrichedTables)
        {
            if (navigationMode == NavigationMode.PrincipalCollections)
            {
                // Colecciones en la tabla principal
                var referencingFks = enrichedTables
                    .SelectMany(t => t.ForeignKeys.Select(fk => new { Child = t, FK = fk }))
                    .Where(x =>
                        string.Equals(x.FK.ReferencedTable, table.Name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.FK.ReferencedSchema ?? "dbo", table.Schema ?? "dbo", StringComparison.OrdinalIgnoreCase)
                    );

                foreach (var entry in referencingFks)
                {
                    var child = entry.Child;
                    var fk = entry.FK;

                    string propName = ToPascalCase(child.Name); // nombre singular
                    if (!table.NavigationProperties.Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                    {
                        table.NavigationProperties.Add(new DbNavigationProperty
                        {
                            PropertyName = propName, //+ "s", // plural
                            TypeName = ToPascalCase(child.Name),
                            IsCollection = true
                        });
                    }
                }
            }
            else if (navigationMode == NavigationMode.ReferenceOnDependent)
            {
                // Referencias en la tabla dependiente
                foreach (var fk in table.ForeignKeys)
                {
                    var referenced = enrichedTables.FirstOrDefault(t =>
                        string.Equals(t.Name, fk.ReferencedTable, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(t.Schema ?? "dbo", fk.ReferencedSchema ?? "dbo", StringComparison.OrdinalIgnoreCase)
                    );

                    string propName = ToPascalCase(fk.ReferencedTable);
                    if (!table.NavigationProperties.Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                    {
                        table.NavigationProperties.Add(new DbNavigationProperty
                        {
                            PropertyName = propName,
                            TypeName = referenced != null ? ToPascalCase(referenced.Name) : ToPascalCase(fk.ReferencedTable),
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
        var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p =>
            char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1).ToLowerInvariant() : "")
        ));
    }
}

public class DbNavigationProperty
{
    public string PropertyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsCollection { get; set; } = false;
}