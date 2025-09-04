namespace DataToolkit.Builder.Models;

public class MapperUploadRequest
{
    // Archivo de la Entity
    public IFormFile EntityFile { get; set; } = default!;

    // Archivo del DTO
    public IFormFile DtoFile { get; set; } = default!;
}
