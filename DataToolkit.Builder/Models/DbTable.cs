using System.Data.Common;

namespace DataToolkit.Builder.Models
{
    public class DbTable
    {
        public string Schema { get; set; } = "dbo";
        public string Name { get; set; } = string.Empty;
        public List<DbColumn> Columns { get; set; } = new();
    }
}
