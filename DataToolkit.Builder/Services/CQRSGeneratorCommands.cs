using DataToolkit.Builder.Models;
using System.Text;
using System.Linq;
using DataToolkit.Builder.Helpers;
using static System.Net.Mime.MediaTypeNames;

namespace DataToolkit.Builder.Services
{
    public static class CQRSGeneratorCommands
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
            // Command
            // -------------------------
            sb.AppendLine($"// {commandName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"namespace Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.Append($"public record {commandName}(");
            sb.Append(string.Join(", ", table.Columns.Select(c => $"{MapClrType(c)} {ToPascalCase(c.Name)}")));
            sb.AppendLine(") : IRequest<int>;");
            sb.AppendLine();

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
            // Command
            // -------------------------
            sb.AppendLine($"// {commandName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"namespace Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.Append($"public record {commandName}(");
            sb.Append(string.Join(", ", pkColumns.Concat(nonPkColumns)
                .Select(c => $"{MapClrType(c)} {ToPascalCase(c.Name)}")));
            sb.AppendLine(") : IRequest<int>;");
            sb.AppendLine();


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
            // Command
            // -------------------------
            sb.AppendLine($"// {commandName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"namespace Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.AppendLine($"public record {commandName}({string.Join(", ", pkColumns.Select(c => $"{MapClrType(c)} {ToPascalCase(c.Name)}"))}) : IRequest<bool>;");
            sb.AppendLine();

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

            // Query
            sb.AppendLine($"// {queryName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"using Application.Features.{domain}.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Queries;");
            sb.AppendLine();
            sb.Append($"public record {queryName}(");
            sb.Append(string.Join(", ", pkColumns.Select(c => $"{MapClrType(c)} {ToPascalCase(c.Name)}")));
            sb.AppendLine($") : IRequest<{responseDto}?>;");
            sb.AppendLine();

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

            // QueryAll
            sb.AppendLine($"// {queryName}.cs");
            sb.AppendLine("using MediatR;");
            sb.AppendLine($"using Application.Features.{domain}.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Queries;");
            sb.AppendLine();
            sb.AppendLine($"public record {queryName}() : IRequest<IEnumerable<{responseDto}>>;");
            sb.AppendLine();

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
