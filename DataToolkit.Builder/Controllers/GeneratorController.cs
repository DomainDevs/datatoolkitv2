using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeneratorController : ControllerBase
{
    private readonly ControllerGeneratorService _service;

    public GeneratorController(ControllerGeneratorService service)
    {
        _service = service;
    }

    [HttpGet("controller/{schema}/{tableName}/{domainName}")]
    public async Task<IActionResult> GenerateController(
        string schema,
        string tableName,
        string domainName)
    {
        var code = await _service.GenerateController(schema, tableName, domainName);

        if (string.IsNullOrWhiteSpace(code))
            return NotFound($"No se pudo generar el controller para {schema}.{tableName}");

        return Content(code, "text/plain");
    }
}
