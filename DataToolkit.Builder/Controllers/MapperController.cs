using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapperController : ControllerBase
{
    // Método 1: subir archivos desde la web (Swagger)
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFiles([FromForm] MapperUploadRequest request)
    {
        if (request.EntityFile == null || request.DtoFile == null)
            return BadRequest("Debe subir ambos archivos: Entity y DTO.");

        string entityContent;
        string dtoContent;

        using (var reader = new StreamReader(request.EntityFile.OpenReadStream()))
            entityContent = await reader.ReadToEndAsync();

        using (var reader = new StreamReader(request.DtoFile.OpenReadStream()))
            dtoContent = await reader.ReadToEndAsync();

        // Generar el mapper
        var mapperCode = MapperGenerator.Generate(entityContent, dtoContent);

        return Content(mapperCode, "text/plain");
    }

    // Método 2: usar archivos desde una ruta local
    [HttpPost("local")]
    public IActionResult FromLocalFiles([FromQuery] string entityPath, [FromQuery] string dtoPath)
    {
        if (!System.IO.File.Exists(entityPath) || !System.IO.File.Exists(dtoPath))
            return BadRequest("Uno de los archivos no existe.");

        var entityContent = System.IO.File.ReadAllText(entityPath);
        var dtoContent = System.IO.File.ReadAllText(dtoPath);

        // Generar el mapper
        var mapperCode = MapperGenerator.Generate(entityContent, dtoContent);

        return Content(mapperCode, "text/plain");
    }
}