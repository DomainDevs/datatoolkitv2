using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceGeneratorController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptExtractionService;

        public ServiceGeneratorController(ScriptExtractionService scriptExtractionService)
        {
            _scriptExtractionService = scriptExtractionService;
        }

        // =========================================================
        // GENERAR INTERFAZ DE SERVICIO
        // =========================================================
        [HttpPost("create-interface")]
        public async Task<IActionResult> GenerateInterface([FromBody] ServiceGeneratorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableName))
                return BadRequest("Se requiere el nombre de la tabla (TableName).");

            if (string.IsNullOrWhiteSpace(request.DomainName))
                return BadRequest("Se requiere el nombre del dominio (DomainName).");

            var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

            var code = ServiceGenerator.GenerateInterface(
                table,
                request.DomainName,
                request.IncludeGetAll,
                request.IncludeGetById,
                request.IncludeCreate,
                request.IncludeUpdate,
                request.IncludeDelete
            );

            return Content(code, "text/plain");
        }

        // =========================================================
        // GENERAR IMPLEMENTACIÓN DEL SERVICIO
        // =========================================================
        [HttpPost("create-service")]
        public async Task<IActionResult> GenerateService([FromBody] ServiceGeneratorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableName))
                return BadRequest("Se requiere el nombre de la tabla (TableName).");

            if (string.IsNullOrWhiteSpace(request.DomainName))
                return BadRequest("Se requiere el nombre del dominio (DomainName).");

            var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

            var code = ServiceGenerator.GenerateImplementation(
                table,
                request.DomainName,
                request.IncludeGetAll,
                request.IncludeGetById,
                request.IncludeCreate,
                request.IncludeUpdate,
                request.IncludeDelete
            );

            return Content(code, "text/plain");
        }

        // =========================================================
        // GENERAR AMBOS (INTERFAZ + IMPLEMENTACIÓN)
        // =========================================================
        [HttpPost("create-bundle")]
        public async Task<IActionResult> GenerateBundle([FromBody] ServiceGeneratorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableName))
                return BadRequest("Se requiere el nombre de la tabla (TableName).");

            if (string.IsNullOrWhiteSpace(request.DomainName))
                return BadRequest("Se requiere el nombre del dominio (DomainName).");

            var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

            var interfaceCode = ServiceGenerator.GenerateInterface(
                table,
                request.DomainName,
                request.IncludeGetAll,
                request.IncludeGetById,
                request.IncludeCreate,
                request.IncludeUpdate,
                request.IncludeDelete
            );

            var implCode = ServiceGenerator.GenerateImplementation(
                table,
                request.DomainName,
                request.IncludeGetAll,
                request.IncludeGetById,
                request.IncludeCreate,
                request.IncludeUpdate,
                request.IncludeDelete
            );

            var combined = $"{interfaceCode}\n\n\n{implCode}";
            return Content(combined, "text/plain");
        }
    }
}
