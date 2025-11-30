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
                if (operation.Equals("Create", StringComparison.OrdinalIgnoreCase)
                    && column.IsPrimaryKey && column.IsIdentity)
                {
                    // En CREATE no incluimos Identity PK
                    sb.AppendLine($"        [JsonIgnore]");
                }

                if (operation.Equals("Update", StringComparison.OrdinalIgnoreCase)
                    && column.IsPrimaryKey)
                {
                    // En UPDATE no incluimos ninguna PK
                    
                    sb.AppendLine($"        [JsonIgnore]");
                }

                string jsonName = _jsonMapping.ContainsKey(column.Name)
                    ? _jsonMapping[column.Name]
                    : ToPascalCase(column.Name);

                sb.AppendLine($"        [JsonPropertyName(\"{jsonName}\")]");

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
                        sb.AppendLine("        [Required]");

                    if (!string.IsNullOrEmpty(rangeAttr))
                        sb.AppendLine($"        {rangeAttr}");
                }

                sb.AppendLine($"        public {clrType}{(column.IsNullable && clrType != "string" ? "" : "")} {jsonName} {{ get; set; }}");
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
                    // Colecciones de entidades que dependen de esta tabla (1:N)
                    var referencingFks = enrichedTables
                        .SelectMany(t => t.ForeignKeys.Select(fk => new { Child = t, FK = fk }))
                        .Where(x =>
                            string.Equals(x.FK.ReferencedTable, table.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.FK.ReferencedSchema ?? "dbo", table.Schema ?? "dbo", StringComparison.OrdinalIgnoreCase)
                        );

                    foreach (var entry in referencingFks)
                    {
                        var child = entry.Child;

                        string propName = ToPascalCase(child.Name);
                        if (!table.NavigationProperties
                                .Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                        {
                            table.NavigationProperties.Add(new DbNavigationProperty
                            {
                                PropertyName = propName,  // singular
                                TypeName = ToPascalCase(child.Name),
                                IsCollection = true
                            });
                        }
                    }
                }
                else if (navigationMode == NavigationMode.ReferenceOnDependent)
                {
                    // Referencias simple hacia la tabla principal (N:1)
                    foreach (var fk in table.ForeignKeys)
                    {
                        var referenced = enrichedTables.FirstOrDefault(t =>
                            string.Equals(t.Name, fk.ReferencedTable, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(t.Schema ?? "dbo", fk.ReferencedSchema ?? "dbo", StringComparison.OrdinalIgnoreCase)
                        );

                        string propName = ToPascalCase(fk.ReferencedTable);
                        if (!table.NavigationProperties
                                .Any(n => n.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase)))
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
        }que 

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

            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[i].ToLower());
            }
            return string.Join("", parts);
        }
    }

}
