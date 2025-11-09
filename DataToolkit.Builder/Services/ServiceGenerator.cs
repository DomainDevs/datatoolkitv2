using DataToolkit.Builder.Models;
using System.Text;
using System.Linq;
using DataToolkit.Builder.Helpers;

namespace DataToolkit.Builder.Services
{
    public static class ServiceGenerator
    {
        // =====================================================
        // Genera la interfaz de servicio (sin CQRS, con Mapperly)
        // =====================================================
        public static string GenerateInterface(
            DbTable table,
            string domainName,
            bool includeGetAll,
            bool includeGetById,
            bool includeCreate,
            bool includeUpdate,
            bool includeDelete)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            // Nombres de DTO por operación
            var createDto = $"{entityName}CreateRequestDto";
            var queryDto = $"{entityName}QueryResponseDto";
            var updateDto = $"{entityName}UpdateRequestDto";

            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            var pkType = pkColumns.Any()
                ? MapClrType(pkColumns.First())
                : "int";

            var sb = new StringBuilder();

            sb.AppendLine($"// I{entityName}Service.cs");
            sb.AppendLine($"namespace Application.Features.{domain}.Services;");
            sb.AppendLine();
            sb.AppendLine($"public interface I{entityName}Service");
            sb.AppendLine("{");

            if (includeGetAll)
                sb.AppendLine($"    Task<IEnumerable<{queryDto}>> GetAllAsync();");

            if (includeGetById)
                sb.AppendLine($"    Task<{queryDto}?> GetByIdAsync({pkType} id);");

            if (includeCreate)
                sb.AppendLine($"    Task<int> CreateAsync({createDto} dto);");

            if (includeUpdate)
                sb.AppendLine($"    Task UpdateAsync({updateDto} dto);");

            if (includeDelete)
                sb.AppendLine($"    Task<bool> DeleteAsync({pkType} id);");

            sb.AppendLine("}");

            return sb.ToString();
        }

        // =====================================================
        // Genera la implementación del servicio (sin CQRS, con Mapperly)
        // =====================================================
        public static string GenerateImplementation(
            DbTable table,
            string domainName,
            bool includeGetAll,
            bool includeGetById,
            bool includeCreate,
            bool includeUpdate,
            bool includeDelete)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);

            // Nombres de DTO por operación
            var createDto = $"{entityName}CreateRequestDto";
            var queryDto = $"{entityName}QueryResponseDto";
            var updateDto = $"{entityName}UpdateRequestDto";
            var mapperName = $"{entityName}Mapper";

            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            var pkType = pkColumns.Any()
                ? MapClrType(pkColumns.First())
                : "int";

            var sb = new StringBuilder();

            sb.AppendLine($"// {entityName}Service.cs");
            sb.AppendLine("using Domain.Interfaces;");
            sb.AppendLine($"using Application.Features.{domain}.DTOs;");
            sb.AppendLine($"using Application.Features.{domain}.Mappers;");
            sb.AppendLine("using Entities = Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {entityName}Service : I{entityName}Service");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly I{entityName}Repository _repo;");
            sb.AppendLine();
            sb.AppendLine($"        public {entityName}Service(I{entityName}Repository repo)");
            sb.AppendLine("        {");
            sb.AppendLine("            _repo = repo;");
            sb.AppendLine("        }");
            sb.AppendLine();

            if (includeGetAll)
            {
                sb.AppendLine($"        public async Task<IEnumerable<{queryDto}>> GetAllAsync()");
                sb.AppendLine("        {");
                sb.AppendLine($"            var entities = await _repo.GetAllAsync();");
                sb.AppendLine($"            return entities.Select(e => {mapperName}.ToDto(e));");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            if (includeGetById)
            {
                sb.AppendLine($"        public async Task<{queryDto}?> GetByIdAsync({pkType} id)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var entity = await _repo.GetByIdAsync(id);");
                sb.AppendLine("            if (entity == null) return null;");
                sb.AppendLine();
                sb.AppendLine($"            return {mapperName}.ToDto(entity);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            if (includeCreate)
            {
                sb.AppendLine($"        public async Task<int> CreateAsync({createDto} dto)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var entity = {mapperName}.ToEntity(dto);");
                sb.AppendLine("            return await _repo.InsertAsync(entity);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            if (includeUpdate)
            {
                sb.AppendLine($"        public async Task UpdateAsync({updateDto} dto)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var entity = {mapperName}.ToEntity(dto);");
                sb.AppendLine("            await _repo.UpdateAsync(entity);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            if (includeDelete)
            {
                sb.AppendLine($"        public async Task<bool> DeleteAsync({pkType} id)");
                sb.AppendLine("        {");
                sb.AppendLine("            var affected = await _repo.DeleteByIdAsync(id);");
                sb.AppendLine("            return affected > 0;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // =====================================================
        // Helpers
        // =====================================================
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
