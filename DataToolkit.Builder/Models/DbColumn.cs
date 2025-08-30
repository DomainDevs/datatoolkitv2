namespace DataToolkit.Builder.Models
{
    public class DbColumn
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }

        // 👇 Nuevos campos importantes
        public string? DefaultValue { get; set; }   // Soporte a DEFAULT
        public bool IsComputed { get; set; }        // Soporte a columnas calculadas
        public string? ComputedDefinition { get; set; } // Expresión si es calculada
        public bool HasCheckConstraint { get; set; }    // Restricciones CHECK
        public string? CheckDefinition { get; set; }
    }
}
