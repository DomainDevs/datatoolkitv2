using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using DataToolkit.Builder.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Text;
using System.Linq.Expressions;
using System.Globalization;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoryController : ControllerBase
{
    private readonly ScriptExtractionService _scriptExtractionService;

    public RepositoryController(ScriptExtractionService scriptExtractionService)
    {
        _scriptExtractionService = scriptExtractionService;
    }

    /// <summary>
    /// Genera el código del repository usando la metadata real de la tabla (incluyendo llaves).
    /// Request debe traer schema y tableName.
    /// </summary>
    [HttpPost("generate/generic")]
    public async Task<IActionResult> GenerateRepository([FromBody] RepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        // Definición de nombres
        var entityNamespace = string.IsNullOrWhiteSpace(request.EntityNamespace)
            ? "Domain.Entities"
            : request.EntityNamespace;

        var repositoryNamespace = string.IsNullOrWhiteSpace(request.RepositoryNamespace)
            ? "Persistence.Repositories"
            : request.RepositoryNamespace;

        var entityName = ToPascalCase(table.Name);
        var repositoryName = $"{entityName}Repository";
        var interfaceName = $"I{entityName}Repository";

        // Generar métodos dinámicos según PK
        var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
        string interfaceMethods;
        string classMethods;

        if (!pkColumns.Any())
        {
            // Sin PK: DeleteAsync y GetByIdAsync por entidad
            interfaceMethods = $@"
        Task<int> DeleteAsync({entityName} entity);
        Task<{entityName}?> GetByIdAsync({entityName} entity);";

            classMethods = $@"
        public Task<int> DeleteAsync({entityName} entity)
        {{
            return _repo.DeleteAsync(entity);
        }}

        public Task<{entityName}?> GetByIdAsync({entityName} entity)
        {{
            return _repo.GetByIdAsync(entity);
        }}";
        }
        else
        {
            // Con PK: métodos con parámetros
            var paramList = BuildParameterList(pkColumns);
            var initObject = BuildEntityInitializer(entityName, pkColumns);

            interfaceMethods = $@"
        Task<int> DeleteByIdAsync({paramList});
        Task<{entityName}?> GetByIdAsync({paramList});";

            classMethods = $@"
        public async Task<int> DeleteByIdAsync({paramList})
        {{
            {initObject}
            return await _repo.DeleteAsync(entity);
        }}

        public Task<{entityName}?> GetByIdAsync({paramList})
        {{
            {initObject}
            return _repo.GetByIdAsync(entity);
        }}";
        }

        // Leer la plantilla
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Repository", "GenericRepository.tpl");
        if (!System.IO.File.Exists(templatePath))
            return NotFound("No se encontró la plantilla GenericRepository.tpl");

        var template = await System.IO.File.ReadAllTextAsync(templatePath);

        // Reemplazar placeholders
        var output = template
            .Replace("{{EntityNamespace}}", entityNamespace)
            .Replace("{{RepositoryNamespace}}", repositoryNamespace)
            .Replace("{{EntityName}}", entityName)
            .Replace("{{RepositoryName}}", repositoryName)
            .Replace("{{InterfaceName}}", interfaceName)
            .Replace("{{InterfaceMethods}}", interfaceMethods)
            .Replace("{{ClassMethods}}", classMethods);

        return Content(output, "text/plain");
    }


    // -------------------------
    // Helpers para generación
    // -------------------------
    private static string BuildParameterList(IEnumerable<DbColumn> pkColumns)
    {
        // Produce: "int codSuc, int codRamo, long nroPol" ... usando SqlTypeMapper para los CLR types
        var parts = new List<string>();
        foreach (var col in pkColumns)
        {
            var (clrType, _) = SqlTypeMapper.ConvertToClrType(col.SqlType, col.Precision, col.Scale, col.Length, col.IsNullable);
            var propName = ToPascalCase(col.Name);
            var varName = ToCamelCase(propName);
            parts.Add($"{clrType} {varName}");
        }
        return string.Join(", ", parts);
    }

    private static string BuildEntityInitializer(string entityName, IEnumerable<DbColumn> pkColumns)
    {
        // Produce: "var entity = new MyEntity { CodSuc = codSuc, CodRamo = codRamo, ... };"
        var assignments = pkColumns.Select(col =>
        {
            var propName = ToPascalCase(col.Name);
            var varName = ToCamelCase(propName);
            return $"{propName} = {varName}";
        });
        var assignJoined = string.Join(", ", assignments);
        return $"var entity = new {entityName} {{ {assignJoined} }};";
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[i].ToLower());
        return string.Join("", parts);
    }

    private static string ToCamelCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        if (pascal.Length == 1) return pascal.ToLower();
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
}

// Clase para recibir la petición
public class RepositoryRequest
{
    // Ahora esperamos TableName & Schema (en lugar de EntityName)
    public string TableName { get; set; } = string.Empty;

    [DefaultValue("dbo")]
    public string Schema { get; set; } = "dbo";

    [DefaultValue("Domain.Entities")]
    public string EntityNamespace { get; set; } = "Domain.Entities";

    [DefaultValue("Persistence.Repositories")]
    public string? RepositoryNamespace { get; set; } = "Persistence.Repositories";
}
 