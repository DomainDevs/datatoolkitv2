namespace DataToolkit.Builder.Helpers
{
    public static class SqlTypeMapper
    {
        public static (string ClrType, string? RangeAttribute) ConvertToClrType(
            string sqlType, int? precision, int? scale, bool isNullable, bool isEntity = false)
        {
            sqlType = sqlType.ToLowerInvariant();
            string clrType;
            string? rangeAttribute = null;

            switch (sqlType)
            {
                // Números enteros
                case "tinyint":
                    clrType = "byte";
                    if (!isEntity) //en las entidades no va, lo valida la DTO
                        rangeAttribute = "[Range(0, 255)]";
                    break;

                case "smallint":
                    clrType = "short";
                    //rangeAttribute = "[Range(short.MinValue, short.MaxValue)]";
                    break;

                case "int":
                    clrType = "int";
                    //rangeAttribute = "[Range(int.MinValue, int.MaxValue)]";
                    break;

                case "bigint":
                    clrType = "long";
                    if (!isEntity) //en las entidades no va, lo valida la DTO
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
                                if (!isEntity) //en las entidades no va, lo valida la DTO
                                    rangeAttribute = $"[Range(0, {maxValue})]";
                            }
                            else
                            {
                                // número entero con precisión limitada
                                var maxValue = long.Parse(new string('9', precision.Value));
                                clrType = "long";
                                if (!isEntity) //en las entidades no va, lo valida la DTO
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
                                if (!isEntity) //en las entidades no va, lo valida la DTO
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
                    if (isEntity)
                    {
                        rangeAttribute = $"[MaxLength({precision})]";
                    }
                    else
                    {
                        rangeAttribute = $"[StringLength({precision})]";
                    }
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
