using DataToolkit.Builder.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Data.Common;

namespace DataToolkit.Builder.Services
{
    public class DtoGeneratorService
    {
        private readonly Dictionary<string, string> _jsonMapping;

        public DtoGeneratorService(Dictionary<string, string>? jsonMapping = null)
        {
            _jsonMapping = jsonMapping ?? new Dictionary<string, string>();
        }

        public string GenerateDto(DbTable table, string mode = "response")
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
                    sb.AppendLine($"        [JsonPropertyName(\"{jsonName}\")]");
                }
                else
                {
                    jsonName = column.Name;
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

                    //sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {column.Name} {{ get; set; }}");
                    sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {jsonName} {{ get; set; }}");

                    /*
                    if (!column.IsNullable)
                        sb.AppendLine("        [Required]");

                    // Mantiene StringLength de cualquier tipo, como en la versión original
                    if (column.Length.HasValue && column.SqlType.ToLower().Contains("char"))
                        sb.AppendLine($"        [StringLength({column.Length.Value})]");

                    //range
                    if (column.SqlType.ToLower().Contains("int"))
                    {
                        sb.AppendLine("        [Range(0, int.MaxValue)]");
                    }

                    // Range
                    if (column.SqlType.ToLower().Contains("numeric"))
                    {

                        if (column.Precision <= 9)
                        {
                            var maxValue = long.Parse(new string('9', column.Precision.GetValueOrDefault(1)));
                            sb.AppendLine($"        [Range(0, {maxValue})]");
                        }
                        else
                        {
                            if (column.Precision <= 18 && column.Scale == 0)
                            {
                                var maxValue = long.Parse(new string('9', column.Precision.Value));
                                sb.AppendLine($"        [Range(0, {maxValue})]");
                            }
                        }
                    }

                    //Decimal
                    if (column.SqlType.ToLower().Contains("decimal"))
                        sb.AppendLine("        [Range(typeof(decimal), \"0\", \"999999999\")]");

                    // RegularExpression (ejemplo básico: decimales con punto o coma)
                    if (column.SqlType == "decimal")
                        sb.AppendLine("        [RegularExpression(@\"^[0-9]+([\\.,][0-9]{1,2})?$\", ErrorMessage = \"Formato inválido. Use punto o coma como separador decimal.\")]");
                    */
                }


