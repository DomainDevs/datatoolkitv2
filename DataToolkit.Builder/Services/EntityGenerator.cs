using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataToolkit.Builder.Models;
using System.Linq;

namespace DataToolkit.Builder.Services
{
    public class EntityGenerator
    {
        public string GenerateEntity(DbTable table)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine("#nullable enable"); // Permite string? automáticamente
            sb.AppendLine();
            sb.AppendLine("namespace Domain.Entities");
            sb.AppendLine("{");

            var className = ToPascalCase(table.Name);
            sb.AppendLine($"    [Table(\"{table.Name}\", Schema = \"{table.Schema}\")]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                var propName = ToPascalCase(column.Name);
                var clrType = MapSqlTypeToClr(column.SqlType);

                sb.AppendLine($"        // {column.Name}");

                if (column.IsIdentity)
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");

                if (!column.IsNullable && !column.IsIdentity && clrType == "string")
                    sb.AppendLine("        [Required]");

                if (IsStringType(column.SqlType) && column.Length.HasValue && column.Length.Value > 0)
                    sb.AppendLine($"        [MaxLength({column.Length.Value})]");

                // Column con soporte para decimal/numeric
                if ((column.SqlType.Equals("decimal", StringComparison.OrdinalIgnoreCase)
                    || column.SqlType.Equals("numeric", StringComparison.OrdinalIgnoreCase))
                    && column.Precision.HasValue && column.Scale.HasValue)
                {
                    sb.AppendLine($"        [Column(\"{column.Name}\", TypeName = \"{column.SqlType}({column.Precision},{column.Scale})\")]");
                }
                else
                {
                    sb.AppendLine($"        [Column(\"{column.Name}\")]");
                }

                // Tipo CLR con nulabilidad
                string nullableSuffix = "";
                if (clrType == "string" && column.IsNullable && !column.IsIdentity)
                    nullableSuffix = "?";
                else if (clrType != "string" && column.IsNullable)
                    nullableSuffix = "?";

                sb.AppendLine($"        public {clrType}{nullableSuffix} {propName} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string MapSqlTypeToClr(string sqlType)
        {
            return sqlType.ToLower() switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" or "numeric" => "decimal",
                "float" => "double",
                "real" => "float",
                "date" or "datetime" or "smalldatetime" or "datetime2" => "DateTime",
                "nvarchar" or "varchar" or "text" or "ntext" => "string",
                "uniqueidentifier" => "Guid",
                _ => "string"
            };
        }

        private bool IsStringType(string sqlType) =>
            sqlType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase)
            || sqlType.Equals("varchar", StringComparison.OrdinalIgnoreCase)
            || sqlType.Equals("text", StringComparison.OrdinalIgnoreCase)
            || sqlType.Equals("ntext", StringComparison.OrdinalIgnoreCase);

        private string ToPascalCase(string name)
        {
            var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parts.Select(p =>
                char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1).ToLowerInvariant() : "")));
        }
    }
}
