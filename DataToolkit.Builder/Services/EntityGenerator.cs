using System.Text;
using DataToolkit.Builder.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataToolkit.Builder.Services
{
    public class EntityGenerator
    {
        /// <summary>
        /// Genera una clase C# con Data Annotations (Table/Key/Column/Identity/MaxLength) a partir de DbTable.
        /// </summary>
        public string GenerateEntity(DbTable table, string @namespace = "DataToolkit.SampleApi.Models")
        {
            var sb = new StringBuilder();

            // usings
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");

            // Nombre de clase en PascalCase seguro
            var className = ToPascalCase(table.Name);
            sb.AppendLine($"    [Table(\"{table.Name}\", Schema = \"{table.Schema}\")]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var col in table.Columns)
            {
                // Key
                if (col.IsPrimaryKey)
                    sb.AppendLine("        [Key]");

                // Identity
                if (col.IsIdentity)
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");

                // Column
                sb.AppendLine($"        [Column(\"{col.Name}\")]");

                // MaxLength (strings)
                if (IsStringType(col.SqlType) && col.Length.HasValue && col.Length.Value > 0)
                {
                    var len = NormalizeStringLength(col.SqlType, col.Length.Value);
                    if (len > 0)
                        sb.AppendLine($"        [MaxLength({len})]");
                }

                var clrType = MapSqlTypeToClr(col.SqlType, col.IsNullable);
                var propName = ToPascalCase(col.Name);
                sb.AppendLine($"        public {clrType} {propName} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static bool IsStringType(string sqlType)
        {
            var t = (sqlType ?? "").ToLowerInvariant();
            return t == "varchar" || t == "nvarchar" || t == "char" || t == "nchar" || t == "text" || t == "ntext";
        }

        private static int NormalizeStringLength(string sqlType, int lengthFromDb)
        {
            var t = (sqlType ?? "").ToLowerInvariant();
            if (t == "nvarchar" || t == "nchar")
                return lengthFromDb > 0 ? lengthFromDb / 2 : lengthFromDb;
            return lengthFromDb;
        }

        private static string MapSqlTypeToClr(string sqlType, bool isNullable)
        {
            var t = (sqlType ?? "").ToLowerInvariant();
            string clr = t switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
                "float" => "double",
                "real" => "float",
                "date" or "datetime" or "smalldatetime" or "datetime2" or "datetimeoffset" => "DateTime",
                "time" => "TimeSpan",
                "uniqueidentifier" => "Guid",
                "binary" or "varbinary" or "image" => "byte[]",
                "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => "string",
                _ => "string"
            };

            if (clr != "string" && clr != "byte[]" && isNullable)
                clr += "?";

            return clr;
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1).ToLowerInvariant() : "")));
        }
    }
}
