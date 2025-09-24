using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapperController : ControllerBase
{
    private readonly ScriptExtractionService _scriptExtractionService;

    public MapperController(ScriptExtractionService scriptExtractionService)
    {
        _scriptExtractionService = scriptExtractionService;
    }

    /// <summary>
    /// Genera el mapper para la tabla indicada usando la metadata real (no upload).
    /// Request debe traer: Schema (opcional), TableName y DomainName.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] MapperRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere TableName.");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere DomainName.");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        // Generar mapper a partir de metadata + domainName
        var mapperCode = MapperGenerator.Generate(table, request.DomainName, request.UseMapperly);

        return Content(mapperCode, "text/plain");
    }
}
