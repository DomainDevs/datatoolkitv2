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
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using DataToolkit.Library.UnitOfWorkLayer;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {repositoryNamespace}");
        sb.AppendLine("{");

        // Clase
        sb.AppendLine($"    public class {repositoryName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IUnitOfWork _unitOfWork;");
        sb.AppendLine($"        private IGenericRepository<{request.EntityName}> Repo => _unitOfWork.GetRepository<{request.EntityName}>();");
        sb.AppendLine();
        sb.AppendLine("        public bool AutoCommit { get; set; } = true;");
        sb.AppendLine();
        sb.AppendLine($"        public {repositoryName}(IUnitOfWork unitOfWork)");
        sb.AppendLine("        {");
        sb.AppendLine("            _unitOfWork = unitOfWork;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // GetAllAsync
        sb.AppendLine($"        public Task<IEnumerable<{request.EntityName}>> GetAllAsync() => Repo.GetAllAsync();");
        sb.AppendLine();

        // GetByIdAsync
        sb.AppendLine($"        public Task<{request.EntityName}?> GetByIdAsync({request.EntityName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Repo.GetByIdAsync(entity);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // InsertAsync
        sb.AppendLine($"        public async Task<int> InsertAsync({request.EntityName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.InsertAsync(entity);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // UpdateAsync con includeProperties opcional
        sb.AppendLine($"        public async Task<int> UpdateAsync({request.EntityName} entity, Expression<Func<{request.EntityName}, object>>? includeProperties = null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.UpdateAsync(entity, includeProperties);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // DeleteAsync
        sb.AppendLine($"        public async Task<int> DeleteAsync({request.EntityName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine("            var result = await Repo.DeleteAsync(entity);");
        sb.AppendLine("            if (AutoCommit) _unitOfWork.Commit();");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");

        // Cierre clase y namespace
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
