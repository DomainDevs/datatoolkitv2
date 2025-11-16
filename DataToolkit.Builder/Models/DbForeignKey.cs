namespace DataToolkit.Builder.Models;

public class DbForeignKey
{
    public string Name { get; set; } = "";
    public string Column { get; set; } = "";
    public string ReferencedSchema { get; set; } = "";
    public string ReferencedTable { get; set; } = "";
    public string ReferencedColumn { get; set; } = "";
}
