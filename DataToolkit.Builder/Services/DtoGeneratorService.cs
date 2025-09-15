using DataToolkit.Builder.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Data.Common;
using DataToolkit.Builder.Helpers;

namespace DataToolkit.Builder.Services
{
    public class DtoGeneratorService
    {
        private readonly Dictionary<string, string> _jsonMapping;

        public DtoGeneratorService(Dictionary<string, string>? jsonMapping = null)
        {
            _jsonMapping = jsonMapping ?? new Dictionary<string, string>();
        }

        public string GenerateDto(DbTable table, string mode = "request")
        {
            var sb = new StringBuilder();
            bool isRequest = mode.ToLower() == "request";

            var className = table.Name + (isRequest ? "RequestDto" : "ResponseDto");

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            if (isRequest)
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine();
            sb.AppendLine("namespace Application.DTOs");
            sb.AppendLine("{");

            //if (isRequest)
            //    sb.AppendLine($"    [GenerateMapper(typeof(Domain.Entities.{ToPascalCase(table.Name)}))]");

            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                // JsonPropertyName según mapeo o por defecto en PascalCase
                string jsonName = _jsonMapping.ContainsKey(column.Name)
                    ? _jsonMapping[column.Name]
                    : ToPascalCase(column.Name); // aquí mantiene funcionalidad original si hay mapeo

                if (isRequest)
                {
                    if (column.IsPrimaryKey && column.IsIdentity)
                    {
                        sb.AppendLine("        //Identity");
                        //sb.AppendLine();
                        //continue;
                    }
                    sb.AppendLine($"        [JsonPropertyName(\"{jsonName}\")]");
                }
                else
                {
                    sb.AppendLine($"        [JsonPropertyName(\"{jsonName}\")]");
                }
                    

                if (column.SqlType.ToLower().Contains("char"))
                {
                    column.Precision = column.Length;
                }

                    var (clrType, rangeAttr) = SqlTypeMapper.ConvertToClrType(
                    column.SqlType,
                    column.Precision,
                    column.Scale,
                    column.IsNullable
                    );

                if (isRequest)
                {
                    if (!column.IsNullable)
                        sb.AppendLine("        [Required]");

                    if (!string.IsNullOrEmpty(rangeAttr))
                        sb.AppendLine($"        {rangeAttr}");

                    sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {jsonName} {{ get; set; }}");

                }
                else
                {
                    sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {jsonName} {{ get; set; }}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string ToPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[i].ToLower());
            }
            return string.Join("", parts);
        }
    }

}
