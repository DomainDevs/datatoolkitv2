namespace DataToolkit.Builder.Models;

public class DbForeignKey
{
    public string Name { get; set; } = "";
    public string Column { get; set; } = "";
    public string ReferencedSchema { get; set; } = "";
    public string ReferencedTable { get; set; } = "";
    public string ReferencedColumn { get; set; } = "";

    // -----------------------------------------------------
    // 👇 NUEVOS CAMPOS NECESARIOS PARA GENERAR NAVEGACIÓN
    // -----------------------------------------------------
    // Nuevas propiedades
    public string? DeleteRule { get; set; }       // e.g. CASCADE, NO ACTION
    public string? UpdateRule { get; set; }       // e.g. CASCADE, NO ACTION
    public bool IsUnique { get; set; }            // FK con restricción UNIQUE
    public bool IsSelfReference { get; set; }     // referencia a la misma tabla
    public bool IsCollection { get; set; }        // inferencia si debe mapearse como colección (1-N) o referencia (1-1)
}
