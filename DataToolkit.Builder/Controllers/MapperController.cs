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
        var mapperCode = MapperGenerator.Generate(entityContent, dtoContent, request.UseMapperly);

        return Content(mapperCode, "text/plain");
    }
}