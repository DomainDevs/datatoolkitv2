using DataToolkit.Builder.Services;
using DataToolkit.Library.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoredProcedureController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptExtractionService;

        public StoredProcedureController(ScriptExtractionService scriptExtractionService)
        {
            _scriptExtractionService = scriptExtractionService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateStoredProcedure([FromBody] StoredProcedureRequest request)
        {

            return null;
        }

    }

    public class StoredProcedureRequest
    {
        public string StoredName { get; set; } = string.Empty;
        public string Schema { get; set; } = "dbo";
    }


}


