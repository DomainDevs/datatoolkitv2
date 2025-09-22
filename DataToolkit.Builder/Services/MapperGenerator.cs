using System.Text.RegularExpressions;
using System.Text;

public static class MapperGenerator
{
    public static string Generate(string entityContent, string dtoContent, bool useMapperly)
    {
        var entityName = ExtractClassName(entityContent);
        var dtoName = ExtractClassName(dtoContent);

        if (useMapperly)
            return GenerateMapperly(entityName, dtoName);

        return GenerateManual(entityContent, dtoContent, entityName, dtoName);
    }

    private static string GenerateManual(string entityContent, string dtoContent, string entityName, string dtoName)
    {
        var entityProps = ExtractProperties(entityContent);
        var dtoProps = ExtractProperties(dtoContent);

        var sb = new StringBuilder();
        sb.AppendLine($"//handwritten mapping (mapping manual es lo más rápido y liviano)");
        sb.AppendLine($"public static class {entityName}Mapper");
        sb.AppendLine("{");

        // DTO → Entity
        sb.AppendLine($"    public static {entityName} ToEntity(this {dtoName} dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = new {entityName}();");
        foreach (var prop in dtoProps)
        {
            if (entityProps.ContainsKey(prop.Key))
            {
                var entityType = entityProps[prop.Key];
                var dtoType = prop.Value;

                if (entityType == dtoType)
                {
                    // mismo tipo → asignación directa
                    sb.AppendLine($"        entity.{prop.Key} = dto.{prop.Key};");
                }
                else
                {
                    // tipos distintos → intentar llamar mapper auxiliar
                    sb.AppendLine($"        entity.{prop.Key} = dto.{prop.Key}?.ToEntity();");
                }
            }
        }
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Entity → DTO
        sb.AppendLine($"    public static {dtoName} ToDto(this {entityName} entity)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var dto = new {dtoName}();");
        foreach (var prop in entityProps)
        {
            if (dtoProps.ContainsKey(prop.Key))
            {
                var entityType = prop.Value;
                var dtoType = dtoProps[prop.Key];

                if (entityType == dtoType)
                {
                    sb.AppendLine($"        dto.{prop.Key} = entity.{prop.Key};");
                }
                else
                {
                    sb.AppendLine($"        dto.{prop.Key} = entity.{prop.Key}?.ToDto();");
                }
            }
        }
        sb.AppendLine("        return dto;");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }


    private static string GenerateMapperly(string entityName, string dtoName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// =========================================================");
        sb.AppendLine("// Este mapper fue generado automáticamente por DataToolkit.");
        sb.AppendLine("// Usa Riok.Mapperly para generar las implementaciones en build.");
        sb.AppendLine("// =========================================================");
        sb.AppendLine();
        sb.AppendLine("using Application.DTOs;");
        sb.AppendLine($"using Application.Features.{entityName}.Commands;");
        sb.AppendLine("using Domain.Entities;");
        sb.AppendLine("using Riok.Mapperly.Abstractions;");
        sb.AppendLine();
        sb.AppendLine("namespace Application.Mappers;");
        sb.AppendLine();
        sb.AppendLine("[Mapper]");
        sb.AppendLine($"public static partial class {entityName}Mapper");
        sb.AppendLine("{");
        sb.AppendLine("    // DTO → Commands");
        sb.AppendLine($"    public static partial Update{entityName}Command ToUpdateCommand(this {dtoName} dto);");
        sb.AppendLine($"    public static partial Create{entityName}Command ToCommandCreate(this {dtoName} dto);");
        sb.AppendLine();
        sb.AppendLine("    // Commands → Entity");
        sb.AppendLine($"    public static partial {entityName} ToEntity(Create{entityName}Command command);");
        sb.AppendLine($"    public static partial {entityName} ToEntity(Update{entityName}Command command);");
        sb.AppendLine();
        sb.AppendLine("    // Entity → DTO");
        sb.AppendLine($"    public static partial {dtoName} ToDto({entityName} entity);");
        sb.AppendLine("}");
        return sb.ToString();
    }


    private static string ExtractClassName(string content)
    {
        var match = Regex.Match(content, @"class\s+(\w+)");
        return match.Success ? match.Groups[1].Value : "UnknownClass";
    }

    private static Dictionary<string, string> ExtractProperties(string content)
    {
        var dict = new Dictionary<string, string>();
        var matches = Regex.Matches(content, @"public\s+(\w+)\??\s+(\w+)\s*{");
        foreach (Match match in matches)
        {
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            dict[name] = type;
        }
        return dict;
    }
}
