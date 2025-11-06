using DataToolkit.Builder.Models;
using System.Text;
using System.Linq;
using DataToolkit.Builder.Helpers;

namespace DataToolkit.Builder.Services
{
    public static class CQRSGeneratorValidator
    {
        public static string GenerateCreateCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);
            var commandName = $"{entityName}CreateCommand";
            var validatorName = $"{entityName}CreateValidator";

            var sb = new StringBuilder();

            // -------------------------
            // Validator
            // -------------------------
            sb.AppendLine($"// {validatorName}.cs");
            sb.AppendLine("using FluentValidation;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Validators;");
            sb.AppendLine();
            sb.AppendLine($"public class {validatorName} : AbstractValidator<{commandName}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public {validatorName}()");
            sb.AppendLine("    {");

            foreach (var col in table.Columns)
            {
                var property = ToPascalCase(col.Name);
                var type = MapClrType(col);

                // Ejemplo de generación automática básica
                if (!col.IsNullable)
                {
                    sb.AppendLine($"        RuleFor(x => x.{property})");
                    sb.AppendLine($"            .NotEmpty().WithMessage(\"El campo {property} es obligatorio.\");");
                }

                // Agrega validaciones adicionales según tipo
                if (type == "string" && col.Length > 0)
                {
                    sb.AppendLine($"        RuleFor(x => x.{property})");
                    sb.AppendLine($"            .MaximumLength({col.Length}).WithMessage(\"El campo {property} no puede exceder {col.Length} caracteres.\");");
                }

                if (type == "int" || type == "decimal" || type == "long")
                {
                    sb.AppendLine($"        RuleFor(x => x.{property})");
                    sb.AppendLine($"            .GreaterThanOrEqualTo(0).WithMessage(\"El campo {property} debe ser mayor o igual a 0.\");");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateUpdateCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);
            var commandName = $"{entityName}UpdateCommand";
            var validatorName = $"{entityName}UpdateValidator";

            var sb = new StringBuilder();

            // -------------------------
            // Validator
            // -------------------------
            sb.AppendLine($"// {validatorName}.cs");
            sb.AppendLine("using FluentValidation;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Validators;");
            sb.AppendLine();
            sb.AppendLine($"public class {validatorName} : AbstractValidator<{commandName}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public {validatorName}()");
            sb.AppendLine("    {");

            // Validaciones base (igual que en Create)
            foreach (var col in table.Columns)
            {
                var property = ToPascalCase(col.Name);
                var type = MapClrType(col);

                if (!col.IsNullable)
                {
                    sb.AppendLine($"        RuleFor(x => x.{property})");
                    sb.AppendLine($"            .NotEmpty().WithMessage(\"El campo {property} es obligatorio.\");");
                }

                if (type == "string" && col.Length > 0)
                {
                    sb.AppendLine($"        RuleFor(x => x.{property})");
                    sb.AppendLine($"            .MaximumLength({col.Length}).WithMessage(\"El campo {property} no puede exceder {col.Length} caracteres.\");");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GenerateDeleteCode(DbTable table, string domainName)
        {
            var entityName = ToPascalCase(table.Name);
            var domain = ToPascalCase(domainName);
            var commandName = $"{entityName}DeleteCommand";
            var validatorName = $"{entityName}DeleteValidator";

            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

            var sb = new StringBuilder();

            // -------------------------
            // Validator
            // -------------------------
            sb.AppendLine($"// {validatorName}.cs");
            sb.AppendLine("using FluentValidation;");
            sb.AppendLine($"using Application.Features.{domain}.Commands;");
            sb.AppendLine();
            sb.AppendLine($"namespace Application.Features.{domain}.Validators;");
            sb.AppendLine();
            sb.AppendLine($"public class {validatorName} : AbstractValidator<{commandName}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public {validatorName}()");
            sb.AppendLine("    {");

            foreach (var pk in pkColumns)
            {
                var property = ToPascalCase(pk.Name);
                sb.AppendLine($"        RuleFor(x => x.{property})");
                sb.AppendLine($"            .GreaterThan(0).WithMessage(\"El identificador {property} debe ser mayor que cero.\");");
            }

            sb.AppendLine("    }");
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
