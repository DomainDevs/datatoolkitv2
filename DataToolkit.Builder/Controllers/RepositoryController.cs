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
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateRepository([FromBody] RepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        // Nombres/espacios
        var entityNamespace = string.IsNullOrWhiteSpace(request.EntityNamespace)
            ? "Domain.Entities"
            : request.EntityNamespace;

        var repositoryNamespace = string.IsNullOrWhiteSpace(request.RepositoryNamespace)
            ? "Persistence.Repositories"
            : request.RepositoryNamespace;

        var entityName = ToPascalCase(table.Name); // nombre de la entidad generado a partir del nombre de la tabla
        var repositoryName = $"{entityName}Repository";
        var interfaceName = $"I{entityName}Repository";

        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using DataToolkit.Library.Repositories;");
        sb.AppendLine($"using {entityNamespace};");
        sb.AppendLine("using Domain.Interfaces;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {repositoryNamespace}");
        sb.AppendLine("{");

        // Interfaz (opcional: genera una interfaz compatible)
        sb.AppendLine($"    // Interfaz I{entityName}Repository (autogenerada)");
        sb.AppendLine($"    public interface {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        Task<int> InsertAsync({entityName} entity);");
        sb.AppendLine($"        Task<int> UpdateAsync({entityName} entity, params Expression<Func<{entityName}, object>>[] includeProperties);");

        var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

        if (!pkColumns.Any())
        {
            // sin PK: fallback a métodos que reciben la entidad
            sb.AppendLine($"        Task<int> DeleteAsync({entityName} entity);");
            sb.AppendLine($"        Task<{entityName}?> GetByIdAsync({entityName} entity);");
        }
        else
        {
            // PK simples o compuestas: generamos firmas con argumentos por cada PK
            var paramList = BuildParameterList(pkColumns);
            sb.AppendLine($"        Task<int> DeleteByIdAsync({paramList});");
            sb.AppendLine($"        Task<{entityName}?> GetByIdAsync({paramList});");
        }

        sb.AppendLine($"        Task<IEnumerable<{entityName}>> GetAllAsync();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Clase
        sb.AppendLine($"    public class {repositoryName} : {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        private readonly GenericRepository<{entityName}> _repo;");
        sb.AppendLine();
        sb.AppendLine($"        public {repositoryName}(IDbConnection connection)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _repo = new GenericRepository<{entityName}>(connection);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public Task<int> InsertAsync({entityName} entity) => _repo.InsertAsync(entity);");
        sb.AppendLine();
        sb.AppendLine($"        public Task<int> UpdateAsync({entityName} entity, params Expression<Func<{entityName}, object>>[] includeProperties)");
        sb.AppendLine("            => _repo.UpdateAsync(entity, includeProperties);");
        sb.AppendLine();

        if (!pkColumns.Any())
        {
            // sin PK: DeleteAsync / GetByIdAsync con entidad
            sb.AppendLine($"        public Task<int> DeleteAsync({entityName} entity) => _repo.DeleteAsync(entity);");
            sb.AppendLine();
            sb.AppendLine($"        public Task<{entityName}?> GetByIdAsync({entityName} entity) => _repo.GetByIdAsync(entity);");
            sb.AppendLine();
        }
        else
        {
            // DeleteByIdAsync + GetByIdAsync con parámetros por cada PK
            var paramList = BuildParameterList(pkColumns);
            var initObject = BuildEntityInitializer(entityName, pkColumns);

            // DeleteByIdAsync
            sb.AppendLine($"        public async Task<int> DeleteByIdAsync({paramList})");
            sb.AppendLine("        {");
            sb.AppendLine($"            {initObject}");
            sb.AppendLine("            return await _repo.DeleteAsync(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetByIdAsync
            sb.AppendLine($"        public Task<{entityName}?> GetByIdAsync({paramList})");
            sb.AppendLine("        {");
            sb.AppendLine($"            {initObject}");
            sb.AppendLine("            return _repo.GetByIdAsync(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // GetAll
        sb.AppendLine($"        public Task<IEnumerable<{entityName}>> GetAllAsync() => _repo.GetAllAsync();");

        sb.AppendLine("    }"); // end class
        sb.AppendLine("}"); // end namespace

        return Content(sb.ToString(), "text/plain");
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
            var (clrType, _) = SqlTypeMapper.ConvertToClrType(col.SqlType, col.Precision, col.Scale, col.IsNullable);
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
 