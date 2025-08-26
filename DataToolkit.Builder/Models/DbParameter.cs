namespace DataToolkit.Builder.Models
{
    public class DbParameter
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public bool IsOutput { get; set; }
        public bool IsNullable { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
    }
}
