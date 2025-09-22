using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Text;
using System.Linq.Expressions;

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

        var entityNamespace = string.IsNullOrWhiteSpace(request.EntityNamespace)
            ? "Domain.Entities"
            : request.EntityNamespace;

        var repositoryNamespace = string.IsNullOrWhiteSpace(request.RepositoryNamespace)
            ? "Persistence.Repositories"
            : request.RepositoryNamespace;

        var repositoryName = $"{request.EntityName}Repository";

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

        // Clase
        sb.AppendLine($"    public class {repositoryName} : I{request.EntityName}Repository");
        sb.AppendLine("    {");
        sb.AppendLine($"        private readonly GenericRepository<{request.EntityName}> _repo;");
        sb.AppendLine();
        sb.AppendLine($"        public {repositoryName}(IDbConnection connection)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _repo = new GenericRepository<{request.EntityName}>(connection);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public Task<int> InsertAsync({request.EntityName} entity) => _repo.InsertAsync(entity);");
        sb.AppendLine();
        sb.AppendLine($"        public Task<int> UpdateAsync({request.EntityName} entity, params Expression<Func<{request.EntityName}, object>>[] includeProperties)");
        sb.AppendLine("            => _repo.UpdateAsync(entity, includeProperties);");
        sb.AppendLine();
        sb.AppendLine($"        public Task<int> DeleteAsync({request.EntityName} entity) => _repo.DeleteAsync(entity);");
        sb.AppendLine();
        sb.AppendLine($"        public Task<{request.EntityName}?> GetByIdAsync({request.EntityName} entity) => _repo.GetByIdAsync(entity);");
        sb.AppendLine();
        sb.AppendLine($"        public Task<IEnumerable<{request.EntityName}>> GetAllAsync() => _repo.GetAllAsync();");
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
