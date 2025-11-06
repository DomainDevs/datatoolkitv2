using DataToolkit.Builder.Models;
using System.Text;
using System.Linq;
using DataToolkit.Builder.Helpers;
using static System.Net.Mime.MediaTypeNames;

namespace DataToolkit.Builder.Services
{
    public static class CQRSGeneratorHandler
    {
        public static string GenerateCreateCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            var commandName = $"{entityName}CreateCommand";
            var handlerName = $"{entityName}CreateHandler";
            var responseDto = $"{entityName}ResponseDto";

            var sb = new StringBuilder();

            // -------------------------
            // Handler
            // -------------------------
            sb.AppendLine($"// {handlerName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine($"using Application.Features.{domain}.Mappers;");
            sb.AppendLine("using Entities = Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Handlers;");
            sb.AppendLine();
            sb.AppendLine($"public class {handlerName} : IRequestHandler<{commandName}, int>");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entityName}Repository _repo;");
            sb.AppendLine($"    public {handlerName}(I{entityName}Repository repo) => _repo = repo;");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<int> Handle({commandName} request, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = {entityName}Mapper.ToEntity(request);");
            sb.AppendLine("        return await _repo.InsertAsync(entity);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateUpdateCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            var commandName = $"{entityName}UpdateCommand";
            var handlerName = $"{entityName}UpdateHandler";

            var sb = new StringBuilder();

            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            var nonPkColumns = table.Columns.Where(c => !c.IsPrimaryKey).ToList();

            // -------------------------
            // Handler
            // -------------------------
            sb.AppendLine($"// {handlerName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine($"using Application.Features.{domain}.Mappers;");
            sb.AppendLine("using Entities = Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Handlers;");
            sb.AppendLine();
            sb.AppendLine($"public class {handlerName} : IRequestHandler<{commandName}, int>");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entityName}Repository _repo;");
            sb.AppendLine($"    public {handlerName}(I{entityName}Repository repo) => _repo = repo;");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<int> Handle({commandName} request, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = {entityName}Mapper.ToEntity(request);");
            sb.AppendLine("        return await _repo.UpdateAsync(entity); //Specify fields => ...UpdateAsync(entity, c => c.Field1, c => c.Field2);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateDeleteCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            var commandName = $"{entityName}DeleteCommand";
            var handlerName = $"{entityName}DeleteHandler";
            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();

            // -------------------------
            // Handler
            // -------------------------
            sb.AppendLine($"// {handlerName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Handlers;");
            sb.AppendLine();
            sb.AppendLine($"public class {handlerName} : IRequestHandler<{commandName}, bool>");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entityName}Repository _repo;");
            sb.AppendLine($"    public {handlerName}(I{entityName}Repository repo) => _repo = repo;");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<bool> Handle({commandName} request, CancellationToken cancellationToken)");
            sb.AppendLine($"        => (await _repo.DeleteByIdAsync({string.Join(", ", pkColumns.Select(c => $"request.{ToPascalCase(c.Name)}"))})) > 0;");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateQueryCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            var queryName = $"{entityName}GetByIdQuery";
            var handlerName = $"{entityName}GetByIdHandler";
            var responseDto = $"{entityName}QueryResponseDto";
            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();

            // Handler
            sb.AppendLine($"// {handlerName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine($"using Application.Features.{domain}.DTOs;");
            sb.AppendLine($"using Application.Features.{domain}.Mappers;");
            sb.AppendLine($"using Application.Features.{domain}.Queries;");
            sb.AppendLine("using Entities = Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Handlers;");
            sb.AppendLine();
            sb.AppendLine($"public class {handlerName} : IRequestHandler<{queryName}, {responseDto}?>");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entityName}Repository _repo;");
            sb.AppendLine($"    public {handlerName}(I{entityName}Repository repo) => _repo = repo;");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<{responseDto}?> Handle({queryName} request, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = await _repo.GetByIdAsync({string.Join(", ", pkColumns.Select(c => $"request.{ToPascalCase(c.Name)}"))});");
            sb.AppendLine("        if (entity == null) return null;");
            sb.AppendLine($"        return {entityName}Mapper.ToDto(entity);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateQueryAllCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            var queryName = $"{entityName}GetAllQuery";
            var handlerName = $"{entityName}GetAllHandler";
            var responseDto = $"{entityName}QueryResponseDto";

            var sb = new StringBuilder();

            // Handler
            sb.AppendLine($"// {handlerName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine($"using Application.Features.{domain}.DTOs;");
            sb.AppendLine($"using Application.Features.{domain}.Queries;");
            sb.AppendLine($"using Application.Features.{domain}.Mappers;");
            sb.AppendLine("using Entities = Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Handlers;");
            sb.AppendLine();
            sb.AppendLine($"public class {handlerName} : IRequestHandler<{queryName}, IEnumerable<{responseDto}>>");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entityName}Repository _repo;");
            sb.AppendLine($"    public {handlerName}(I{entityName}Repository repo) => _repo = repo;");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<IEnumerable<{responseDto}>> Handle({queryName} request, CancellationToken ct)");
            sb.AppendLine($"        => (await _repo.GetAllAsync()).Select(entity => {entityName}Mapper.ToDto(entity));");
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
            return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
        }

        private static string MapClrType(DbColumn col)
        {
            return SqlTypeMapper.ConvertToClrType(col.SqlType, col.Precision, col.Scale, col.Length, col.IsNullable).ClrType;
        }
    }
}
