namespace DataToolkit.Builder.Models;

public class MapperRequest
{
    public string Schema { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public bool UseMapperly { get; set; } = false;
}