using DataToolkit.Builder.Services;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScriptController : ControllerBase
{
    private readonly ScriptExtractionService _scriptService;

    public ScriptController(ScriptExtractionService scriptService)
    {
        _scriptService = scriptService;
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetScript(
        [FromQuery] string objectName,
        [FromQuery] string objectType,
        [FromQuery] string objectSchema = "dbo",
        [FromQuery] DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(objectType) || string.IsNullOrWhiteSpace(objectSchema))
            return BadRequest("objectName, objectType y objectSchema son requeridos.");

        var scriptSQL = await _scriptService.GetCreateScriptAsync(objectName, objectType, objectSchema, provider);
        if (scriptSQL == null)
            return NotFound("No se encontró el script para el objeto especificado.");

        return Ok(new { scriptSQL });
    }
}
