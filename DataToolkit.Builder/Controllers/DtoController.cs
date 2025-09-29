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
        /*
        // ===========================
        // CREATE
        // ===========================
        */
        // schema opcional, por defecto "dbo"
        //[HttpGet("{domainName}/{tableName}")]
        [HttpGet("create")]
        public async Task<IActionResult> GenerateCreateDto(
            string domainName,
            string tableName,
            string mode = "request",
            string schema = "dbo")
        {
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                return NotFound($"Table {schema}.{tableName} not found");

            var code = _dtoGeneratorService.GenerateDto(table, domainName, mode.ToLower(), "Create");

            return Ok(code);
        }

        //public async Task<IActionResult> GenerateCreate([FromBody] CQRSRequest request)
        // ===========================
        // UPDATE
        // ===========================
        [HttpGet("update")]
        public async Task<IActionResult> GenerateUpdateDto(
            string domainName,
            string tableName,
            string mode = "request",
            string schema = "dbo")
        {
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                return NotFound($"Table {schema}.{tableName} not found");

            var code = _dtoGeneratorService.GenerateDto(table, domainName, mode.ToLower(), "Update");

            return Ok(code);
        }

        // ===========================
        // QUERY
        // ===========================
        [HttpGet("query")]
        public async Task<IActionResult> GenerateQueryDto(
            string domainName,
            string tableName,
            string mode = "response",
            string schema = "dbo")
        {
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table == null)
                return NotFound($"Table {schema}.{tableName} not found");

            var code = _dtoGeneratorService.GenerateDto(table, domainName, mode.ToLower(), "Query");

            return Ok(code);
        }
    }
}
