using DataToolkit.Builder.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Data.Common;
using DataToolkit.Builder.Helpers;
using DataToolkit.Builder.Controllers;

namespace DataToolkit.Builder.Services
{
    public class DtoGeneratorService
    {
        private readonly Dictionary<string, string> _jsonMapping;

        public DtoGeneratorService(Dictionary<string, string>? jsonMapping = null)
        {
            _jsonMapping = jsonMapping ?? new Dictionary<string, string>();
        }

        public string GenerateDto(DbTable table, string domainName, string mode = "response", string operation = "Create")
        {
            var sb = new StringBuilder();
            bool isRequest = mode.ToLower() == "request";

            var className = $"{ToPascalCase(table.Name)}{operation}{(isRequest ? "RequestDto" : "ResponseDto")}";

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            if (isRequest)
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");

            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domainName}.DTOs");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                // REGLAS SEGÚN OPERACIÓN
                if (
                    (operation.Equals("Create", StringComparison.OrdinalIgnoreCase)
                    || operation.Equals("Update", StringComparison.OrdinalIgnoreCase))
                    && column.IsPrimaryKey && column.IsIdentity)
                {
                    // En CREATE/UPDATE no incluimos Identity PK
                    sb.AppendLine($"        [JsonIgnore]");
                }

                string jsonName = _jsonMapping.ContainsKey(column.Name)
                    ? _jsonMapping[column.Name]
                    : ToCamelCase(column.Name);

                string fieldName = _jsonMapping.ContainsKey(column.Name)
                    ? _jsonMapping[column.Name]
                    : ToPascalCase(column.Name);

                if (column.IsPrimaryKey && column.IsIdentity) 
                {
                    sb.AppendLine($"        //[JsonPropertyName(\"{jsonName}\")]");
                } else
                {
                    sb.AppendLine($"        [JsonPropertyName(\"{jsonName}\")]");
                }
                    

                var (clrType, rangeAttr) = SqlTypeMapper.ConvertToClrType(
                    column.SqlType,
                    column.Precision,
                    column.Scale,
                    column.Length,
                    column.IsNullable
                );

                if (isRequest)
                {
                    if (!column.IsNullable)
                    {
                        if (column.IsPrimaryKey && column.IsIdentity)
                        {
                            sb.AppendLine("        //[Required]");
                        }
                        else
                        {
                            sb.AppendLine("        [Required]");
                        }
                        
                    }
                        
                    if (!string.IsNullOrEmpty(rangeAttr))
                    {
                        sb.AppendLine($"        {rangeAttr}");
                    }
                        
                }
                if (clrType == "string")
                {
                    // agregar StringLength
                    sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {fieldName} {{ get; set; }} = \"\"; ");
                }
                else
                {
                    // agregar otras validaciones por default
                    sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {fieldName} {{ get; set; }}");
                }
                    sb.AppendLine();
            }

            // Propiedades de navegación
            foreach (var nav in table.NavigationProperties)
            {
                var suffix = mode.ToLower() == "request" ? "RequestDto" : "ResponseDto";
                var dtoName = $"{nav.TypeName}{operation}{suffix}";

                if (nav.IsCollection)
                    sb.AppendLine($"        public List<{dtoName}> {nav.PropertyName} {{ get; set; }} = new();");
                else
                    sb.AppendLine($"        public {dtoName}? {nav.PropertyName} {{ get; set; }}");

                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public IEnumerable<DbTable> AddNavigationPropertiesForDtos(
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
                                IsCollection = (!entry.FK.IsUnique) //(!entry.FK.IsCollection && entry.FK.IsUnique)
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


        public string GenerateCascadeDtos(
            DbTable rootTable,
            List<DbTable> enrichedTables,
            string domainName,
            string mode,
            string operation)
        {
            var sb = new StringBuilder();

            // 1) Generar DTO del root
            sb.AppendLine(GenerateDto(rootTable, domainName, mode, operation));

            // 2) Generar hijos según NavigationProperties
            foreach (var nav in rootTable.NavigationProperties)
            {
                var childTable = enrichedTables
                    .FirstOrDefault(t => t.Name.Equals(nav.TypeName, StringComparison.OrdinalIgnoreCase));

                if (childTable == null)
                    continue;

                // Generar DTO del hijo
                sb.AppendLine();
                sb.AppendLine("// ---- Child DTO ----");
                sb.AppendLine(GenerateDto(childTable, domainName, mode, operation));

                // Si es colección, no hacemos generación recursiva adicional.
                // Igual que EntityGenerator: cada tabla genera su propio archivo.
            }

            return sb.ToString();
        }


        
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

        //Cuando el Front es JavasScript (Vue,React) = ToCamelCase, Cuando es Asp.net/Api Rest = (ToPascalCase), por convención
        private string ToCamelCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            // Si NO contiene separadores → convertir solo la primera letra en minúscula y retornar.
            if (!name.Contains('_') && !name.Contains('-') && !name.Contains(' '))
            {
                return char.ToLowerInvariant(name[0]) + name.Substring(1);
            }

            var parts = name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                var clean = new string(parts[i].Where(char.IsLetterOrDigit).ToArray());
                if (string.IsNullOrEmpty(clean))
                    continue;

                if (i == 0)
                {
                    // primera palabra en minúscula
                    builder.Append(clean.ToLowerInvariant());
                }
                else
                {
                    // siguiente palabras en PascalCase
                    builder.Append(char.ToUpperInvariant(clean[0]));
                    if (clean.Length > 1)
                        builder.Append(clean.Substring(1).ToLowerInvariant());
                }
            }

            return builder.ToString();
        }

    }

}
