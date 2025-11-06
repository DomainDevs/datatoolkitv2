using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CQRSValidatorController : ControllerBase
{
    private readonly ScriptExtractionService _scriptExtractionService;

    public CQRSValidatorController(ScriptExtractionService scriptExtractionService)
    {
        _scriptExtractionService = scriptExtractionService;
    }

    // ===========================
    // CREATE VALIDATOR
    // ===========================
    [HttpPost("create")]
    public async Task<IActionResult> GenerateCreateValidator([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorValidator.GenerateCreateCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // UPDATE VALIDATOR
    // ===========================
    [HttpPost("update")]
    public async Task<IActionResult> GenerateUpdateValidator([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorValidator.GenerateUpdateCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // ===========================
    // DELETE VALIDATOR
    // ===========================
    [HttpPost("delete")]
    public async Task<IActionResult> GenerateDeleteValidator([FromBody] CQRSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest("Se requiere el nombre de la tabla (TableName).");

        if (string.IsNullOrWhiteSpace(request.DomainName))
            return BadRequest("Se requiere el nombre del dominio (DomainName).");

        var schema = string.IsNullOrWhiteSpace(request.Schema) ? "dbo" : request.Schema;
        var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, request.TableName);
        if (table == null)
            return NotFound($"No se encontró metadata para {schema}.{request.TableName}");

        var code = CQRSGeneratorValidator.GenerateDeleteCode(table, request.DomainName);
        return Content(code, "text/plain");
    }

    // -------------------------
    // Helpers de nombres
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
