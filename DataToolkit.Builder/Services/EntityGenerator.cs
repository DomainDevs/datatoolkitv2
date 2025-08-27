using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataToolkit.Builder.Models;

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
            sb.AppendLine("namespace DataToolkit.SampleApi.Models");
            sb.AppendLine("{");

            // Clase con atributo Table
            sb.AppendLine($"    [Table(\"{table.Name}\", Schema = \"{table.Schema}\")]");
            sb.AppendLine($"    public class {table.Name}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                // Comentario simple
                sb.AppendLine($"        // {column.Name}");

                // PK Identity (ejemplo, puedes ajustar según tu lógica)
                if (column.IsIdentity)
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");

                // Column
                sb.AppendLine($"        [Column(\"{column.Name}\")]");

                // MaxLength para strings
                if (column.SqlType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) && column.Length.HasValue)
                    sb.AppendLine($"        [MaxLength({column.Length.Value})]");

                // Tipo con nulabilidad
                var clrType = MapSqlTypeToClr(column.SqlType);
                if (!column.IsNullable && clrType != "string")
                    sb.AppendLine($"        public {clrType} {column.Name} {{ get; set; }}");
                else
                    sb.AppendLine($"        public {clrType}? {column.Name} {{ get; set; }}");

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
    }
}
