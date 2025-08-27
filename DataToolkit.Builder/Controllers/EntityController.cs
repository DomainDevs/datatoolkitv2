using Microsoft.AspNetCore.Mvc;
using DataToolkit.Builder.Services;
using DataToolkit.Builder.Models;
using DataToolkit.Library.Connections;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptExtractionService;
        private readonly EntityGenerator _entityGenerator;

        public EntityController(ScriptExtractionService scriptExtractionService, EntityGenerator entityGenerator)
        {
            _scriptExtractionService = scriptExtractionService;
            _entityGenerator = entityGenerator;
        }

        // GET api/entity/table?tableName=Departamento&schema=dbo&ns=DataToolkit.SampleApi.Models
        [HttpGet("table")]
        public async Task<IActionResult> GenerateTableEntity(
            [FromQuery] string tableName,
            [FromQuery] string schema = "dbo",
            [FromQuery(Name = "ns")] string namespaceName = "DataToolkit.SampleApi.Models")
        {
            var table = await _scriptExtractionService.ExtractTableMetadataAsync(schema, tableName);
            if (table is null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            var code = _entityGenerator.GenerateEntity(table, namespaceName);
            return Ok(new { code });
        }
    }
}
