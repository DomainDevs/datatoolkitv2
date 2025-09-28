using System.Text;
using DataToolkit.Builder.Models;
using System.Linq;

public static class MapperGenerator
{
    /// <summary>
    /// Genera el código del mapper usando la metadata de la tabla (DbTable) y el domainName.
    /// Si useMapperly = true genera el mapper parcial para Riok.Mapperly; si no, genera mapper manual.
    /// </summary>
    public static string Generate(DbTable table, string domainName, bool useMapperly)
    {
        var entityName = ToPascalCase(table.Name);            // e.g. PvHeader
        var domain = ToPascalCase(domainName);                // e.g. Polizas
        var requestDtoName = entityName + "RequestDto";
        var responseDtoName = entityName + "ResponseDto";

        if (useMapperly)
            return GenerateMapperly(domain, entityName, requestDtoName, responseDtoName);

        return GenerateManual(domain, entityName, requestDtoName, responseDtoName, table);
    }

    // Manual mapper: basándose en columnas del DbTable
    private static string GenerateManual(string domain, string entityName, string requestDto, string responseDto, DbTable table)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// =========================================================");
        sb.AppendLine("// Mapper manual (generado por DataToolkit)");
        sb.AppendLine("// =========================================================");
        sb.AppendLine();
        sb.AppendLine($"using Application.Features.{domain}.DTOs;");
        sb.AppendLine($"using Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace Application.Features.{domain}.Mappers;");
        sb.AppendLine();
        sb.AppendLine($"public static class {entityName}Mapper");
        sb.AppendLine("{");

        // DTO → Entity (RequestDto -> Entity)
        sb.AppendLine($"    public static {entityName} ToEntity(this {requestDto} dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {entityName}");
        sb.AppendLine("        {");
        foreach (var col in table.Columns)
        {
            var prop = ToPascalCase(col.Name);
            sb.AppendLine($"            {prop} = dto.{prop},");
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Entity → DTO (Entity -> ResponseDto)
        sb.AppendLine($"    public static {responseDto} ToDto(this {entityName} entity)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {responseDto}");
        sb.AppendLine("        {");
        foreach (var col in table.Columns)
        {
            var prop = ToPascalCase(col.Name);
            sb.AppendLine($"            {prop} = entity.{prop},");
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    // Mapperly partial mapper
    private static string GenerateMapperly(string domain, string entityName, string requestDto, string responseDto)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// =========================================================");
        sb.AppendLine("// Este mapper fue generado automáticamente por DataToolkit.");
        sb.AppendLine("// Usa Riok.Mapperly para generar las implementaciones en build.");
        sb.AppendLine("// =========================================================");
        sb.AppendLine();
        sb.AppendLine($"using Application.Features.{domain}.DTOs;");
        sb.AppendLine($"using Application.Features.{domain}.Commands;");
        sb.AppendLine("using Riok.Mapperly.Abstractions;");
        sb.AppendLine("using Entities = Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace Application.Features.{domain}.Mappers;");
        sb.AppendLine();
        //sb.AppendLine("[Mapper]");
        sb.AppendLine("[Mapper(AllowNullPropertyAssignment = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]");
        sb.AppendLine($"public static partial class {entityName}Mapper");
        sb.AppendLine("{");
        sb.AppendLine($"    // DTO → Commands");
        sb.AppendLine($"    public static partial {entityName}UpdateCommand ToUpdateCommand(this {entityName}UpdateRequestDto dto);");
        sb.AppendLine($"    public static partial {entityName}CreateCommand ToCommandCreate(this {entityName}CreateRequestDto dto);");
        sb.AppendLine();
        sb.AppendLine($"    // Commands → Entity");
        sb.AppendLine($"    public static partial Entities.{entityName} ToEntity({entityName}CreateCommand command);");
        sb.AppendLine($"    public static partial Entities.{entityName} ToEntity({entityName}UpdateCommand command);");
        sb.AppendLine();
        sb.AppendLine($"    // Entity → DTO");
        sb.AppendLine($"    public static partial {entityName}QueryResponseDto ToDto(Entities.{entityName} entity);");
        sb.AppendLine();
        sb.AppendLine("}");

        return sb.ToString();
    }

    // -------------------------
    // Helpers
    // -------------------------
    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var parts = name.Split(new[] { '_', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
        return string.Join("", parts);
    }
}