                //string clrType = ConvertToClrType(column.SqlType, column.IsNullable, column.Precision, column.Scale);
                //sb.AppendLine($"        public {clrType} {column.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string ConvertToClrType(string sqlType, bool isNullable, int? precision = null, int? scale = null)
        {
            //sqlType = sqlType.ToLower();
            sqlType = sqlType.ToLowerInvariant();
            string clrType;
            string? rangeAttribute = null;

            switch (sqlType)
            {
                // Números enteros
                case "tinyint":
                    clrType = "byte";
                    rangeAttribute = "[Range(0, 255)]";
                    break;

                case "smallint":
                    clrType = "short";
                    rangeAttribute = "[Range(short.MinValue, short.MaxValue)]";
                    break;

                case "int":
                    clrType = "int";
                    rangeAttribute = "[Range(int.MinValue, int.MaxValue)]";
                    break;

                case "bigint":
                    clrType = "long";
                    rangeAttribute = "[Range(long.MinValue, long.MaxValue)]";
                    break;

                // Decimales / numéricos con precisión y escala
                case "decimal":
                case "numeric":
                    if (precision.HasValue && scale.HasValue)
                    {
                        if (scale.Value == 0)
                        {
                            if (precision.Value <= 9)
                            {
                                var maxValue = int.Parse(new string('9', precision.Value));
                                clrType = "int";
                                rangeAttribute = $"[Range(0, {maxValue})]";
                            }
                            else
                            {
                                var maxValue = long.Parse(new string('9', precision.Value));
                                clrType = "long";
                                rangeAttribute = $"[Range(0, {maxValue})]";
                            }
                            
                        }
                        else
                        {
                            // número con decimales
                            clrType = "decimal";
                            // Ejemplo: precision = 10, scale = 2 → rango ±99999999.99
                            var intPart = precision.Value - scale.Value;
                            if (intPart > 0)
                            {
                                var maxIntPart = new string('9', intPart);
                                var maxDecPart = new string('9', scale.Value);
                                var maxValue = $"{maxIntPart}.{maxDecPart}";
                                rangeAttribute = $"[Range(typeof(decimal), \"0\", \"{maxValue}\")]";
                            }
                        }
                    }
                    else
                    {
                        clrType = "decimal";
                    }
                    break;

                // Flotantes
                case "real":
                    clrType = "float";
                    break;

                case "float":
                    clrType = "double";
                    break;

                // Texto
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                case "text":
                case "ntext":
                    clrType = "string";
                    rangeAttribute = $"[StringLength({precision})]";
                    break;
                // Fecha y tiempo
                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "timestamp":
                    clrType = "DateTime";
                    break;

                case "time":
                    clrType = "TimeSpan";
                    break;

                // Binarios
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                    clrType = "byte[]";
                    break;

                case "bit":
                    clrType = "bool";
                    break;

                default:
                    clrType = "object";
                    break;
            }

            // Manejo de nullables
            if (isNullable && clrType != "string" && clrType != "byte[]" && clrType != "object")
            {
                clrType += "?";
            }

            return (clrType);


            /*
            if (sqlType is "numeric" or "decimal")
            {

                if (scale.HasValue && scale.Value > 0)
                {
                    // Tiene decimales → siempre decimal
                    return isNullable ? "decimal?" : "decimal";
                }

                if (precision.HasValue)
                {
                    if (precision.Value <= 9)
                        return isNullable ? "int?" : "int";
                    if (precision.Value <= 18)
                        return isNullable ? "long?" : "long";
                }

                return isNullable ? "decimal?" : "decimal"; // precision > 18
            }

            return sqlType switch
            {
                "int" => isNullable ? "int?" : "int",
                "bigint" => isNullable ? "long?" : "long",
                "smallint" => isNullable ? "short?" : "short",
                "tinyint" => isNullable ? "byte?" : "byte",
                "bit" => isNullable ? "bool?" : "bool",
                "float" => isNullable ? "double?" : "double",
                "real" => isNullable ? "float?" : "float",

                "datetime" or "date" or "smalldatetime" or "datetime2"
                    => isNullable ? "DateTime?" : "DateTime",
                "datetimeoffset" => isNullable ? "DateTimeOffset?" : "DateTimeOffset",
                "time" => isNullable ? "TimeSpan?" : "TimeSpan",

                "char" or "nchar" or "varchar" or "nvarchar"
                    or "text" or "ntext" => "string",

                "binary" or "varbinary" or "image" or "rowversion" or "timestamp"
                    => "byte[]",

                "uniqueidentifier" => isNullable ? "Guid?" : "Guid",

                _ => "string"
            };*/
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

    public static class SqlTypeMapper
    {
        public static (string ClrType, string? RangeAttribute) ConvertToClrType(
            string sqlType, int? precision, int? scale, bool isNullable)
        {
            sqlType = sqlType.ToLowerInvariant();
            string clrType;
            string? rangeAttribute = null;

            switch (sqlType)
            {
                // Números enteros
                case "tinyint":
                    clrType = "byte";
                    rangeAttribute = "[Range(0, 255)]";
                    break;

                case "smallint":
                    clrType = "short";
                    rangeAttribute = "[Range(short.MinValue, short.MaxValue)]";
                    break;

                case "int":
                    clrType = "int";
                    rangeAttribute = "[Range(int.MinValue, int.MaxValue)]";
                    break;

                case "bigint":
                    clrType = "long";
                    rangeAttribute = "[Range(long.MinValue, long.MaxValue)]";
                    break;

                // Decimales / numéricos con precisión y escala
                case "decimal":
                case "numeric":
                    if (precision.HasValue && scale.HasValue)
                    {
                        if (scale.Value == 0)
                        {
                            if (precision.Value <= 9)
                            {
                                // número entero con precisión limitada
                                var maxValue = int.Parse(new string('9', precision.Value));
                                clrType = "int";
                                rangeAttribute = $"[Range(0, {maxValue})]";
                            }
                            else
                            {
                                // número entero con precisión limitada
                                var maxValue = long.Parse(new string('9', precision.Value));
                                clrType = "long";
                                rangeAttribute = $"[Range(0, {maxValue})]";
                            }

                        }
                        else
                        {
                            // número con decimales
                            clrType = "decimal";
                            // Ejemplo: precision = 10, scale = 2 → rango ±99999999.99
                            var intPart = precision.Value - scale.Value;
                            if (intPart > 0)
                            {
                                var maxIntPart = new string('9', intPart);
                                var maxDecPart = new string('9', scale.Value);
                                var maxValue = $"{maxIntPart}.{maxDecPart}";
                                rangeAttribute = $"[Range(typeof(decimal), \"0\", \"{maxValue}\")]";
                            }
                        }
                    }
                    else
                    {
                        clrType = "decimal";
                    }
                    break;

                // Flotantes
                case "real":
                    clrType = "float";
                    break;

                case "float":
                    clrType = "double";
                    break;

                // Texto
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                case "text":
                case "ntext":
                    clrType = "string";
                    rangeAttribute = $"[StringLength({precision})]";
                    break;

                // Fecha y tiempo
                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "timestamp":
                    clrType = "DateTime";
                    break;

                case "time":
                    clrType = "TimeSpan";
                    break;

                // Binarios
                case "binary":
                case "varbinary":
                case "image":
                case "rowversion":
                    clrType = "byte[]";
                    break;

                case "bit":
                    clrType = "bool";
                    break;

                default:
                    clrType = "object";
                    break;
            }

            // Manejo de nullables
            if (isNullable && clrType != "string" && clrType != "byte[]" && clrType != "object")
            {
                clrType += "?";
            }

            return (clrType, rangeAttribute);
        }
    }


}
