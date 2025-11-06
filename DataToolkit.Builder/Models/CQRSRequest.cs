namespace DataToolkit.Builder.Models;

// Clase para recibir la petición
public class CQRSRequest
{
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";

    // Nuevo: concepto de dominio (Ej: Clientes)
    public string DomainName { get; set; } = string.Empty;
}