using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataToolkit.Builder.Models;

namespace DataToolkit.Builder.Services
{
    public class CSharpModelGenerator
    {
        public string GenerateClass(DbTable table, string namespaceName = "DataToolkit.SampleApi.Models")
        {
            var sb = new StringBuilder();

            // Usings
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // Table attribute
            sb.AppendLine($"    [Table(\"{table.Name}\")]");
            sb.AppendLine($"    public class {table.Name}");
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

                // Type mapping
                string clrType = MapSqlTypeToClr(col.SqlType, col.IsNullable);

                sb.AppendLine($"        public {clrType} {col.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string MapSqlTypeToClr(string sqlType, bool isNullable)
        {
            string clrType = sqlType switch
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

            // Nullable value types
            if (clrType != "string" && clrType != "byte[]" && isNullable)
                clrType += "?";

            return clrType;
        }
    }
}
