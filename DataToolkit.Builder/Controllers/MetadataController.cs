using DataToolkit.Builder.Services;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetadataController : ControllerBase
{
    private readonly MetadataService _metadataService;

    public MetadataController(MetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    [HttpGet("objects")]
    public async Task<IActionResult> GetAllObjects([FromQuery] DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        try
        {
            var result = await _metadataService.GetDatabaseObjectsAsync(provider);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener objetos de base de datos", error = ex.Message });
        }
    }
}
