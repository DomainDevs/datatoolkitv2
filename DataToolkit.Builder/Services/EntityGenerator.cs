using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataToolkit.Builder.Models;
using System.Linq;
using DataToolkit.Builder.Helpers;

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

            //var pkColumns = new List<string>();

            foreach (var column in table.Columns)
            {
                var propName = ToPascalCase(column.Name);

                if (column.SqlType.ToLower().Contains("char"))
                {
                    column.Precision = column.Length;
                }

                var (clrType, rangeAttr) = SqlTypeMapper.ConvertToClrType(
                column.SqlType,
                column.Precision,
                column.Scale,
                column.IsNullable,
                true
                );

                sb.AppendLine($"        // {column.Name}");

                // Si la columna es PK
                if (column.IsPrimaryKey)
                {
                    //pkColumns.Add($"{clrType} {propName}");
                    if (column.IsIdentity) ///Si identity fijo lleva Key
                    {
                        sb.AppendLine("        [Key]");
                        sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                    }
                    else
                    {
                        //Si no es identity, debe validar si es llave primaria compuesta
                        //if(table.PrimaryKeyCount==1)
                        sb.AppendLine("        [Key]");
                    }
                }
                //Si es identity y no acepta nulo, por defecto es Required
                if (!column.IsNullable && !column.IsIdentity && clrType == "string")
                    sb.AppendLine("        [Required]");

                // Column con soporte para decimal/numeric
                if ((column.SqlType.Equals("decimal", StringComparison.OrdinalIgnoreCase)
                    || column.SqlType.Equals("numeric", StringComparison.OrdinalIgnoreCase))
                    && column.Precision.HasValue && column.Scale.HasValue)
                {
                    sb.AppendLine($"        [Column(\"{column.Name}\")]");
                    if (!string.IsNullOrEmpty(rangeAttr))
                        sb.AppendLine($"        {rangeAttr}");
                }
                else
                {
                    sb.AppendLine($"        [Column(\"{column.Name}\")]");
                    if (IsStringType(column.SqlType) && column.Length.HasValue && column.Length.Value > 0)
                        if (!string.IsNullOrEmpty(rangeAttr))
                            sb.AppendLine($"        {rangeAttr}");
                }

                // Tipo CLR con nulabilidad
                string nullableSuffix = "";


                sb.AppendLine($"        public {clrType}{nullableSuffix} {propName} {{ get; set; }}");
                sb.AppendLine();
            }

            // Cierra la clase
            /*
             *sb.AppendLine("        // Record anidado para PK");
            if (pkColumns.Count > 0)
            {
                sb.AppendLine($"        public record Key({string.Join(", ", pkColumns)});");
                sb.AppendLine();
            }*/

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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
