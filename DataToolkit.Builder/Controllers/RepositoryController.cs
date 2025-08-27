using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RepositoryController : ControllerBase
{
    private readonly EntityGenerator _entityGenerator;

    public RepositoryController(EntityGenerator entityGenerator)
    {
        _entityGenerator = entityGenerator;
    }

    [HttpPost("generate")]
    public IActionResult GenerateRepository([FromBody] RepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return BadRequest("Se requiere el nombre de la entidad.");

        // Valores por defecto si no se envían
        var entityNamespace = string.IsNullOrWhiteSpace(request.EntityNamespace)
            ? "Domain.Entities"
            : request.EntityNamespace;

        var repositoryNamespace = string.IsNullOrWhiteSpace(request.RepositoryNamespace)
            ? "Persistence.Repositories"
            : request.RepositoryNamespace;

        // Genera el nombre del repositorio
        var repositoryName = $"{request.EntityName}Repository";

        // Genera el código usando plantilla simple
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("using DataToolkit.Library.Repositories;");
        sb.AppendLine($"using {entityNamespace};");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {repositoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public class {repositoryName}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IUnitOfWork _unitOfWork;");
        sb.AppendLine();
        sb.AppendLine("    public bool AutoCommit { get; set; } = true;");
        sb.AppendLine();
        sb.AppendLine($"    public {repositoryName}(IUnitOfWork unitOfWork)");
        sb.AppendLine("    {");
        sb.AppendLine("        _unitOfWork = unitOfWork;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private GenericRepository<{request.EntityName}> Repo => _unitOfWork.GetRepository<{request.EntityName}>();");
        sb.AppendLine();
        sb.AppendLine("    public Task<IEnumerable<" + request.EntityName + ">> GetAllAsync() => Repo.GetAllAsync();");
        sb.AppendLine();
        sb.AppendLine("    public Task<" + request.EntityName + "?> GetByIdAsync(int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var keys = new Dictionary<string, object> { { \"Id\", id } };");
        sb.AppendLine("        return Repo.GetByIdAsync(keys);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task<int> InsertAsync(" + request.EntityName + " entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await Repo.InsertAsync(entity);");
        sb.AppendLine("        if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task<int> UpdateAsync(" + request.EntityName + " entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await Repo.UpdateAsync(entity);");
        sb.AppendLine("        if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task<int> DeleteAsync(int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var keys = new Dictionary<string, object> { { \"Id\", id } };");
        sb.AppendLine("        var result = await Repo.DeleteAsync(keys);");
        sb.AppendLine("        if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return Content(sb.ToString(), "text/plain");
    }
}

// Clase para recibir la petición
public class RepositoryRequest
{
    public string EntityName { get; set; } = string.Empty;

    [DefaultValue("Domain.Entities")]
    public string EntityNamespace { get; set; } = "Domain.Entities";

    [DefaultValue("Persistence.Repositories")]
    public string? RepositoryNamespace { get; set; } = "Persistence.Repositories";
}