namespace DataToolkit.Builder.Models
{
    public class DbGroupResult
    {
        public List<DbObjectRef> Tables { get; set; } = new();
        public List<DbObjectRef> Views { get; set; } = new();
        public List<DbObjectRef> Procedures { get; set; } = new();
        public List<DbObjectRef> Triggers { get; set; } = new();
    }
}
