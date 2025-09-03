using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptService;
        private readonly EntityGenerator _entityGenerator;

        public EntityController(ScriptExtractionService scriptService, EntityGenerator entityGenerator)
        {
            _scriptService = scriptService;
            _entityGenerator = entityGenerator;
        }

        [HttpGet("table_metadata")]
        public async Task<IActionResult> GetTable([FromQuery] string schema = "dbo", [FromQuery] string tableName = "")
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
                return BadRequest("Schema y TableName son requeridos.");

            var table = await _scriptService.ExtractTableMetadataAsync(schema, tableName);

            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            // Devuelve la metadata en JSON
            return Ok(table);
        }

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateEntity([FromQuery] string schema = "dbo", [FromQuery] string tableName = "")
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
                return BadRequest("Schema y TableName son requeridos.");

            var table = await _scriptService.ExtractTableMetadataAsync(schema, tableName);

            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            var entityCode = _entityGenerator.GenerateEntity(table);

            // Devuelve el código C# como texto
            return Ok(entityCode);
        }
    }
}
