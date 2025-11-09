namespace DataToolkit.Builder.Models;

// Clase para recibir la petición
public class ServiceGeneratorRequest
{
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string DomainName { get; set; } = string.Empty;

    // 🔹 Marcas de control para generar solo los métodos deseados
    public bool IncludeGetAll { get; set; } = true;
    public bool IncludeGetById { get; set; } = true;
    public bool IncludeCreate { get; set; } = true;
    public bool IncludeUpdate { get; set; } = true;
    public bool IncludeDelete { get; set; } = true;

}