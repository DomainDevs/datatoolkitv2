namespace DataToolkit.Builder.Models
{
    public class DBObject
    {
        public string Name { get; set; } = "";
        public string Schema { get; set; } = "dbo";
        public string ScriptSQL { get; set; } = "";
        public string ObjectType { get; set; } = ""; // Ej: P = procedure, V = view, etc.
        public List<String>? Permissions { get; set; } = new(); // Opcional
    }
}
