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
        [HttpGet("{tableName}")]
        public async Task<IActionResult> GenerateDto(
            string tableName,
            string mode = "request",
            string schema = "dbo") // valor por defecto
        {
            // Extraer metadata
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                return NotFound($"Table {schema}.{tableName} not found");

            // Generar DTO según el modo
            var code = _dtoGeneratorService.GenerateDto(table, mode.ToLower());

            return Ok(code);
        }
    }
}
