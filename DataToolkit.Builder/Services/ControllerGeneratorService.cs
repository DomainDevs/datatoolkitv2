using System.Text;
using System.Linq;
using DataToolkit.Builder.Helpers;
using DataToolkit.Builder.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Services
{
    public class ControllerGeneratorService
    {
        private readonly ScriptExtractionService _scriptExtractionService;

        public ControllerGeneratorService(ScriptExtractionService scriptExtractionService)
        {
            _scriptExtractionService = scriptExtractionService;
        }

        public async Task<string> GenerateController(string schema, string tableName, string domainName)
        {
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                throw new InvalidOperationException($"No se encontró la tabla {schema}.{tableName}.");

            var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (!pkColumns.Any())
                throw new InvalidOperationException($"La tabla {schema}.{tableName} no define columna PK.");

            var entityName = ToPascalCase(table.Name);
            var controllerName = $"{entityName}Controller";
            var requestDto = $"RequestDto"; //{entityName}
            var commandCreate = $"Create{entityName}Command";
            var queryAll = $"{entityName}GetAllQuery";
            var queryById = $"{entityName}GetByIdQuery";
            var commandUpdate = $"{entityName}UpdateCommand";
            var commandDelete = $"{entityName}DeleteCommand";

            // parámetros PK
            var pkParams = string.Join(", ", pkColumns.Select(c =>
            {
                var (clrType, _) = SqlTypeMapper.ConvertToClrType(c.SqlType, c.Precision, c.Scale, c.Length, c.IsNullable);
                return $"{clrType} {c.Name.ToLower()}";
            }));

            // route template
            var pkRoute = string.Join("/", pkColumns.Select(c => $"{{{c.Name.ToLower()}}}"));

            // argumentos para llamadas
            var pkArgs = string.Join(", ", pkColumns.Select(c => c.Name.ToLower()));

            // new { ... } en CreatedAtAction
            var pkNewObj = string.Join(", ", pkColumns.Select(c => $"{c.Name.ToLower()} = {c.Name.ToLower()}"));

            var sb = new StringBuilder();

            // usings
            sb.AppendLine("using MediatR;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine($"using Application.Features.{domainName}.Queries;");
            sb.AppendLine($"using Application.Features.{domainName}.Commands;");
            sb.AppendLine($"using Application.Features.{domainName}.DTOs;");
            sb.AppendLine($"using Application.Features.{domainName}.Mappers;");
            sb.AppendLine();
            sb.AppendLine("namespace API.Controllers;");
            sb.AppendLine();
            sb.AppendLine("[ApiController]");
            sb.AppendLine($"[Route(\"api/[controller]\")]");
            sb.AppendLine($"public class {controllerName} : ControllerBase");
            sb.AppendLine("{");
            sb.AppendLine("    private readonly IMediator _mediator;");
            sb.AppendLine();
            sb.AppendLine($"    public {controllerName}(IMediator mediator) => _mediator = mediator;");
            sb.AppendLine();

            // GetAll
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    // GET: api/{entityName}");
            sb.AppendLine("    // =====================================");
            sb.AppendLine("    [HttpGet]");
            sb.AppendLine($"    [ProducesResponseType(typeof(ResponseDTO<IEnumerable<{entityName}QueryResponseDto>>), StatusCodes.Status200OK)]");
            sb.AppendLine("    public async Task<IActionResult> GetAll()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var list = await _mediator.Send(new {queryAll}());");
            sb.AppendLine("        return Ok(ApiResponse.Success(list, \"Consulta exitosa\"));");
            sb.AppendLine("    }");
            sb.AppendLine();

            // GetById
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    // GET: api/{entityName}/{pkRoute}");
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    [HttpGet(\"{pkRoute}\")]");
            sb.AppendLine($"    [ProducesResponseType(typeof(ResponseDTO<IEnumerable<{entityName}QueryResponseDto>>), StatusCodes.Status200OK)]");
            sb.AppendLine($"    public async Task<IActionResult> GetById({pkParams})");
            sb.AppendLine("    {");
            sb.AppendLine($"        var item = await _mediator.Send(new {queryById}({pkArgs}));");
            sb.AppendLine("        if (item == null) return NotFound(ApiResponse.Fail<object>(\"Registro no encontrado\"));");
            sb.AppendLine("        return Ok(ApiResponse.Success(item, \"Registro encontrado\"));");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Create
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    // POST: api/{entityName}");
            sb.AppendLine("    // =====================================");
            sb.AppendLine("    [HttpPost]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status201Created)]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]");
            sb.AppendLine($"    public async Task<IActionResult> Create([FromBody] {entityName}Create{requestDto} dto)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!ModelState.IsValid) { ");
            sb.AppendLine("         var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(); ");
            sb.AppendLine("         return BadRequest(ApiResponse.Fail(\"Error de validación\", errors)); ");
            sb.AppendLine("        } ");
            sb.AppendLine($"        var command = dto.ToCommandCreate();");
            sb.AppendLine("        var result = await _mediator.Send(command);");

            if (pkColumns.Count == 1)
            {
                var pkName = pkColumns[0].Name.ToLower();
                sb.AppendLine($"        return CreatedAtAction(nameof(GetById), new {{ {pkName} = result }}, ApiResponse.Success(result, \"Registro creado correctamente\"));");
            }
            else
            {
                sb.AppendLine("        // Para PK compuestos se asume que el handler devuelve un objeto con todas las claves");
                sb.AppendLine("        return CreatedAtAction(nameof(GetById), result, ApiResponse.Success(result, \"Registro creado correctamente\"));");
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            // Update
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    // PUT: api/{entityName}/{pkRoute}");
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    [HttpPut(\"{pkRoute}\")]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status200OK)]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status404NotFound)]");
            sb.AppendLine($"    public async Task<IActionResult> Update({pkParams}, [FromBody] {entityName}Update{requestDto} dto)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!ModelState.IsValid) {");
            sb.AppendLine("         var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();");
            sb.AppendLine("         return BadRequest(ApiResponse.Fail(\"Error de validación\", errors));");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        //Inyección de llaves");
            foreach (var pk in pkColumns)
            {
                var paramName = pk.Name.ToLower();
                var propName = ToPascalCase(pk.Name);
                sb.AppendLine($"        dto.{propName} = {paramName};");
            }
            sb.AppendLine();
            sb.AppendLine($"        var command = dto.ToUpdateCommand();");
            sb.AppendLine("        var result = await _mediator.Send(command);");
            sb.AppendLine("        return result == 0 ? NotFound(ApiResponse.Fail<object>(\"Registro no encontrado\")) : Ok(ApiResponse.Success(result, \"Registro actualizado correctamente\"));");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Delete
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    // DELETE: api/{entityName}/{pkRoute}");
            sb.AppendLine("    // =====================================");
            sb.AppendLine($"    [HttpDelete(\"{pkRoute}\")]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status200OK)]");
            sb.AppendLine("    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status404NotFound)]");
            sb.AppendLine($"    public async Task<IActionResult> Delete({pkParams})");
            sb.AppendLine("    {");
            sb.AppendLine($"        var deleted = await _mediator.Send(new {commandDelete}({pkArgs}));");
            sb.AppendLine("        return !deleted ? NotFound(ApiResponse.Fail<object>(\"Registro no encontrado\")) : Ok(ApiResponse.Success(true, \"Registro eliminado correctamente\"));");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            return sb.ToString();
        }


        private string ToPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
        }
    }
}
