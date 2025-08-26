using System.Data.Common;

namespace DataToolkit.Builder.Models
{
    public class DbStoredProcedure
    {
        public string Schema { get; set; } = "dbo";
        public string Name { get; set; } = string.Empty;
        public List<DbParameter> Parameters { get; set; } = new();
        public List<DbColumn> ResultColumns { get; set; } = new();
    }
}
