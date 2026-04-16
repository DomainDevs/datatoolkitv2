using System.Reflection;

namespace DataToolkit.Library.Metadata
{
    public class EntityMetadata
    {
        public string TableName { get; set; }
        public List<PropertyInfo> Properties { get; set; } = new();
        public List<PropertyInfo> KeyProperties { get; set; } = new();
        public List<PropertyInfo> IdentityProperties { get; set; } = new();
        public List<PropertyInfo> RequiredProperties { get; set; } = new();
        public Dictionary<PropertyInfo, string> ColumnMappings { get; set; } = new();
    }
}
