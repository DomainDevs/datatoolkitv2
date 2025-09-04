using System.Text;
using System.Text.RegularExpressions;

namespace DataToolkit.Builder.Services;

public static class MapperGenerator
{
    public static string Generate(string entityContent, string dtoContent)
    {
        // Extraer el nombre de la clase Entity
        var entityName = ExtractClassName(entityContent);
        // Extraer el nombre de la clase DTO
        var dtoName = ExtractClassName(dtoContent);

        // Extraer propiedades de cada archivo
        var entityProps = ExtractProperties(entityContent);
        var dtoProps = ExtractProperties(dtoContent);

        var sb = new StringBuilder();
        sb.AppendLine($"public static class {entityName}Mapper");
        sb.AppendLine("{");

        // Mapper DTO → Entity
        sb.AppendLine($"    public static {entityName} ToEntity(this {dtoName} dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = new {entityName}();");
        foreach (var prop in dtoProps)
        {
            if (entityProps.ContainsKey(prop.Key))
                sb.AppendLine($"        entity.{prop.Key} = dto.{prop.Key};");
        }
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Mapper Entity → DTO
        sb.AppendLine($"    public static {dtoName} ToDto(this {entityName} entity)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var dto = new {dtoName}();");
        foreach (var prop in entityProps)
        {
            if (dtoProps.ContainsKey(prop.Key))
                sb.AppendLine($"        dto.{prop.Key} = entity.{prop.Key};");
        }
        sb.AppendLine("        return dto;");
        sb.AppendLine("    }");

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
