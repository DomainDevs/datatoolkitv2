using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using DataToolkit.Builder.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Linq;
using System.Globalization;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CQRSCommandsController : ControllerBase
{
    private readonly ScriptExtractionService _scriptExtractionService;

    public CQRSCommandsController(ScriptExtractionService scriptExtractionService)
    {
        _scriptExtractionService = scriptExtractionService;
    }

    // ===========================
    // CREATE
    // ===========================
    [HttpPost("create")]
    public async Task<IActionResult> GenerateCreate([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorCommands.GenerateCreateCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // UPDATE
    // ===========================
    [HttpPost("update")]
    public async Task<IActionResult> GenerateUpdate([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorCommands.GenerateUpdateCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // DELETE
    // ===========================
    [HttpPost("delete")]
    public async Task<IActionResult> GenerateDelete([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorCommands.GenerateDeleteCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // QUERY (GetById)
    // ===========================
    [HttpPost("query")]
    public async Task<IActionResult> GenerateQuery([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorCommands.GenerateQueryCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // QUERYALL (GetAll)
    // ===========================
    [HttpPost("queryall")]
    public async Task<IActionResult> GenerateQueryAll([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorCommands.GenerateQueryAllCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // -------------------------
    // Helpers para conversión de nombres
    // -------------------------
    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[i].ToLower());
        return string.Join("", parts);
    }

    private static string ToCamelCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        if (pascal.Length == 1) return pascal.ToLower();
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
}
