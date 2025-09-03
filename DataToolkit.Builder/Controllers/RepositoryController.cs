using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Text;

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
        sb.AppendLine("using DataToolkit.Library.Repositories;");
        sb.AppendLine($"using {entityNamespace};");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {repositoryNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {repositoryName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IUnitOfWork _unitOfWork;");
        sb.AppendLine($"        private GenericRepository<{request.EntityName}, {request.EntityName}.Key> Repo => _unitOfWork.GetRepository<{request.EntityName}, {request.EntityName}.Key>();");
        sb.AppendLine();
        sb.AppendLine("        public bool AutoCommit { get; set; } = true;");
        sb.AppendLine();
        sb.AppendLine($"        public {repositoryName}(IUnitOfWork unitOfWork)");
        sb.AppendLine("        {");
        sb.AppendLine("            _unitOfWork = unitOfWork;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public Task<IEnumerable<{request.EntityName}>> GetAllAsync() => Repo.GetAllAsync();");
        sb.AppendLine();
        sb.AppendLine($"        public Task<{request.EntityName}?> GetByIdAsync({request.EntityName}.Key key) => Repo.GetByIdAsync(key);");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<int> InsertAsync({request.EntityName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.InsertAsync(entity);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<int> UpdateAsync({request.EntityName} entity, {request.EntityName}.Key key)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.UpdateAsync(entity, key);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<int> DeleteAsync({request.EntityName}.Key key)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.DeleteAsync(key);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
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
