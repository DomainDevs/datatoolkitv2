using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DtoController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptExtractionService;
        private readonly DtoGeneratorService _dtoGeneratorService;

        public DtoController(
            ScriptExtractionService scriptExtractionService,
            DtoGeneratorService dtoGeneratorService)
        {
            _scriptExtractionService = scriptExtractionService;
            _dtoGeneratorService = dtoGeneratorService;
        }

        // schema opcional, por defecto "dbo"
        [HttpGet("{domainName}/{tableName}")]
        public async Task<IActionResult> GenerateDto(
            string domainName,
            string tableName,
            string mode = "request",
            string schema = "dbo")
        {
            // Extraer metadata
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                return NotFound($"Table {schema}.{tableName} not found");

            // Generar DTO según el modo, pasando también el domainName
            var code = _dtoGeneratorService.GenerateDto(table, domainName, mode.ToLower());

            return Ok(code);
        }
    }
}
