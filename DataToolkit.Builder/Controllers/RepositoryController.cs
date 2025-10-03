using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using DataToolkit.Builder.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;

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

    [HttpPost("generate/generic")]
    public async Task<IActionResult> GenerateRepository([FromBody] RepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var entityNamespace = string.IsNullOrWhiteSpace(request.EntityNamespace) ? "Domain.Entities" : request.EntityNamespace;
        var repositoryNamespace = string.IsNullOrWhiteSpace(request.RepositoryNamespace) ? "Persistence.Repositories" : request.RepositoryNamespace;

        var entityName = ToPascalCase(table.Name);
        var repositoryName = $"{entityName}Repository";
        var interfaceName = $"I{entityName}Repository";

        var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

        // Construir variables de reemplazo para plantilla
        string pkParameters = pkColumns.Any() ? BuildParameterList(pkColumns) : "";
        string entityParameter = pkColumns.Any() ? "" : $"{entityName} entity";
        string selectPropertiesParameter = $", params Expression<Func<{entityName}, object>>[]? selectProperties";
        string pkInitializer = pkColumns.Any() ? BuildEntityInitializer(entityName, pkColumns) : "";
        string entityInitFallback = pkColumns.Any() ? "" : "var entity = entity;";

        var replacements = new Dictionary<string, string>
        {
            ["{{EntityNamespace}}"] = entityNamespace,
            ["{{RepositoryNamespace}}"] = repositoryNamespace,
            ["{{EntityName}}"] = entityName,
            ["{{RepositoryName}}"] = repositoryName,
            ["{{InterfaceName}}"] = interfaceName,
            ["{{PKParameters}}"] = pkParameters,
            ["{{EntityParameter}}"] = string.IsNullOrWhiteSpace(entityParameter) ? "" : entityParameter,
            ["{{SelectPropertiesParameter}}"] = selectPropertiesParameter,
            ["{{PKInitializer}}"] = pkInitializer,
            ["{{EntityInitFallback}}"] = entityInitFallback,
            ["{{DeleteSuffix}}"] = pkColumns.Any() ? "ByIdAsync" : ""
        };

        string ProcessTemplate(string template) =>
            replacements.Aggregate(template, (current, kv) => current.Replace(kv.Key, kv.Value));

        var repoTemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Repository", "GenericRepository.tpl");
        var ifaceTemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Repository", "GenericInterface.tpl");

        if (!System.IO.File.Exists(repoTemplatePath) || !System.IO.File.Exists(ifaceTemplatePath))
            return NotFound("No se encontraron las plantillas requeridas.");

        var repoTemplate = await System.IO.File.ReadAllTextAsync(repoTemplatePath);
        var ifaceTemplate = await System.IO.File.ReadAllTextAsync(ifaceTemplatePath);

        var repositoryCode = ProcessTemplate(repoTemplate);
        var interfaceCode = ProcessTemplate(ifaceTemplate);

        if (!request.AsZip)
        {
            return Content(repositoryCode + Environment.NewLine + Environment.NewLine + interfaceCode, "text/plain");
        }

        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var repoEntry = archive.CreateEntry($"{repositoryName}.cs");
            await using (var writer = new StreamWriter(repoEntry.Open()))
                await writer.WriteAsync(repositoryCode);

            var ifaceEntry = archive.CreateEntry($"{interfaceName}.cs");
            await using (var writer = new StreamWriter(ifaceEntry.Open()))
                await writer.WriteAsync(interfaceCode);
        }

        ms.Position = 0;
        return File(ms.ToArray(), "application/zip", $"{entityName}_Repository.zip");
    }

    private static string BuildParameterList(IEnumerable<DbColumn> pkColumns)
    {
        return string.Join(", ", pkColumns.Select(c =>
        {
            var (clrType, _) = SqlTypeMapper.ConvertToClrType(c.SqlType, c.Precision, c.Scale, c.Length, c.IsNullable);
            var varName = ToCamelCase(ToPascalCase(c.Name));
            return $"{clrType} {varName}";
        }));
    }

    private static string BuildEntityInitializer(string entityName, IEnumerable<DbColumn> pkColumns)
    {
        var assignments = string.Join(", ", pkColumns.Select(c =>
        {
            var propName = ToPascalCase(c.Name);
            var varName = ToCamelCase(propName);
            return $"{propName} = {varName}";
        }));
        return $"var entity = new {entityName} {{ {assignments} }};";
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p.ToLower())));
    }

    private static string ToCamelCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
}

public class RepositoryRequest
{
    public string TableName { get; set; } = string.Empty;
    [DefaultValue("dbo")]
    public string Schema { get; set; } = "dbo";
    [DefaultValue("Domain.Entities")]
    public string EntityNamespace { get; set; } = "Domain.Entities";
    [DefaultValue("Persistence.Repositories")]
    public string? RepositoryNamespace { get; set; } = "Persistence.Repositories";
    public bool AsZip { get; set; } = false;
}
